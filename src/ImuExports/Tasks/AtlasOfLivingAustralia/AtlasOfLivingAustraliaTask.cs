using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CommandLine;
using CsvHelper;
using IMu;
using ImuExports.Config;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps;
using ImuExports.Tasks.AtlasOfLivingAustralia.Config;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using LiteDB;
using Renci.SshNet;
using Serilog;
using FileMode = System.IO.FileMode;

namespace ImuExports.Tasks.AtlasOfLivingAustralia
{
    public class AtlasOfLivingAustraliaTask : ImuTaskBase, ITask
    {
        private readonly IFactory<Occurrence> occurrenceFactory;
        private readonly IEnumerable<IModuleSearchConfig> moduleSearchConfigs;
        private readonly AtlasOfLivingAustraliaOptions options = GlobalOptions.Options.Ala;

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

                if (this.options.ParsedModifiedAfterDate.HasValue ||
                    this.options.ParsedModifiedBeforeDate.HasValue)
                {
                    foreach (var moduleSearchConfig in this.moduleSearchConfigs)
                    {
                        if (Program.ImportCanceled)
                        {
                            this.Cleanup();
                            return;
                        }

                        var irns = CacheIrns(moduleSearchConfig.ModuleName,
                            moduleSearchConfig.ModuleSelectName,
                            moduleSearchConfig.Terms,
                            moduleSearchConfig.Columns,
                            moduleSearchConfig.IrnSelectFunc);

                        cachedIrns.AddRange(irns);
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
                using (var csvWriter = new CsvWriter(new StreamWriter(this.options.Destination + @"occurrences.csv", false, Encoding.UTF8)))
                {
                    csvWriter.Configuration.RegisterClassMap<OccurrenceClassMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.Configuration.SanitizeForInjection = false;
                    csvWriter.WriteRecords(occurrences);
                }

                Log.Logger.Information("Saving multimedia data as csv");
                using (var csvWriter = new CsvWriter(new StreamWriter(this.options.Destination + @"multimedia.csv", false, Encoding.UTF8)))
                {
                    var multimedia = occurrences.SelectMany(x => x.Multimedia);

                    csvWriter.Configuration.RegisterClassMap<MultimediaClassMap>();
                    csvWriter.Configuration.HasHeaderRecord = true;
                    csvWriter.Configuration.SanitizeForInjection = false;
                    csvWriter.WriteRecords(multimedia);
                }

                // Copy meta.xml
                Log.Logger.Information("Copying meta.xml");
                File.Copy(@"meta.xml", this.options.Destination + @"meta.xml", true);
                
                // Compress/Upload files to ALA if automated export
                if (this.options.IsAutomated)
                {
                    // Determine filename
                    string startDate = null; 
                    string endDate;

                    if (this.options.ParsedModifiedAfterDate.HasValue)
                    {
                        startDate = this.options.ParsedModifiedAfterDate?.ToString("yyyy-MM-dd");
                    }
                    
                    if (this.options.ParsedModifiedBeforeDate.HasValue)
                    {
                        endDate =
                            this.options.ParsedModifiedBeforeDate <=
                            this.options.DateStarted
                                ? this.options.ParsedModifiedBeforeDate?.ToString("yyyy-MM-dd")
                                : this.options.DateStarted.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        endDate = this.options.DateStarted.ToString("yyyy-MM-dd");
                    }
                    
                    var zipFilename = startDate != null ? 
                         $"mv-dwca-{startDate}-to-{endDate}.zip"
                        : $"mv-dwca-{endDate}.zip";
                            
                    var tempFilepath = $"{Path.GetTempPath()}{Utils.RandomString(8)}.tmp";
                    var stopwatch = Stopwatch.StartNew();
                    
                    try
                    {
                        // Zip Directory
                        ZipFile.CreateFromDirectory(this.options.Destination, tempFilepath,
                            CompressionLevel.NoCompression, false);
                        Log.Logger.Information(
                            "Created temporary zip file {tempFilepath} in {Elapsed} ({ElapsedMilliseconds} ms)",
                            tempFilepath,
                            stopwatch.Elapsed, stopwatch.ElapsedMilliseconds);

                        // Delete uncompressed files
                        stopwatch.Restart();
                        Directory.EnumerateFiles(this.options.Destination).ToList().ForEach(File.Delete);
                        Log.Logger.Information(
                            "Deleted uncompressed files in {Destination} in {Elapsed} ({ElapsedMilliseconds} ms)",
                            this.options.Destination, stopwatch.Elapsed, stopwatch.ElapsedMilliseconds);

                        // Move zip file
                        stopwatch.Restart();
                        File.Move(tempFilepath, $"{this.options.Destination}{zipFilename}");
                        Log.Logger.Information(
                            "Moved zip file {zipFilename} to {Destination} in {Elapsed} ({ElapsedMilliseconds} ms)",
                            zipFilename, this.options.Destination, stopwatch.Elapsed,
                            stopwatch.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        // Log and cleanup before exit
                        Log.Logger.Fatal(ex, "Error creating zip archive");
                        Cleanup();
                        Environment.Exit(Parser.DefaultExitCodeFail);
                    }

                    try
                    {
                        // Upload files
                        using (var client = new SftpClient(ConfigurationManager.AppSettings["AtlasOfLivingAustralia:SFTP:Host"],
                            22, ConfigurationManager.AppSettings["AtlasOfLivingAustralia:SFTP:Username"],
                            ConfigurationManager.AppSettings["AtlasOfLivingAustralia:SFTP:Password"]))
                        {
                            Log.Logger.Information("Connecting to sftp server {Host}", ConfigurationManager.AppSettings["AtlasOfLivingAustralia:SFTP:Host"]);
                            client.Connect();
                            
                            stopwatch.Restart();
                            using (var fileStream = new FileStream($"{this.options.Destination}{zipFilename}", FileMode.Open))
                            {
                                Log.Logger.Information(
                                    "Uploading zip {zipFilename} ({Length})", zipFilename, Utils.BytesToString(fileStream.Length));
                                client.BufferSize = 4 * 1024; // bypass Payload error large files
                                client.UploadFile(fileStream, zipFilename);
                            }
                            stopwatch.Stop();
                            Log.Logger.Information(
                                "Uploaded {zipFilename} in {Elapsed} ({ElapsedMilliseconds} ms)",
                                zipFilename, stopwatch.Elapsed, stopwatch.ElapsedMilliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log and cleanup before exit
                        Log.Logger.Fatal(ex, "Error uploading zip archive");
                        Cleanup();
                        Environment.Exit(Parser.DefaultExitCodeFail);
                    }

                    // Update/Insert application
                    using (var db = new LiteRepository(ConfigurationManager.ConnectionStrings["LiteDB"].ConnectionString))
                    {
                        var application = this.options.Application;
                
                        if (application != null)
                        {
                            Log.Logger.Information("Updating ALA Application PreviousDateRun {PreviousDateRun} to {DateStarted}", application.PreviousDateRun, this.options.DateStarted);
                            application.PreviousDateRun = this.options.DateStarted;
                            db.Upsert(application);
                        }
                    }
                    
                    // Finished successfully so run cleanup
                    Cleanup();
                }
            }
        }

        private void Cleanup()
        {
            // Remove any temporary files and directory if running automated export
            if (options.IsAutomated)
            {
                Log.Logger.Information("Deleting temporary directory {Destination}", options.Destination);
                Directory.Delete(options.Destination, true);
            }
        }

        private Terms BuildFullExportSearchTerms()
        {
            var searchTerms = new Terms();
            searchTerms.Add("ColCategory", "Natural Sciences");
            searchTerms.Add("MdaDataSets_tab", AtlasOfLivingAustraliaConstants.QueryString);
            searchTerms.Add("AdmPublishWebNoPassword", "Yes");

            if (options.ParsedModifiedAfterDate.HasValue)
            {
                searchTerms.Add("AdmDateModified", options.ParsedModifiedAfterDate.Value.ToString("MMM dd yyyy"), ">=");
            }

            if (options.ParsedModifiedBeforeDate.HasValue)
            {
                searchTerms.Add("AdmDateModified", options.ParsedModifiedBeforeDate.Value.ToString("MMM dd yyyy"), "<=");
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
            "preparedby=StrPreparedByRef_tab.(NamPartyType,NamFullName,NamOrganisation,NamBranch,NamDepartment,NamOrganisation,NamOrganisationOtherNames_tab,NamSource,AddPhysStreet,AddPhysCity,AddPhysState,AddPhysCountry,ColCollaborationName)",
        };
    }
}
