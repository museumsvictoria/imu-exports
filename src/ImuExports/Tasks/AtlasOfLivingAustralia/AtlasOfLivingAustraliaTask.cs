using CsvHelper;
using IMu;
using ImuExports.Config;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImuExports.Tasks.AtlasOfLivingAustralia
{
    public class AtlasOfLivingAustraliaTask : ImuTaskBase, ITask
    {
        private readonly IFactory<Occurrence> occurrenceFactory;
        private readonly IEnumerable<IModuleSearchConfig> moduleSearchConfigs;

        public AtlasOfLivingAustraliaTask(
            IFactory<Occurrence> occurrenceFactory,
            IEnumerable<IModuleSearchConfig> moduleSearchConfigs)
        {
            this.occurrenceFactory = occurrenceFactory;
            this.moduleSearchConfigs = moduleSearchConfigs;
        }

        public void Run()
        {            
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
            {
                // Cache Irns
                var cachedIrns = new List<long>();

                if (GlobalOptions.Options.Ala.ParsedModifiedAfterDate.HasValue ||
                    GlobalOptions.Options.Ala.ParsedModifiedBeforeDate.HasValue)
                {
                    foreach (var moduleSearchConfig in this.moduleSearchConfigs)
                    {
                        if (Program.ImportCanceled)
                        {
                            this.Cleanup();
                            return;
                        }

                        cachedIrns.AddRange(this.CacheIrns(moduleSearchConfig.ModuleName, moduleSearchConfig.Terms, moduleSearchConfig.Columns, moduleSearchConfig.IrnSelectFunc));
                    }
                }
                else
                {
                    if (Program.ImportCanceled)
                    {
                        this.Cleanup();
                        return;
                    }

                    cachedIrns = this.CacheIrns("ecatalogue", this.BuildFullExportSearchTerms()).ToList();
                }

                // Fetch data
                var occurrences = new List<Occurrence>();
                var offset = 0;
                Log.Logger.Information("Fetching data");
                while (true)
                {
                    if (Program.ImportCanceled)
                    {
                        this.Cleanup();
                        return;
                    }

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

                        occurrences.AddRange(results.Rows.Select(occurrenceFactory.Make));

                        offset += results.Count;

                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                    }
                }

                // Save data
                Log.Logger.Information("Saving occurrence data as csv");
                using (var csvWriter = new CsvWriter(new StreamWriter(GlobalOptions.Options.Ala.Destination + @"occurrences.csv", false, Encoding.UTF8)))
                {
                    csvWriter.Configuration.RegisterClassMap<OccurrenceClassMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.Configuration.SanitizeForInjection = false;
                    csvWriter.WriteRecords(occurrences);
                }

                Log.Logger.Information("Saving multimedia data as csv");
                using (var csvWriter = new CsvWriter(new StreamWriter(GlobalOptions.Options.Ala.Destination + @"multimedia.csv", false, Encoding.UTF8)))
                {
                    var multimedia = occurrences.SelectMany(x => x.Multimedia);

                    csvWriter.Configuration.RegisterClassMap<MultimediaClassMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.Configuration.SanitizeForInjection = false;
                    csvWriter.WriteRecords(multimedia);
                }

                // Copy meta.xml
                File.Copy(@"meta.xml", GlobalOptions.Options.Ala.Destination + @"meta.xml", true);
            }
        }

        private void Cleanup()
        {
            // Remove any temporary files and directory
            if (GlobalOptions.Options.Ala.IsAutomated)
            {
                Directory.Delete(GlobalOptions.Options.Ala.Destination, true);
            }
        }

        private Terms BuildFullExportSearchTerms()
        {
            var searchTerms = new Terms();
            searchTerms.Add("ColCategory", "Natural Sciences");
            searchTerms.Add("MdaDataSets_tab", "Atlas of Living Australia");
            searchTerms.Add("AdmPublishWebNoPassword", "Yes");

            if (GlobalOptions.Options.Ala.ParsedModifiedAfterDate.HasValue)
            {
                searchTerms.Add("AdmDateModified", GlobalOptions.Options.Ala.ParsedModifiedAfterDate.Value.ToString("MMM dd yyyy"), ">=");
            }

            if (GlobalOptions.Options.Ala.ParsedModifiedBeforeDate.HasValue)
            {
                searchTerms.Add("AdmDateModified", GlobalOptions.Options.Ala.ParsedModifiedBeforeDate.Value.ToString("MMM dd yyyy"), "<=");
            }

            return searchTerms;
        }
        
        private string[] ExportColumns => new []
        {
            "irn",
            "ColRegPrefix",
            "ColRegNumber",
            "ColRegPart",
            "ColTypeOfItem",
            "AdmDateModified",
            "AdmTimeModified",
            "ColDiscipline",
            "colevent=ColCollectionEventRef.(ExpExpeditionName,ColCollectionEventCode,ColCollectionMethod,ColDateVisitedFrom,ColDateVisitedTo,ColTimeVisitedTo,ColTimeVisitedFrom,AquDepthToMet,AquDepthFromMet,site=ColSiteRef.(SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocIslandGroup,LocIsland,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab]),collectors=ColParticipantRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName))",
            "SpeNoSpecimens",
            "BirTotalClutchSize",
            "SpeSex_tab",
            "SpeStageAge_tab",
            "preparations=[StrSpecimenNature_tab,StrSpecimenForm_tab,StrFixativeTreatment_tab,StrStorageMedium_tab,StrDatePrepared0]",
            "DarYearCollected",
            "DarMonthCollected",
            "DarDayCollected",
            "site=SitSiteRef.(SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocIslandGroup,LocIsland,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab])",
            "identifications=[IdeTypeStatus_tab,IdeCurrentNameLocal_tab,identifiers=IdeIdentifiedByRef_nesttab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),IdeDateIdentified0,IdeQualifier_tab,IdeQualifierRank_tab,taxa=TaxTaxonomyRef_tab.(irn,ClaScientificName,ClaKingdom,ClaPhylum,ClaSubphylum,ClaSuperclass,ClaClass,ClaSubclass,ClaSuperorder,ClaOrder,ClaSuborder,ClaInfraorder,ClaSuperfamily,ClaFamily,ClaSubfamily,ClaTribe,ClaSubtribe,ClaGenus,ClaSubgenus,ClaSpecies,ClaSubspecies,ClaRank,AutAuthorString,ClaApplicableCode,comname=[ComName_tab,ComStatus_tab])]",
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
            "preparedby=StrPreparedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName)",
        };
    }
}
