using System.Diagnostics;
using ImageMagick;
using IMu;
using ImuExports.Tasks.AusGeochem.Config;
using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using ImuExports.Tasks.AusGeochem.Mapping;
using ImuExports.Tasks.AusGeochem.Models;
using LiteDB;
using Microsoft.Extensions.Options;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem;

public class AusGeochemTask : ImuTaskBase, ITask
{
    private readonly AusGeochemOptions _options = (AusGeochemOptions)CommandOptions.TaskOptions;
    private readonly AppSettings _appSettings;
    private readonly IImuFactory<Sample> _sampleFactory;
    private readonly IFactory<Lookups> _lookupsFactory;
    private readonly IAusGeochemClient _ausGeochemClient;
    private readonly IEnumerable<IModuleSearchConfig> _moduleSearchConfigs;
    
    public AusGeochemTask(
        IOptions<AppSettings> appSettings,
        IImuFactory<Sample> sampleFactory,
        IFactory<Lookups> lookupsFactory,
        IAusGeochemClient ausGeochemClient,
        IEnumerable<IModuleSearchConfig> moduleSearchConfigs) : base(appSettings)
    {
        _appSettings = appSettings.Value;
        _sampleFactory = sampleFactory;
        _lookupsFactory = lookupsFactory;
        _ausGeochemClient = ausGeochemClient;
        _moduleSearchConfigs = moduleSearchConfigs;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            // Authenticate
            await _ausGeochemClient.Authenticate(stoppingToken);
            
            if (_options.DeleteAll)
            {
                // Delete all samples and images in AusGeochem
                foreach (var package in _appSettings.AusGeochem.DataPackages)
                {
                    await DeleteAllByDataPackageId(package.Id, stoppingToken);
                }
                
                return;
            }
            
            // Make lookup lists for use in creating Dtos to send
            var lookups = await _lookupsFactory.Make(stoppingToken);

            // Process all packages in appsettings 
            foreach (var package in _appSettings.AusGeochem.DataPackages)
            {
                // Fetch data from EMu
                var samples = await FetchSamples(package.Discipline, stoppingToken);
                
                // Build and then send sample Dtos
                await SendSamples(lookups, samples, package.Id, stoppingToken);
            }

            // Update/Insert application
            using var db = new LiteRepository(new ConnectionString()
            {
                Filename = $"{AppContext.BaseDirectory}{_appSettings.LiteDbFilename}",
                Upgrade = true
            });
            
            var application = _options.Application;
    
            if (application != null)
            {
                Log.Logger.Information("Updating AusGeochem Application PreviousDateRun {PreviousDateRun} to {DateStarted}", application.PreviousDateRun, _options.DateStarted);
                application.PreviousDateRun = _options.DateStarted;
                db.Upsert(application);
            }
        }
    }

    private Terms BuildTerms(string discipline)
    {
        var terms = new Terms();
        terms.Add("ColDiscipline", discipline);
        terms.Add("MdaDataSets_tab", AusGeochemConstants.ImuDataSetsQueryString);
        
        return terms;
    }

    private string[] ExportColumns => new[]
    {
        "irn",
        "ColRegPrefix",
        "ColRegNumber",
        "ColRegPart",
        "ColDiscipline",
        "ColCollectionName_tab",
        "RocRockName",
        "RocRockDescription",
        "RocMainMineralsPresent",
        "RocThinSection",
        "MinSpecies",
        "MinVariety",
        "MinAssociatedMatrix",
        "MinXrayed",
        "MinChemicalAnalysis",
        "MinType",
        "MinTypeType",
        "site=SitSiteRef.(EraDepthDeterminationMethod,LocPreciseLocation,EraMvRockUnit_tab,EraEra,EraAge1,EraAge2,EraMvStage,EraDepthFromMt,EraDepthToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatRadiusUnit_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatPreferred_tab],geo=[LocCountry_tab,LocProvinceStateTerritory_tab,LocDistrictCountyShire_tab,LocTownship_tab,LocNearestNamedPlace_tab,LocPreciseLocation])",
        "media=MulMultiMediaRef_tab.(irn,MulTitle,MulIdentifier,MulMimeType,MulCreator_tab,MdaDataSets_tab,metadata=[MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab],DetAlternateText,RigCreator_tab,RigSource_tab,RigAcknowledgementCredit,RigCopyrightStatement,RigCopyrightStatus,RigLicence,RigLicenceDetails,ChaRepository_tab,ChaMd5Sum,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
        "preparations=[StrSpecimenForm_tab]",
        "LocDateCollectedTo",
        "LocDateCollectedFrom",
        "collectors=LocCollectorsRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName)",
        "prevno=[ManPreviousCollectionName_tab,ManPreviousNumbers_tab]",
        "AdmPublishWebNoPassword"
    };

    private async Task<List<Sample>> FetchSamples(string discipline, CancellationToken stoppingToken)
    {
        // Cache Irns
        Log.Logger.Information("Caching {Discipline} catalogue irns", discipline);
        var cachedIrns = new List<long>();
        
        if (_options.Application.PreviousDateRun.HasValue)
        {
            foreach (var moduleSearchConfig in _moduleSearchConfigs)
            {
                stoppingToken.ThrowIfCancellationRequested();

                if (moduleSearchConfig is IWithTermFilter filter)
                {
                    filter.TermFilters = new List<KeyValuePair<string, string>>()
                    {
                        new("ColDiscipline", discipline)
                    };
                }

                var irns = await CacheIrns(moduleSearchConfig.ModuleName, 
                    moduleSearchConfig.ModuleSelectName,
                    moduleSearchConfig.Terms,
                    moduleSearchConfig.Columns,
                    moduleSearchConfig.IrnSelectFunc,
                    stoppingToken);

                cachedIrns.AddRange(irns);
            }

            // Remove any duplicates
            cachedIrns = cachedIrns.Distinct().ToList();
        }
        else
        {
            stoppingToken.ThrowIfCancellationRequested();

            cachedIrns = (await CacheIrns("ecatalogue", this.BuildTerms(discipline), stoppingToken)).ToList();
        }

        // Fetch sample data from EMu
        var samples = new List<Sample>();
        var offset = 0;
        Log.Logger.Information("Fetching {Discipline} catalogue data", discipline);
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            using var imuSession = new ImuSession("ecatalogue", _appSettings.Emu.Host, int.Parse(_appSettings.Emu.Port));
                
            var cachedIrnsBatch = cachedIrns
                .Skip(offset)
                .Take(Constants.DataBatchSize)
                .ToList();

            if (cachedIrnsBatch.Count == 0)
                break;

            imuSession.FindKeys(cachedIrnsBatch);

            var results = imuSession.Fetch("start", 0, -1, this.ExportColumns);

            Log.Logger.Debug("Fetched {RecordCount} {Discipline} records from EMu", cachedIrnsBatch.Count, discipline);

            samples.AddRange(results.Rows.Select(map => _sampleFactory.Make(map, stoppingToken)));

            offset += results.Count;

            Log.Logger.Information("Fetch catalogue data progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
        }

        return samples;
    }

    private async Task SendSamples(Lookups lookups, IList<Sample> samples, int? dataPackageId, CancellationToken stoppingToken)
    {
        // Exit if DataPackageId not known
        if (dataPackageId == null)
        {
            Log.Logger.Fatal("DataPackageId is null, cannot continue without one, exiting");
            Environment.Exit(Constants.ExitCodeError);
        }
        
        // Fetch all current SampleWithLocationDtos
        Log.Logger.Information("Fetching all current SampleWithLocationDtos within AusGeochem for DataPackageId {DataPackageId}", dataPackageId);
        var currentSampleDtos = await _ausGeochemClient.GetSamplesByPackageId(dataPackageId.Value, stoppingToken);

        // Send/Delete samples
        Log.Logger.Information("Sending samples for DataPackageId {DataPackageId}", dataPackageId);
        var offset = 0;
        foreach (var sample in samples)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            var existingSampleDto = currentSampleDtos.SingleOrDefault(x =>
                string.Equals(x.SampleDto.SourceId, sample.Irn, StringComparison.OrdinalIgnoreCase));

            if (existingSampleDto != null)
            {
                var updatedSampleDto = sample.ToSampleWithLocationDto(lookups, existingSampleDto);

                if (sample.Deleted)
                {
                    // Delete sample
                    await _ausGeochemClient.DeleteSample(updatedSampleDto, stoppingToken);
                    
                    // Delete images
                    IList<ImageReadDto> imageDtos = new List<ImageReadDto>();
                    // Fetch all images associated with sample
                    if (updatedSampleDto.Id != null)
                        imageDtos = await _ausGeochemClient.GetImagesBySampleId(updatedSampleDto.Id.Value, stoppingToken);

                    // Delete all images
                    foreach (var imageDto in imageDtos)
                    {
                        await _ausGeochemClient.DeleteImage(imageDto, stoppingToken);
                    }
                }
                else
                {
                    // Update sample
                    await _ausGeochemClient.SendSample(updatedSampleDto, Method.Put, stoppingToken);

                    // Update images
                    IList<ImageReadDto> imageDtos = new List<ImageReadDto>();
                    // Fetch all images associated with sample
                    if (updatedSampleDto.Id != null)
                        imageDtos = await _ausGeochemClient.GetImagesBySampleId(updatedSampleDto.Id.Value, stoppingToken);

                    // Delete all images
                    foreach (var imageDto in imageDtos)
                    {
                        await _ausGeochemClient.DeleteImage(imageDto, stoppingToken);
                    }

                    // Re-Send all images
                    foreach (var image in sample.Images)
                    {
                        // Fetch image as base64 string from EMu
                        var base64Image = await FetchImageAsBase64(stoppingToken, image);

                        if (updatedSampleDto.Id != null)
                            await _ausGeochemClient.SendImage(image, base64Image, updatedSampleDto.Id.Value, stoppingToken);
                    }
                }
            }
            else
            {
                var createSampleDto = sample.ToSampleWithLocationDto(lookups, dataPackageId, _appSettings.AusGeochem.ArchiveId);

                if (!sample.Deleted)
                {
                    // Create sample
                    await _ausGeochemClient.SendSample(createSampleDto, Method.Post, stoppingToken);

                    // Send Images
                    if (sample.Images.Any())
                    {
                        // Get created sample so we can link sample to image via id
                        createSampleDto = await _ausGeochemClient.GetSampleBySourceId(sample.Irn, stoppingToken);

                        foreach (var image in sample.Images)
                        {
                            // Fetch image as base64 string from EMu
                            var base64Image = await FetchImageAsBase64(stoppingToken, image);

                            if (createSampleDto.Id != null)
                                await _ausGeochemClient.SendImage(image, base64Image, createSampleDto.Id.Value, stoppingToken);
                        }
                    }
                }
                else
                    Log.Logger.Debug("Nothing to do with Sample {ShortName} as it is marked for deletion but doesnt exist in AusGeochem", createSampleDto.ShortName);
            }

            offset++;
            Log.Logger.Information("Send samples progress... {Offset}/{TotalResults}", offset, samples.Count);
        }
    }

    private async Task<string> FetchImageAsBase64(CancellationToken stoppingToken, Image image)
    {
        try
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            var stopwatch = Stopwatch.StartNew();
            
            using var imuSession = new ImuSession("emultimedia", _appSettings.Emu.Host, int.Parse(_appSettings.Emu.Port));
            imuSession.FindKey(image.Irn);
            var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

            if (resource == null)
                throw new IMuException("MultimediaResourceNotFound");

            await using var sourceFileStream = resource["file"] as FileStream;

            using var imageResource = new MagickImage(sourceFileStream);

            imageResource.Format = MagickFormat.Jpg;
            imageResource.Quality = 90;
            imageResource.FilterType = FilterType.Lanczos;
            imageResource.ColorSpace = ColorSpace.sRGB;
            imageResource.Resize(new MagickGeometry(3000) { Greater = true });
            imageResource.UnsharpMask(0.5, 0.5, 0.6, 0.025);

            var base64Image = imageResource.ToBase64();

            stopwatch.Stop();
            
            Log.Logger.Debug("Completed fetching image {Irn} in {ElapsedMilliseconds}", image.Irn,
                stopwatch.ElapsedMilliseconds);

            return base64Image;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching image {Irn}, exiting", image.Irn);
            Environment.Exit(Constants.ExitCodeError);
        }
        
        return null;
    }

    private async Task DeleteAllByDataPackageId(int? dataPackageId, CancellationToken stoppingToken)
    {
        // Exit if DataPackageId not known
        if (dataPackageId == null)
        {
            Log.Logger.Fatal("DataPackageId is null, cannot continue without one, exiting");
            Environment.Exit(Constants.ExitCodeError);
        }
        
        // Fetch all current SampleWithLocationDtos
        Log.Logger.Information("Fetching all current SampleWithLocationDtos within AusGeochem for DataPackageId {DataPackageId}", dataPackageId);
        var currentSampleDtos = await _ausGeochemClient.GetSamplesByPackageId(dataPackageId.Value, stoppingToken);

        Log.Logger.Information("Deleting all entities for DataPackageId {DataPackageId}", dataPackageId);
        foreach (var sampleDto in currentSampleDtos)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            IList<ImageReadDto> imageDtos = new List<ImageReadDto>();
            // Fetch all images associated with sample
            if (sampleDto.Id != null)
                imageDtos = await _ausGeochemClient.GetImagesBySampleId(sampleDto.Id.Value, stoppingToken);

            // Delete all images
            foreach (var imageDto in imageDtos)
            {
                stoppingToken.ThrowIfCancellationRequested();
                
                await _ausGeochemClient.DeleteImage(imageDto, stoppingToken);
            }
            
            // Delete sample
            await _ausGeochemClient.DeleteSample(sampleDto, stoppingToken);
        }
    }
}