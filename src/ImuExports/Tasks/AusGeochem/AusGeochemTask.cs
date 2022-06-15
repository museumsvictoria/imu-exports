using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IMu;
using ImuExports.Tasks.AusGeochem.ClassMaps;
using ImuExports.Tasks.AusGeochem.Config;
using ImuExports.Tasks.AusGeochem.Models;
using Microsoft.Extensions.Options;

namespace ImuExports.Tasks.AusGeochem;

public class AusGeochemTask : ImuTaskBase, ITask
{
    private readonly AusGeochemOptions _options = (AusGeochemOptions)CommandOptions.TaskOptions;
    private readonly AppSettings _appSettings;
    private readonly IFactory<Specimen> _specimenFactory;

    public AusGeochemTask(
        IOptions<AppSettings> appSettings,
        IFactory<Specimen> specimenFactory) : base(appSettings)
    {
        _appSettings = appSettings.Value;
        _specimenFactory = specimenFactory;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            // Cache Irns
            var cachedIrns = await CacheIrns("ecatalogue", this.BuildExportSearchTerms(), stoppingToken);

            // Fetch data
            var specimens = new List<Specimen>();
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

                    specimens.AddRange(results.Rows.Select(map => _specimenFactory.Make(map, stoppingToken)));

                    offset += results.Count;

                    Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                }
            }

            // Save data
            Log.Logger.Information("Saving specimen data as csv");
            
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                SanitizeForInjection = false
            };
            
            await using (var writer = new StreamWriter(_options.Destination + @"specimens.csv", false, Encoding.UTF8))
            await using (var csv = new CsvWriter(writer, csvConfig))
            {
                csv.Context.RegisterClassMap<SpecimenClassMap>();
                await csv.WriteRecordsAsync(specimens, stoppingToken);
            }
        }
    }

    private Terms BuildExportSearchTerms()
    {
        var searchTerms = new Terms();
        searchTerms.Add("ColCategory", "Natural Sciences");
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