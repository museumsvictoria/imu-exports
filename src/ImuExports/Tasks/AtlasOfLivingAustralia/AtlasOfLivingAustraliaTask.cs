using IMu;
using ImuExports.Tasks.AtlasOfLivingAustralia.Config;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using Microsoft.Extensions.Options;

namespace ImuExports.Tasks.AtlasOfLivingAustralia;

public class AtlasOfLivingAustraliaTask : ImuTaskBase, ITask
{
    private readonly AtlasOfLivingAustraliaOptions _options = (AtlasOfLivingAustraliaOptions)CommandOptions.TaskOptions;
    private readonly AppSettings _appSettings;
    private readonly IFactory<Occurrence> _occurrenceFactory;
    private readonly IEnumerable<IModuleSearchConfig> _moduleSearchConfigs;
    
    public AtlasOfLivingAustraliaTask(
        IOptions<AppSettings> appSettings,
        IFactory<Occurrence> occurrenceFactory,
        IEnumerable<IModuleSearchConfig> moduleSearchConfigs) : base(appSettings)
    {
        _appSettings = appSettings.Value;
        _occurrenceFactory = occurrenceFactory;
        _moduleSearchConfigs = moduleSearchConfigs;
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
            {
                // Cache Irns
                var cachedIrns = new List<long>();
            
                if (_options.ParsedModifiedAfterDate.HasValue ||
                    _options.ParsedModifiedBeforeDate.HasValue)
                {
                    foreach (var moduleSearchConfig in _moduleSearchConfigs)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
            
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
            
                    cachedIrns = (await this.CacheIrns("ecatalogue", this.BuildFullExportSearchTerms(), stoppingToken)).ToList();
                }
            
                // Fetch data
                var occurrences = new List<Occurrence>();
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
            
                        occurrences.AddRange(results.Rows.Select(_occurrenceFactory.Make));
            
                        offset += results.Count;
            
                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                    }
                }
            }
        }, stoppingToken);
    }

    private Terms BuildFullExportSearchTerms()
    {
        var searchTerms = new Terms();
        searchTerms.Add("ColCategory", "Natural Sciences");
        searchTerms.Add("MdaDataSets_tab", AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString);
        searchTerms.Add("AdmPublishWebNoPassword", "Yes");

        if (_options.ParsedModifiedAfterDate.HasValue)
            searchTerms.Add("AdmDateModified", _options.ParsedModifiedAfterDate.Value.ToString("MMM dd yyyy"), ">=");

        if (_options.ParsedModifiedBeforeDate.HasValue)
            searchTerms.Add("AdmDateModified", _options.ParsedModifiedBeforeDate.Value.ToString("MMM dd yyyy"), "<=");

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
        "colevent=ColCollectionEventRef.(AdmDateModified,AdmTimeModified,ExpExpeditionName,ColCollectionEventCode,ColCollectionMethod,ColDateVisitedFrom,ColDateVisitedTo,ColTimeVisitedTo,ColTimeVisitedFrom,AquDepthToMet,AquDepthFromMet,site=ColSiteRef.(AdmDateModified,AdmTimeModified,SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocIslandGroup,LocIsland,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab,LatPreferred_tab]),collectors=ColParticipantRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName))",
        "SpeNoSpecimens",
        "BirTotalClutchSize",
        "SpeSex_tab",
        "SpeStageAge_tab",
        "preparations=[StrSpecimenNature_tab,StrSpecimenForm_tab,StrFixativeTreatment_tab,StrStorageMedium_tab,StrDatePrepared0]",
        "DarYearCollected",
        "DarMonthCollected",
        "DarDayCollected",
        "site=SitSiteRef.(AdmDateModified,AdmTimeModified,SitSiteCode,SitSiteNumber,geo=[LocOcean_tab,LocContinent_tab,LocCountry_tab,LocProvinceStateTerritory_tab,LocIslandGroup,LocIsland,LocDistrictCountyShire_tab,LocTownship_tab],LocPreciseLocation,LocElevationASLFromMt,LocElevationASLToMt,latlong=[LatLongitudeDecimal_nesttab,LatLatitudeDecimal_nesttab,LatRadiusNumeric_tab,LatDatum_tab,determinedBy=LatDeterminedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName),LatDetDate0,LatLatLongDetermination_tab,LatDetSource_tab,LatPreferred_tab])",
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