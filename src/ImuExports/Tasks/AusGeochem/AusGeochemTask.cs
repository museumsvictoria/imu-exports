using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IMu;
using ImuExports.Tasks.AusGeochem.ClassMaps;
using ImuExports.Tasks.AusGeochem.Config;
using ImuExports.Tasks.AusGeochem.Extensions;
using ImuExports.Tasks.AusGeochem.Models;
using ImuExports.Tasks.AusGeochem.Models.Api;
using LiteDB;
using Microsoft.Extensions.Options;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem;

public class AusGeochemTask : ImuTaskBase, ITask
{
    private readonly AusGeochemOptions _options = (AusGeochemOptions)CommandOptions.TaskOptions;
    private readonly AppSettings _appSettings;
    private readonly IFactory<Sample> _sampleFactory;
    private readonly IAusGeochemClient _ausGeochemClient;
    
    public AusGeochemTask(
        IOptions<AppSettings> appSettings,
        IFactory<Sample> sampleFactory,
        IAusGeochemClient ausGeochemClient) : base(appSettings)
    {
        _appSettings = appSettings.Value;
        _sampleFactory = sampleFactory;
        _ausGeochemClient = ausGeochemClient;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Cache Mineralogy Irns
            var cachedIrns = await CacheIrns("ecatalogue", this.BuildMineralogySearchTerms(), stoppingToken);

            // Fetch Mineralogy data
            var mineralogySamples = new List<Sample>();
            var offset = 0;
            Log.Logger.Information("Fetching data");
            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();

                using (var imuSession = new ImuSession("ecatalogue", _appSettings.Emu.Host, int.Parse(_appSettings.Emu.Port)))
                {
                    var cachedIrnsBatch = cachedIrns
                        .Skip(offset)
                        .Take(Constants.DataBatchSize)
                        .ToList();

                    if (cachedIrnsBatch.Count == 0)
                        break;

                    imuSession.FindKeys(cachedIrnsBatch);

                    var results = imuSession.Fetch("start", 0, -1, this.ExportColumns);

                    Log.Logger.Debug("Fetched {RecordCount} records from Imu", cachedIrnsBatch.Count);

                    mineralogySamples.AddRange(results.Rows.Select(map => _sampleFactory.Make(map, stoppingToken)));

                    offset += results.Count;

                    Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                }
            }
            
            // TODO: remove after testing complete
            mineralogySamples = mineralogySamples.Take(5).ToList();
            
            // Cache Petrology Irns
            cachedIrns = await CacheIrns("ecatalogue", this.BuildPetrologySearchTerms(), stoppingToken);

            // Fetch Petrology data
            var petrologySamples = new List<Sample>();
            offset = 0;
            Log.Logger.Information("Fetching data");
            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();
            
                using (var imuSession = new ImuSession("ecatalogue", _appSettings.Emu.Host, int.Parse(_appSettings.Emu.Port)))
                {
                    var cachedIrnsBatch = cachedIrns
                        .Skip(offset)
                        .Take(Constants.DataBatchSize)
                        .ToList();
            
                    if (cachedIrnsBatch.Count == 0)
                        break;
            
                    imuSession.FindKeys(cachedIrnsBatch);
            
                    var results = imuSession.Fetch("start", 0, -1, this.ExportColumns);
            
                    Log.Logger.Debug("Fetched {RecordCount} records from Imu", cachedIrnsBatch.Count);
            
                    petrologySamples.AddRange(results.Rows.Select(map => _sampleFactory.Make(map, stoppingToken)));
            
                    offset += results.Count;
            
                    Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                }
            }
            
            // TODO: remove after testing complete
            petrologySamples = petrologySamples.Take(5).ToList();

            if (!string.IsNullOrWhiteSpace(_options.Destination))
            {
                // Save to CSV if destination specified
                Log.Logger.Information("Saving sample data as csv");
            
                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    SanitizeForInjection = false
                };
            
                await using (var writer = new StreamWriter(_options.Destination + @"mineralogy-samples.csv", false, Encoding.UTF8))
                await using (var csv = new CsvWriter(writer, csvConfig))
                {
                    csv.Context.RegisterClassMap<MineralogySampleClassMap>();
                    await csv.WriteRecordsAsync(mineralogySamples, stoppingToken);
                }
            
                await using (var writer = new StreamWriter(_options.Destination + @"petrology-samples.csv", false, Encoding.UTF8))
                await using (var csv = new CsvWriter(writer, csvConfig))
                {
                    csv.Context.RegisterClassMap<PetrologySampleClassMap>();
                    await csv.WriteRecordsAsync(petrologySamples, stoppingToken);
                }
            }
            else
            {
                // Send directly to AusGeochem via API
                
                // Authenticate
                await _ausGeochemClient.Authenticate(stoppingToken);
                
                // Fetch lookup lists
                var (locationKinds, materials, sampleKinds, materialsLookup) = await _ausGeochemClient.FetchLookups(stoppingToken);

                // Send mineralogy samples
                await SendSamples(locationKinds, materials, sampleKinds, materialsLookup, mineralogySamples, _appSettings.AusGeochem.MineralogyDataPackageId, stoppingToken);
                
                // Send petrology samples
                await SendSamples(locationKinds, materials, sampleKinds, materialsLookup, petrologySamples, _appSettings.AusGeochem.PetrologyDataPackageId, stoppingToken);

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
    }

    private Terms BuildMineralogySearchTerms()
    {
        var searchTerms = new Terms();
        searchTerms.Add("ColDiscipline", "Mineralogy");
        searchTerms.Add("MdaDataSets_tab", AusGeochemConstants.QueryString);
        searchTerms.Add("AdmPublishWebNoPassword", "Yes");

        return searchTerms;
    }
    
    private Terms BuildPetrologySearchTerms()
    {
        var searchTerms = new Terms();
        searchTerms.Add("ColDiscipline", "Petrology");
        searchTerms.Add("MdaDataSets_tab", AusGeochemConstants.QueryString);
        searchTerms.Add("AdmPublishWebNoPassword", "Yes");

        return searchTerms;
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
        "preparations=[StrSpecimenForm_tab]",
        "LocDateCollectedTo",
        "LocDateCollectedFrom",
        "collectors=LocCollectorsRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName)",
        "prevno=[ManPreviousCollectionName_tab,ManPreviousNumbers_tab]"
    };

    private async Task SendSamples(IList<LocationKindDto> locationKinds, IList<MaterialDto> materials,
        IList<SampleKindDto> sampleKinds, IList<MaterialLookup> materialsLookup, IList<Sample> samples,
        int? dataPackageId, CancellationToken stoppingToken)
    {
        // Exit if DataPackageId not known
        if (dataPackageId == null)
        {
            Log.Logger.Fatal("DataPackageId is null, cannot continue without one, exiting");
            Environment.Exit(Constants.ExitCodeError);
        }

        Log.Logger.Information("Sending sample data to AusGeochem API where DataPackageId {DataPackageId}",
            dataPackageId);

        // Fetch all current SampleWithLocationDtos
        var currentDtos = await _ausGeochemClient.FetchCurrentSamples(dataPackageId.Value, stoppingToken);

        var offset = 0;
        foreach (var sample in samples)
        {
            var existingDto = currentDtos.SingleOrDefault(x =>
                string.Equals(x.SampleDto.SourceId, sample.SampleId, StringComparison.OrdinalIgnoreCase));

            if (existingDto != null)
            {
                var dto = existingDto.UpdateFromSample(sample, locationKinds, materials, sampleKinds, materialsLookup);

                Log.Logger.Debug("Updating existing Dto {ShortName}", dto.ShortName);

                await _ausGeochemClient.SendSample(dto, Method.Put, stoppingToken);
            }
            else
            {
                var dto = sample.CreateSampleWithLocationDto(locationKinds, materials, sampleKinds, materialsLookup,
                    dataPackageId,
                    _appSettings.AusGeochem.ArchiveId);

                Log.Logger.Debug("Creating new Dto {ShortName}", dto.ShortName);

                await _ausGeochemClient.SendSample(dto, Method.Post, stoppingToken);
            }

            offset++;
            Log.Logger.Information("Api upload progress... {Offset}/{TotalResults}", offset, samples.Count);
        }
    }
}