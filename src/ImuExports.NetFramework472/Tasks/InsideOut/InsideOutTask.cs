using System.Collections.Generic;
using System.IO;
using System.Linq;
using IMu;
using ImuExports.NetFramework472.Config;
using ImuExports.NetFramework472.Infrastructure;
using ImuExports.NetFramework472.Tasks.InsideOut.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace ImuExports.NetFramework472.Tasks.InsideOut
{
    public class InsideOutTask : ImuTaskBase, ITask
    {
        private readonly IFactory<Object> objectFactory;

        public InsideOutTask(IFactory<Object> objectFactory)
        {
            this.objectFactory = objectFactory;
        }

        public void Run()
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
            {
                // Cache Irns
                var cachedIrns = this.CacheIrns("enarratives", BuildSearchTerms());

                // Fetch data
                var objects = new List<Object>();
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

                        objects.AddRange(results.Rows.Select(objectFactory.Make));

                        offset += results.Count;

                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedIrns.Count);
                    }
                }

                // Save data
                var filename = $"{GlobalOptions.Options.Io.Destination}export.json";
                if (File.Exists(filename))
                {
                    File.SetAttributes(filename, FileAttributes.Normal);
                }

                File.WriteAllText(filename, JsonConvert.SerializeObject(objects, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }));
            }
        }

        private static Terms BuildSearchTerms()
        {
            var searchTerms = new Terms();
            
            searchTerms.Add("DetPurpose_tab", "Exhibition: MIO Act 1");
            searchTerms.Add("AdmPublishWebNoPassword", "Yes");

            return searchTerms;
        }

        private static string[] Columns => new[]
        {
            "irn",
            "AdmPublishWebNoPassword",
            "AdmDateModified",
            "AdmTimeModified",
            "DetPurpose_tab",
            "DetNarrativeIdentifier",
            "DetVersion",
            "NarTitle",
            "NarNarrative",
            "IntInterviewNotes_tab",
            "emv=ObjObjectsRef_tab.(irn,ColCategory)",
            "media=MulMultiMediaRef_tab.(irn,MulTitle,MulIdentifier,MulMimeType,MulDescription,MdaDataSets_tab,metadata=[MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab],DetAlternateText,RigCreator_tab,RigSource_tab,RigAcknowledgementCredit,RigCopyrightStatement,RigCopyrightStatus,RigLicence,RigLicenceDetails,ChaRepository_tab,ChaMd5Sum,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)"
        };
    }
}