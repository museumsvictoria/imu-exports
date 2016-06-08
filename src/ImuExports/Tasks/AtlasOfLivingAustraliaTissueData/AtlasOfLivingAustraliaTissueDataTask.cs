using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using ImuExports.Config;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustraliaTissueData.CsvMaps;
using ImuExports.Tasks.AtlasOfLivingAustraliaTissueData.Models;
using IMu;
using Serilog;

namespace ImuExports.Tasks.AtlasOfLivingAustraliaTissueData
{
    public class AtlasOfLivingAustraliaTissueDataTask : ImuTaskBase, ITask
    {
        private readonly IFactory<OccurrenceTissueData> occurrenceTissueDataFactory;

        public AtlasOfLivingAustraliaTissueDataTask(IFactory<OccurrenceTissueData> occurrenceTissueDataFactory)
        {
            this.occurrenceTissueDataFactory = occurrenceTissueDataFactory;
        }

        public void Run()
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
            {
                // Cache Irns
                var cachedIrns = this.CacheIrns("ecatalogue", BuildSearchTerms());

                // Fetch data
                var occurrences = new List<OccurrenceTissueData>();
                var offset = 0;
                Log.Logger.Information("Fetching data");
                while (true)
                {
                    if (Program.ImportCanceled)
                        return;

                    using (var imuSession = ImuSessionProvider.CreateInstance("ecatalogue"))
                    {
                        var cachedIrnsBatch = cachedIrns
                            .Skip(offset)
                            .Take(Constants.DataBatchSize)
                            .ToList();

                        if (cachedIrnsBatch.Count == 0 || occurrences.Count >= 2000)
                            break;

                        imuSession.FindKeys(cachedIrnsBatch);

                        var results = imuSession.Fetch("start", 0, -1, Columns);

                        Log.Logger.Debug("Fetched {RecordCount} records from Imu", cachedIrnsBatch.Count);

                        occurrences.AddRange(results.Rows.Select(occurrenceTissueDataFactory.Make));

                        offset += results.Count;

                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                    }
                }

                // Save data
                Log.Logger.Information("Saving occurrence data as csv");
                using (var csvWriter = new CsvWriter(new StreamWriter(Config.Config.Options.Atd.Destination + @"occurrences.csv", false, Encoding.UTF8)))
                {
                    csvWriter.Configuration.RegisterClassMap<OccurrenceTissueDataCsvMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.WriteRecords(occurrences);
                }
            }
        }

        private Terms BuildSearchTerms()
        {
            var searchTerms = new Terms();
            searchTerms.Add("ColDiscipline", "DNA Laboratory");
            searchTerms.Add("ColRegPrefix", "Z");
            searchTerms.Add("TisAvailableForLoan", "Yes");
            searchTerms.Add("AdmPublishWebNoPassword", "Yes");
            
            DateTime modifiedAfterDate;
            if (!string.IsNullOrWhiteSpace(Config.Config.Options.Atd.ModifiedAfterDate) && DateTime.TryParseExact(Config.Config.Options.Atd.ModifiedAfterDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out modifiedAfterDate))
            {
                searchTerms.Add("AdmDateModified", modifiedAfterDate.ToString("MMM dd yyyy"), ">=");
            }

            DateTime modifiedBeforeDate;
            if (!string.IsNullOrWhiteSpace(Config.Config.Options.Atd.ModifiedBeforeDate) && DateTime.TryParseExact(Config.Config.Options.Atd.ModifiedBeforeDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out modifiedBeforeDate))
            {
                searchTerms.Add("AdmDateModified", modifiedBeforeDate.ToString("MMM dd yyyy"), "<=");
            }

            return searchTerms;
        }

        private static string[] Columns => new []
        {
            "irn",
            "ColRegPrefix",
            "ColRegNumber",
            "ColRegPart",
            "ColTypeOfItem",
            "AdmDateModified",
            "AdmTimeModified",
            "ColDiscipline",
            "TisOtherInstitutionName",
            "TisOtherInstitutionNo",
            "tissue=[TisInitialPreservation_tab,TisLtStorageMethod_tab,TisDatePrepared0,TisTissueType_tab,TisInitialQuantity_tab]",
            "parent=ColParentRecordRef.(irn,ColRegPrefix,ColRegNumber,ColRegPart,ColDiscipline,MdaDataSets_tab,ColParentType)",
            "colevent=ColCollectionEventRef.(ExpExpeditionName,ColCollectionEventCode,ColCollectionMethod,ColDateVisitedFrom,ColDateVisitedTo,ColTimeVisitedTo,ColTimeVisitedFrom,AquDepthToMet,AquDepthFromMet,site=ColSiteRef.(SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab]),collectors=ColParticipantRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName))",
            "SpeNoSpecimens",
            "BirTotalClutchSize",
            "SpeSex_tab",
            "SpeStageAge_tab",
            "preparations=[StrSpecimenNature_tab,StrSpecimenForm_tab,StrFixativeTreatment_tab,StrStorageMedium_tab,StrDatePrepared0]",
            "DarYearCollected",
            "DarMonthCollected",
            "DarDayCollected",
            "site=SitSiteRef.(SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab])",
            "identifications=[IdeTypeStatus_tab,IdeCurrentNameLocal_tab,identifiers=IdeIdentifiedByRef_nesttab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),IdeDateIdentified0,IdeQualifier_tab,IdeQualifierRank_tab,taxa=TaxTaxonomyRef_tab.(irn,ClaScientificName,ClaKingdom,ClaPhylum,ClaSubphylum,ClaSuperclass,ClaClass,ClaSubclass,ClaSuperorder,ClaOrder,ClaSuborder,ClaInfraorder,ClaSuperfamily,ClaFamily,ClaSubfamily,ClaTribe,ClaSubtribe,ClaGenus,ClaSubgenus,ClaSpecies,ClaSubspecies,ClaRank,AutAuthorString,ClaApplicableCode,comname=[ComName_tab,ComStatus_tab])]",
            "ManOnLoan",
            "location=LocCurrentLocationRef.(irn)",
            "TisTissueUsedUp",
            "GneDnaUsedUp",
            "TisAvailableForLoan",
            "preparedBy=StrPreparedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName)",
            "TisCollectionCode",
            "TisRegistrationNumber"
        };
    }
}