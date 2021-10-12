using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using IMu;
using ImuExports.Config;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AusGeochem.ClassMaps;
using ImuExports.Tasks.AusGeochem.Config;
using ImuExports.Tasks.AusGeochem.Models;
using Serilog;

namespace ImuExports.Tasks.AusGeochem
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
                using (var csvWriter = new CsvWriter(new StreamWriter(this.options.Destination + @"specimens.csv", false, Encoding.UTF8)))
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
            "ColTypeOfItem",
            "AdmDateModified",
            "AdmTimeModified",
            "ColDiscipline",
            "MdaDataSets_tab",
            "colevent=ColCollectionEventRef.(AdmDateModified,AdmTimeModified,ExpExpeditionName,ColCollectionEventCode,ColCollectionMethod,ColDateVisitedFrom,ColDateVisitedTo,ColTimeVisitedTo,ColTimeVisitedFrom,AquDepthToMet,AquDepthFromMet,site=ColSiteRef.(AdmDateModified,AdmTimeModified,SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocIslandGroup,LocIsland,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab]),collectors=ColParticipantRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName))",
            "SpeNoSpecimens",
            "BirTotalClutchSize",
            "SpeSex_tab",
            "SpeStageAge_tab",
            "preparations=[StrSpecimenNature_tab,StrSpecimenForm_tab,StrFixativeTreatment_tab,StrStorageMedium_tab,StrDatePrepared0]",
            "DarYearCollected",
            "DarMonthCollected",
            "DarDayCollected",
            "site=SitSiteRef.(AdmDateModified,AdmTimeModified,SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocIslandGroup,LocIsland,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab])",
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
}