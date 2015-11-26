using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.CsvMaps;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using Serilog;

namespace ImuExports.Tasks.AtlasOfLivingAustralia
{
    public class AtlasOfLivingAustraliaTask : ITask
    {
        private readonly IFactory<Occurrence> occurrenceFactory;
        private readonly IImuSessionProvider imuSessionProvider;

        public AtlasOfLivingAustraliaTask(IFactory<Occurrence> occurrenceFactory,
            IImuSessionProvider imuSessionProvider)
        {
            this.occurrenceFactory = occurrenceFactory;
            this.imuSessionProvider = imuSessionProvider;
        }

        public void Run()
        {
            using (Log.Logger.BeginTimedOperation(string.Format("{0} starting", GetType().Name), "AtlasOfLivingAustraliaTask.Run"))
            {
                var cachedIrns = new List<long>();
                var occurrences = new List<Occurrence>();
                var offset = 0;

                // Cache Irns
                using (var imuSession = imuSessionProvider.CreateInstance("ecatalogue"))
                {
                    Log.Logger.Information("Caching data");

                    var terms = BuildTerms();
                    var hits = imuSession.FindTerms(terms);

                    Log.Logger.Information("Found {Hits} records where {@Terms}", hits, terms.List);
                    
                    while (true)
                    {
                        if (Program.ImportCanceled)
                            return;

                        var results = imuSession.Fetch("start", offset, Constants.CachedDataBatchSize, new[] { "irn" });

                        if (results.Count == 0)
                            break;

                        var irns = results.Rows.Select(x => long.Parse(x.GetEncodedString("irn"))).ToList();
                        
                        cachedIrns.AddRange(irns);

                        offset += results.Count;

                        Log.Logger.Information("AtlasOfLivingAustraliaTask cache progress... {Offset}/{TotalResults}", offset, hits);
                    }
                }

                // Fetch data
                offset = 0;
                Log.Logger.Information("Fetching data");
                while (true)
                {
                    if (Program.ImportCanceled)
                        return;

                    using (var imuSession = imuSessionProvider.CreateInstance("ecatalogue"))
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
                using (var csvWriter = new CsvWriter(new StreamWriter(CommandLineConfig.Options.Ala.Destination + @"occurrences.csv", false, Encoding.UTF8)))
                {
                    csvWriter.Configuration.RegisterClassMap<OccurrenceCsvMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.Configuration.DoubleQuoteString = @"\""";
                    csvWriter.WriteRecords(occurrences);
                }

                Log.Logger.Information("Saving image data as csv");
                using (var csvWriter = new CsvWriter(new StreamWriter(CommandLineConfig.Options.Ala.Destination + @"images.csv", false, Encoding.UTF8)))
                {
                    var images = occurrences.SelectMany(x => x.Images);

                    csvWriter.Configuration.RegisterClassMap<ImageCsvMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.Configuration.DoubleQuoteString = @"\""";
                    csvWriter.WriteRecords(images);
                }

                // Copy meta.xml
                File.Copy(@"meta.xml", CommandLineConfig.Options.Ala.Destination + @"meta.xml", true);
            }
        }

        private Terms BuildTerms()
        {
            var terms = new Terms();
            terms.Add("ColCategory", "Natural Sciences");
            terms.Add("MdaDataSets_tab", "Website - Atlas of Living Australia");

            DateTime modifiedAfterDate;
            if (!string.IsNullOrWhiteSpace(CommandLineConfig.Options.Ala.ModifiedAfterDate) &&
                DateTime.TryParseExact(CommandLineConfig.Options.Ala.ModifiedAfterDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out modifiedAfterDate))
            {
                terms.Add("AdmDateModified", modifiedAfterDate.ToString("MMM dd yyyy"), ">=");
            }

            DateTime modifiedBeforeDate;
            if (!string.IsNullOrWhiteSpace(CommandLineConfig.Options.Ala.ModifiedBeforeDate) &&
                DateTime.TryParseExact(CommandLineConfig.Options.Ala.ModifiedBeforeDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out modifiedBeforeDate))
            {
                terms.Add("AdmDateModified", modifiedBeforeDate.ToString("MMM dd yyyy"), "<=");
            }

            return terms;
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