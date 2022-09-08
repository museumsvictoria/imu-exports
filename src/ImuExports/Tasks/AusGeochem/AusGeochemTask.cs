using IMu;
using ImuExports.Tasks.AusGeochem.Config;
using ImuExports.Tasks.AusGeochem.Models;
using LiteDB;
using Microsoft.Extensions.Options;

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
                    await _ausGeochemClient.DeleteAllByDataPackageId(package.Id, stoppingToken);
                }
                
                return;
            }
            
            // Make lookup lists for use in creating Dtos to send
            var lookups = await _lookupsFactory.Make(stoppingToken);

            // Process all packages in appsettings 
            foreach (var package in _appSettings.AusGeochem.DataPackages)
            {
                // Fetch samples from IMu
                var samples = await FetchSamples(package.Discipline, stoppingToken);

                // Build and then send sample Dtos
                await _ausGeochemClient.SendSamples(lookups, samples, package.Id, stoppingToken);
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
        // Cache Irns only
        Log.Logger.Information("Caching {Discipline} catalogue irns", discipline);
        var cachedIrns = new List<long>();
        
        if (_options.Application.PreviousDateRun.HasValue)
        {
            // If search has run before we need to search multiple EMu modules for any changes
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

        // Fetch actual catalogue data from IMu
        var samples = new List<Sample>();
        var offset = 0;
        Log.Logger.Information("Fetching {Discipline} catalogue data", discipline);
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

            var results = imuSession.Fetch("start", 0, -1, this.ExportColumns);

            Log.Logger.Debug("Fetched {RecordCount} {Discipline} records from IMu", cachedIrnsBatch.Count, discipline);

            // Build sample from catalogue data and add to samples
            samples.AddRange(results.Rows.Select(map => _sampleFactory.Make(map, stoppingToken)));

            offset += results.Count;

            Log.Logger.Information("Fetch catalogue data progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
        }

        return samples;
    }
}