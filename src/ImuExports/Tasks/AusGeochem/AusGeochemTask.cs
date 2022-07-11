using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ImuExports.Tasks.AusGeochem;

public class AusGeochemTask : ImuTaskBase, ITask
{
    private readonly AusGeochemOptions _options = (AusGeochemOptions)CommandOptions.TaskOptions;
    private readonly AppSettings _appSettings;
    private readonly IFactory<Sample> _sampleFactory;

    public AusGeochemTask(
        IOptions<AppSettings> appSettings,
        IFactory<Sample> sampleFactory) : base(appSettings)
    {
        _appSettings = appSettings.Value;
        _sampleFactory = sampleFactory;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Cache Mineralogy Irns
            var cachedIrns = await CacheIrns("ecatalogue", this.BuildMineralogySearchTerms(), stoppingToken);

            // TODO: remove after testing complete
            cachedIrns = cachedIrns.Take(1).ToList();

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
            
            // Cache Petrology Irns
            cachedIrns = await CacheIrns("ecatalogue", this.BuildPetrologySearchTerms(), stoppingToken);
            
            // TODO: remove after testing complete
            cachedIrns = cachedIrns.Take(1).ToList();
            
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
                Log.Logger.Information("Sending sample data via API");
                
                using var client = new RestClient(_appSettings.AusGeochem.BaseUrl);

                client.UseSystemTextJson(new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                
                // Request JWT
                var request = new RestRequest("authenticate", Method.Post).AddJsonBody(new LoginRequest()
                {
                    Password = _appSettings.AusGeochem.Password,
                    Username = _appSettings.AusGeochem.Username
                });

                var response = await client.ExecuteAsync<LoginResponse>(request, stoppingToken);

                if (!response.IsSuccessful)
                {
                    Log.Logger.Error(response.ErrorException, "Could not successfully authenticate, exiting");
                    Environment.Exit(Constants.ExitCodeError);
                }
                else if (!string.IsNullOrWhiteSpace(response.Data?.Token))
                {
                    client.Authenticator = new JwtAuthenticator(response.Data.Token);                
                }

                // Test sending mineralogy samples
                foreach (var sample in mineralogySamples)
                {
                    var dto = sample.ToSampleWithLocationDto();
                    
                    dto.SampleDto.DataPackageId = 3133263;
                    dto.SampleDto.MaterialName = "Basalt";
                    dto.SampleDto.MaterialId = 108735;
                    
                    var sampleRequest = new RestRequest("core/sample-with-locations", Method.Post).AddJsonBody(dto);
                
                    var sampleResponse = await client.ExecuteAsync(sampleRequest, stoppingToken);
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
}