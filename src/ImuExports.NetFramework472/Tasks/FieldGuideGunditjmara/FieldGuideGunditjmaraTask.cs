﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using IMu;
using ImuExports.NetFramework472.Config;
using ImuExports.NetFramework472.Infrastructure;
using ImuExports.NetFramework472.Tasks.FieldGuideGunditjmara.Models;
using Newtonsoft.Json;
using Serilog;

namespace ImuExports.NetFramework472.Tasks.FieldGuideGunditjmara
{
    public class FieldGuideGunditjmaraTask : ImuTaskBase, ITask
    {
        private readonly IFactory<GunditjmaraSpecies> gunditjmaraSpeciesFactory;

        public FieldGuideGunditjmaraTask(IFactory<GunditjmaraSpecies> gunditjmaraSpeciesFactory)
        {
            this.gunditjmaraSpeciesFactory = gunditjmaraSpeciesFactory;
        }

        public void Run()
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
            {
                // Cache Irns
                var cachedIrns = this.CacheIrns("enarratives", BuildSearchTerms());

                // Fetch data
                var species = new List<GunditjmaraSpecies>();
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

                        species.AddRange(results.Rows.Select(gunditjmaraSpeciesFactory.Make));

                        offset += results.Count;

                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                    }
                }

                // Save data
                File.WriteAllText($"{GlobalOptions.Options.Gun.Destination}export.json", JsonConvert.SerializeObject(species, Formatting.Indented));
            }
        }

        private static Terms BuildSearchTerms()
        {
            var searchTerms = new Terms();
            
            searchTerms.Add("DetPurpose_tab", "App: Gunditjmara");
            searchTerms.Add("AdmPublishWebNoPassword", "Yes");

            return searchTerms;
        }

        private static string[] Columns => new[]
        {
            "irn",
            "AdmPublishWebNoPassword",
            "AdmDateModified",
            "AdmTimeModified",                    
            "taxa=[TaxTaxaRef_tab.(comname=[ComName_tab,ComStatus_tab]),TaxTaxaNotes_tab]",
            "SpeGroup",
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
            "SpeCallTimeFromTo_tab",
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