using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using IMu;
using ImuExports.Config;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.CsvMaps;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using Serilog;

namespace ImuExports.Tasks.AtlasOfLivingAustralia
{
    public class AtlasOfLivingAustraliaTask : ImuTaskBase, ITask
    {
        private readonly IFactory<Occurrence> occurrenceFactory;

        public AtlasOfLivingAustraliaTask(IFactory<Occurrence> occurrenceFactory)
        {
            this.occurrenceFactory = occurrenceFactory;
        }

        public void Run()
        {
            using (Log.Logger.BeginTimedOperation(string.Format("{0} starting", GetType().Name), string.Format("{0}.Run", GetType().Name)))
            {
                // Cache Irns
                var cachedIrns = this.CacheIrns("ecatalogue", BuildSearchTerms());

                // Fetch data
                var occurrences = new List<Occurrence>();
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

                        if (cachedIrnsBatch.Count == 0)
                            break;

                        imuSession.FindKeys(cachedIrnsBatch);

                        var results = imuSession.Fetch("start", 0, -1, Columns);

                        Log.Logger.Debug("Fetched {RecordCount} records from Imu", cachedIrnsBatch.Count);

                        occurrences.AddRange(results.Rows.Select(occurrenceFactory.Make));

                        offset += results.Count;

                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                    }
                }

                // Save data
                Log.Logger.Information("Saving occurrence data as csv");
                using (var csvWriter = new CsvWriter(new StreamWriter(Config.Config.Options.Ala.Destination + @"occurrences.csv", false, Encoding.UTF8)))
                {
                    csvWriter.Configuration.RegisterClassMap<OccurrenceCsvMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.Configuration.DoubleQuoteString = @"\""";
                    csvWriter.WriteRecords(occurrences);
                }

                Log.Logger.Information("Saving image data as csv");
                using (var csvWriter = new CsvWriter(new StreamWriter(Config.Config.Options.Ala.Destination + @"images.csv", false, Encoding.UTF8)))
                {
                    var images = occurrences.SelectMany(x => x.Images);

                    csvWriter.Configuration.RegisterClassMap<ImageCsvMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.Configuration.DoubleQuoteString = @"\""";
                    csvWriter.WriteRecords(images);
                }

                // Copy meta.xml
                File.Copy(@"meta.xml", Config.Config.Options.Ala.Destination + @"meta.xml", true);
            }
        }

        private Terms BuildSearchTerms()
        {
            var searchTerms = new Terms();
            searchTerms.Add("ColCategory", "Natural Sciences");
            searchTerms.Add("MdaDataSets_tab", "Website - Atlas of Living Australia");

            DateTime modifiedAfterDate;
            if (!string.IsNullOrWhiteSpace(Config.Config.Options.Ala.ModifiedAfterDate) && DateTime.TryParseExact(Config.Config.Options.Ala.ModifiedAfterDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out modifiedAfterDate))
            {
                searchTerms.Add("AdmDateModified", modifiedAfterDate.ToString("MMM dd yyyy"), ">=");
            }

            DateTime modifiedBeforeDate;
            if (!string.IsNullOrWhiteSpace(Config.Config.Options.Ala.ModifiedBeforeDate) && DateTime.TryParseExact(Config.Config.Options.Ala.ModifiedBeforeDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out modifiedBeforeDate))
            {
                searchTerms.Add("AdmDateModified", modifiedBeforeDate.ToString("MMM dd yyyy"), "<=");
            }

            return searchTerms;
        }

        private string[] Columns
        {
            get
            {
                return new []
                {
                    "irn",
                    "ColRegPrefix",
                    "ColRegNumber",
                    "ColRegPart",
                    "ColTypeOfItem",
                    "AdmDateModified",
                    "AdmTimeModified",
                    "ColDiscipline",
                    "colevent=ColCollectionEventRef.(ExpExpeditionName,ColCollectionEventCode,ColCollectionMethod,ColDateVisitedFrom,ColDateVisitedTo,ColTimeVisitedTo,ColTimeVisitedFrom,AquDepthToMet,AquDepthFromMet,site=ColSiteRef.(SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab]),collectors=ColParticipantRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName))",
                    "SpeNoSpecimens",
                    "BirTotalClutchSize",
                    "SpeSex_tab",
                    "SpeStageAge_tab",
                    "preparations=[StrSpecimenNature_tab,StrSpecimenForm_tab,StrFixativeTreatment_tab,StrStorageMedium_tab]",
                    "DarYearCollected",
                    "DarMonthCollected",
                    "DarDayCollected",
                    "site=SitSiteRef.(SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab])",
                    "identifications=[IdeTypeStatus_tab,IdeCurrentNameLocal_tab,identifiers=IdeIdentifiedByRef_nesttab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),IdeDateIdentified0,IdeQualifier_tab,IdeQualifierRank_tab,taxa=TaxTaxonomyRef_tab.(irn,ClaScientificName,ClaKingdom,ClaPhylum,ClaSubphylum,ClaSuperclass,ClaClass,ClaSubclass,ClaSuperorder,ClaOrder,ClaSuborder,ClaInfraorder,ClaSuperfamily,ClaFamily,ClaSubfamily,ClaTribe,ClaSubtribe,ClaGenus,ClaSubgenus,ClaSpecies,ClaSubspecies,ClaRank,AutAuthorString,ClaApplicableCode,comname=[ComName_tab,ComStatus_tab])]",
                    "media=MulMultiMediaRef_tab.(irn,MulTitle,MulDescription,MulMimeType,MulCreator_tab,MdaDataSets_tab,credit=<erights:MulMultiMediaRef_tab>.(RigAcknowledgement,RigType),AdmPublishWebNoPassword)"
                };
            }
        }
    }
}