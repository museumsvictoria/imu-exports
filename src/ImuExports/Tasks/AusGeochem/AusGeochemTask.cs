using IMu;
using ImuExports.Tasks.AusGeochem.Config;
using ImuExports.Tasks.AusGeochem.Factories;
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

            // Fetch mineralogy data from EMu
            var mineralogySamples = await FetchSamples("Mineralogy", stoppingToken);

            // Fetch petrology data from EMu
            var petrologySamples = await FetchSamples("Petrology", stoppingToken);
            
            // Authenticate
            await _ausGeochemClient.Authenticate(stoppingToken);
            
            // Make lookup lists for use in creating Dtos to send
            var lookups = await _lookupsFactory.Make(stoppingToken);

            // Build and then send mineralogy sample Dtos
            await SendSamples(lookups, mineralogySamples, _appSettings.AusGeochem.MineralogyDataPackageId, stoppingToken);
            
            // Build and then send petrology samples Dtos
            await SendSamples(lookups, petrologySamples, _appSettings.AusGeochem.PetrologyDataPackageId, stoppingToken);

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
        var currentDtos = await _ausGeochemClient.FetchCurrentSamples(dataPackageId.Value, stoppingToken);

        // Send/Delete samples
        Log.Logger.Information("Sending samples for DataPackageId {DataPackageId}", dataPackageId);
        var offset = 0;
        foreach (var sample in samples)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            var existingDto = currentDtos.SingleOrDefault(x =>
                string.Equals(x.SampleDto.SourceId, sample.SampleId, StringComparison.OrdinalIgnoreCase));

            if (existingDto != null)
            {
                var dto = sample.ToSampleWithLocationDto(lookups, existingDto);

                if (sample.Deleted)
                    // Delete
                    await _ausGeochemClient.DeleteSample(dto, stoppingToken);
                else
                    // Update
                    await _ausGeochemClient.SendSample(dto, Method.Put, stoppingToken);
            }
            else
            {
                var dto = sample.ToSampleWithLocationDto(lookups, dataPackageId, _appSettings.AusGeochem.ArchiveId);

                if (!sample.Deleted)
                    // Create
                    await _ausGeochemClient.SendSample(dto, Method.Post, stoppingToken);
                else
                    Log.Logger.Debug("Nothing to do with Sample {ShortName} as it is marked for deletion but doesnt exist in AusGeochem", dto.ShortName);
            }

            offset++;
            Log.Logger.Information("Send samples progress... {Offset}/{TotalResults}", offset, samples.Count);
        }
    }
}