using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using IMu;
using ImuExports.NetFramework472.Config;
using ImuExports.NetFramework472.Infrastructure;
using ImuExports.NetFramework472.Tasks.AusGeochem.ClassMaps;
using ImuExports.NetFramework472.Tasks.AusGeochem.Config;
using ImuExports.NetFramework472.Tasks.AusGeochem.Models;
using Serilog;

namespace ImuExports.NetFramework472.Tasks.AusGeochem
{
    public class AusGeochemTask : ImuTaskBase, ITask
    {
        private readonly IFactory<Specimen> specimenFactory;
        private readonly AusGeochemOptions options = GlobalOptions.Options.Agn;

        public AusGeochemTask(
            IFactory<Specimen> specimenFactory)
        {
            this.specimenFactory = specimenFactory;
        }

        public void Run()
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
            {
                // Cache Irns
                List<long> cachedIrns;

                if (Program.ImportCanceled) return;

                cachedIrns = CacheIrns("ecatalogue", this.BuildExportSearchTerms()).ToList();

                // Fetch data
                var specimens = new List<Specimen>();
                var offset = 0;
                Log.Logger.Information("Fetching data");
                while (true)
                {
                    if (Program.ImportCanceled) return;

                    using (var imuSession = ImuSessionProvider.CreateInstance("ecatalogue"))
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

                        specimens.AddRange(results.Rows.Select(specimenFactory.Make));

                        offset += results.Count;

                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                    }
                }

                // Save data
                Log.Logger.Information("Saving specimen data as csv");
                using (var csvWriter = new CsvWriter(new StreamWriter(this.options.Destination + @"specimens.csv",
                    false, Encoding.UTF8)))
                {
                    csvWriter.Configuration.RegisterClassMap<SpecimenClassMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.Configuration.SanitizeForInjection = false;
                    csvWriter.WriteRecords(specimens);
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
            "prevno=[ManPreviousCollectionName_tab,ManPreviousNumbers_tab]",
        };
    }
}