using System.Collections.Generic;
using System.IO;
using System.Linq;
using IMu;
using ImuExports.Config;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldGuideGippsland.Models;
using Newtonsoft.Json;
using Serilog;

namespace ImuExports.Tasks.FieldGuideGippsland
{
    public class FieldGuideGippslandTask : ImuTaskBase, ITask
    {
        private readonly IFactory<Species> speciesFactory;

        public FieldGuideGippslandTask(IFactory<Species> speciesFactory)
        {
            this.speciesFactory = speciesFactory;
        }

        public void Run()
        {
            using (Log.Logger.BeginTimedOperation(string.Format("{0} starting", GetType().Name), string.Format("{0}.Run", GetType().Name)))
            {
                // Cache Irns
                var cachedIrns = this.CacheIrns("enarratives", BuildSearchTerms());

                // Fetch data
                var species = new List<Species>();
                var offset = 0;
                Log.Logger.Information("Fetching data");
                while (true)
                {
                    if (Program.ImportCanceled)
                        return;

                    using (var imuSession = ImuSessionProvider.CreateInstance("enarratives"))
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

                        species.AddRange(results.Rows.Select(speciesFactory.Make));

                        offset += results.Count;

                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                    }
                }

                // Save data
                File.WriteAllText(string.Format("{0}export.json", Config.Config.Options.Fgg.Destination), JsonConvert.SerializeObject(species, Formatting.Indented));
            }
        }

        private Terms BuildSearchTerms()
        {
            var searchTerms = new Terms();
            
            searchTerms.Add("DetPurpose_tab", "App: Gippsland");
            searchTerms.Add("AdmPublishWebNoPassword", "Yes");

            return searchTerms;
        }

        public string[] Columns
        {
            get
            {
                return new[]
                {
                    "irn",
                    "AdmPublishWebNoPassword",
                    "AdmDateModified",
                    "AdmTimeModified",                    
                    "taxa=[TaxTaxaRef_tab.(comname=[ComName_tab,ComStatus_tab]),TaxTaxaNotes_tab]",
                    "SpeTaxonGroup",
                    "SpeTaxonSubGroup",
                    "SpeBriefID",
                    "SpeIdentifyingCharacters",
                    "SpeHabitatNotes",
                    "SpeDistribution",
                    "SpeBiology",
                    "SpeDiet",
                    "SpeFastFact",
                    "SpeHazards",
                    "SpeFlightStart",
                    "SpeFlightEnd",
                    "SpeDepth_tab",
                    "SpeWaterColumnLocation_tab",
                    "SpeColour_tab",
                    "SpeHabitat_tab",
                    "SpeMaximumSize",
                    "authors=NarAuthorsRef_tab.(NamPartyType,NamFullName,NamOrganisation,ColCollaborationName)",
                    "media=MulMultiMediaRef_tab.(irn,MulTitle,MulIdentifier,MulMimeType,MdaDataSets_tab,metadata=[MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab],DetAlternateText,RigCreator_tab,RigSource_tab,RigAcknowledgementCredit,RigCopyrightStatement,RigCopyrightStatus,RigLicence,RigLicenceDetails,ChaRepository_tab,ChaMd5Sum,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)"
                };
            }
        }
    }
}