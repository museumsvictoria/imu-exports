using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IMu;
using ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps;
using ImuExports.Tasks.AtlasOfLivingAustralia.Config;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using LiteDB;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace ImuExports.Tasks.AtlasOfLivingAustralia;

public class AtlasOfLivingAustraliaTask : ImuTaskBase, ITask
{
    private readonly AtlasOfLivingAustraliaOptions _options = (AtlasOfLivingAustraliaOptions)CommandOptions.TaskOptions;
    private readonly AppSettings _appSettings;
    private readonly IImuFactory<Occurrence> _occurrenceFactory;
    private readonly IEnumerable<IModuleSearchConfig> _moduleSearchConfigs;
    
    public AtlasOfLivingAustraliaTask(
        IOptions<AppSettings> appSettings,
        IImuFactory<Occurrence> occurrenceFactory,
        IEnumerable<IModuleSearchConfig> moduleSearchConfigs) : base(appSettings)
    {
        _appSettings = appSettings.Value;
        _occurrenceFactory = occurrenceFactory;
        _moduleSearchConfigs = moduleSearchConfigs;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
        {
            // Cache Irns
            var cachedIrns = new List<long>();

            if (_options.ParsedModifiedAfterDate.HasValue ||
                _options.ParsedModifiedBeforeDate.HasValue)
            {
                foreach (var moduleSearchConfig in _moduleSearchConfigs)
                {
                    stoppingToken.ThrowIfCancellationRequested();

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

                cachedIrns = (await CacheIrns("ecatalogue", BuildFullExportSearchTerms(), stoppingToken)).ToList();
            }

            // Fetch data
            var occurrences = new List<Occurrence>();
            var offset = 0;
            Log.Logger.Information("Fetching data");
            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();

                using var imuSession = new ImuSession("ecatalogue", _appSettings.Imu.Host, _appSettings.Imu.Port);
                
                var cachedIrnsBatch = cachedIrns
                    .Skip(offset)
                    .Take(Constants.DataBatchSize)
                    .ToList();

                if (cachedIrnsBatch.Count == 0)
                    break;

                imuSession.FindKeys(cachedIrnsBatch);

                var results = imuSession.Fetch("start", 0, -1, ExportColumns);

                Log.Logger.Debug("Fetched {RecordCount} records from IMu", cachedIrnsBatch.Count);
                    
                occurrences.AddRange(results.Rows.Select(map => _occurrenceFactory.Make(map, stoppingToken)));

                offset += results.Count;

                Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
            }

            // Save data
            Log.Logger.Information("Saving occurrence data as csv");

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                SanitizeForInjection = false
            };

            await using (var writer = new StreamWriter(_options.Destination + @"occurrences.csv", false, Encoding.UTF8))
            await using (var csv = new CsvWriter(writer, csvConfig))
            {
                csv.Context.RegisterClassMap<OccurrenceClassMap>();
                await csv.WriteRecordsAsync(occurrences, stoppingToken);
            }

            Log.Logger.Information("Saving multimedia data as csv");
            await using (var writer = new StreamWriter(_options.Destination + @"multimedia.csv", false, Encoding.UTF8))
            await using (var csv = new CsvWriter(writer, csvConfig))
            {
                var multimedia = occurrences.SelectMany(x => x.Multimedia);

                csv.Context.RegisterClassMap<MultimediaClassMap>();
                await csv.WriteRecordsAsync(multimedia, stoppingToken);
            }
            
            // Copy meta.xml
            Log.Logger.Information("Copying meta.xml");
            File.Copy(@$"{AppContext.BaseDirectory}meta.xml", _options.Destination + @"meta.xml", true);
            
            // Compress/Upload files to ALA if automated export
            if (_options.IsAutomated)
            {
                // Determine filename
                string startDate = null; 
                string endDate = null;

                if (_options.ParsedModifiedAfterDate.HasValue)
                {
                    startDate = _options.ParsedModifiedAfterDate?.ToString("yyyy-MM-dd");
                }
                
                if (_options.ParsedModifiedBeforeDate.HasValue)
                {
                    endDate = _options.ParsedModifiedBeforeDate <= _options.DateStarted
                            ? _options.ParsedModifiedBeforeDate?.ToString("yyyy-MM-dd")
                            : _options.DateStarted.ToString("yyyy-MM-dd");
                }

                string zipFilename;
                if (startDate != null && endDate != null)
                {
                    zipFilename = $"mv-dwca-{startDate}-to-{endDate}.zip";
                }
                else if (startDate != null)
                {
                    zipFilename = $"mv-dwca-after-{startDate}.zip";
                }
                else if (endDate != null)
                {
                    zipFilename = $"mv-dwca-before-{endDate}.zip";
                }
                else
                {
                    zipFilename = $"mv-dwca.zip";
                }  
                
                var tempFilepath = $"{Path.GetTempPath()}{Utils.RandomString(8)}.tmp";
                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    // Zip Directory
                    ZipFile.CreateFromDirectory(_options.Destination, tempFilepath,
                        CompressionLevel.NoCompression, false);
                    Log.Logger.Information(
                        "Created temporary zip file {TempFilepath} in {Elapsed} ({ElapsedMilliseconds} ms)",
                        tempFilepath,
                        stopwatch.Elapsed, stopwatch.ElapsedMilliseconds);

                    // Delete uncompressed files
                    stopwatch.Restart();
                    Directory.EnumerateFiles(_options.Destination).ToList().ForEach(File.Delete);
                    Log.Logger.Information(
                        "Deleted uncompressed files in {Destination} in {Elapsed} ({ElapsedMilliseconds} ms)",
                        _options.Destination, stopwatch.Elapsed, stopwatch.ElapsedMilliseconds);

                    // Move zip file
                    stopwatch.Restart();
                    File.Move(tempFilepath, $"{_options.Destination}{zipFilename}");
                    Log.Logger.Information(
                        "Moved zip file {ZipFilename} to {Destination} in {Elapsed} ({ElapsedMilliseconds} ms)",
                        zipFilename, _options.Destination, stopwatch.Elapsed,
                        stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    // Log and cleanup before exit
                    Log.Logger.Fatal(ex, "Error creating zip archive");

                    throw;
                }

                try
                {
                    // Upload files
                    using var client = new SftpClient(_appSettings.AtlasOfLivingAustralia.Host,
                        22, _appSettings.AtlasOfLivingAustralia.Username,
                        _appSettings.AtlasOfLivingAustralia.Password);
                    
                    Log.Logger.Information("Connecting to sftp server {Host}", _appSettings.AtlasOfLivingAustralia.Host);
                    client.Connect();
                        
                    stopwatch.Restart();
                    await using (var fileStream = new FileStream($"{_options.Destination}{zipFilename}", FileMode.Open))
                    {
                        Log.Logger.Information(
                            "Uploading zip {ZipFilename} ({Length})", zipFilename, Utils.BytesToString(fileStream.Length));
                        client.BufferSize = 4 * 1024; // bypass Payload error large files
                        client.UploadFile(fileStream, zipFilename);
                    }
                        
                    stopwatch.Stop();
                    Log.Logger.Information(
                        "Uploaded {ZipFilename} in {Elapsed} ({ElapsedMilliseconds} ms)",
                        zipFilename, stopwatch.Elapsed, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    // Log and cleanup before exit
                    Log.Logger.Fatal(ex, "Error uploading zip archive");
                    
                    throw;
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
                    Log.Logger.Information("Updating ALA Application PreviousDateRun {PreviousDateRun} to {DateStarted}", application.PreviousDateRun, _options.DateStarted);
                    application.PreviousDateRun = _options.DateStarted;
                    db.Upsert(application);
                }
            }
        }
    }

    private Terms BuildFullExportSearchTerms()
    {
        var searchTerms = new Terms();
        searchTerms.Add("ColCategory", "Natural Sciences");
        searchTerms.Add("MdaDataSets_tab", AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString);
        searchTerms.Add("AdmPublishWebNoPassword", "Yes");

        if (_options.ParsedModifiedAfterDate.HasValue)
            searchTerms.Add("AdmDateModified", _options.ParsedModifiedAfterDate.Value.ToString("MMM dd yyyy"), ">=");

        if (_options.ParsedModifiedBeforeDate.HasValue)
            searchTerms.Add("AdmDateModified", _options.ParsedModifiedBeforeDate.Value.ToString("MMM dd yyyy"), "<=");

        return searchTerms;
    }

    private string[] ExportColumns => new[]
    {
        "irn",
        "ColRegPrefix",
        "ColRegNumber",
        "ColRegPart",
        "ColTypeOfItem",
        "AdmDateModified",
        "AdmTimeModified",
        "ColDiscipline",
        "MdaDataSets_tab",
        "colevent=ColCollectionEventRef.(AdmDateModified,AdmTimeModified,ExpExpeditionName,ColCollectionEventCode,ColCollectionMethod,ColDateVisitedFrom,ColDateVisitedTo,ColTimeVisitedTo,ColTimeVisitedFrom,AquDepthToMet,AquDepthFromMet,site=ColSiteRef.(AdmDateModified,AdmTimeModified,SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocIslandGroup,LocIsland,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab,LatPreferred_tab]),collectors=ColParticipantRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName))",
        "SpeNoSpecimens",
        "BirTotalClutchSize",
        "SpeSex_tab",
        "SpeStageAge_tab",
        "preparations=[StrSpecimenNature_tab,StrSpecimenForm_tab,StrFixativeTreatment_tab,StrStorageMedium_tab,StrDatePrepared0]",
        "DarYearCollected",
        "DarMonthCollected",
        "DarDayCollected",
        "site=SitSiteRef.(AdmDateModified,AdmTimeModified,SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocIslandGroup,LocIsland,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab,LatPreferred_tab])",
        "identifications=[IdeTypeStatus_tab,IdeCurrentNameLocal_tab,identifiers=IdeIdentifiedByRef_nesttab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),IdeDateIdentified0,IdeQualifier_tab,IdeQualifierRank_tab,taxa=TaxTaxonomyRef_tab.(irn,AdmDateModified,AdmTimeModified,ClaScientificName,ClaKingdom,ClaPhylum,ClaSubphylum,ClaSuperclass,ClaClass,ClaSubclass,ClaSuperorder,ClaOrder,ClaSuborder,ClaInfraorder,ClaSuperfamily,ClaFamily,ClaSubfamily,ClaTribe,ClaSubtribe,ClaGenus,ClaSubgenus,ClaSpecies,ClaSubspecies,ClaRank,AutAuthorString,ClaApplicableCode,comname=[ComName_tab,ComStatus_tab])]",
        "media=MulMultiMediaRef_tab.(irn,MulTitle,MulIdentifier,MulMimeType,MulCreator_tab,MdaDataSets_tab,metadata=[MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab],DetAlternateText,RigCreator_tab,RigSource_tab,RigAcknowledgementCredit,RigCopyrightStatement,RigCopyrightStatus,RigLicence,RigLicenceDetails,ChaRepository_tab,ChaMd5Sum,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
        "parent=ColParentRecordRef.(irn,ColRegPrefix,ColRegNumber,ColRegPart,ColDiscipline,MdaDataSets_tab,ColTypeOfItem)",
        "tissue=[TisInitialPreservation_tab,TisLtStorageMethod_tab,TisDatePrepared0,TisTissueType_tab]",
        "TisCollectionCode",
        "TisOtherInstitutionNo",
        "TisRegistrationNumber",
        "ManOnLoan",
        "location=LocCurrentLocationRef.(irn)",
        "TisTissueUsedUp",
        "GneDnaUsedUp",
        "TisAvailableForLoan",
        "preparedby=StrPreparedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName)"
    };
}