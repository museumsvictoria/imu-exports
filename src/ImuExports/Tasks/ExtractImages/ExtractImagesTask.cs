using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using ImuExports.Config;
using IMu;
using ImuExports.Infrastructure;
using Serilog;

namespace ImuExports.Tasks.ExtractImages
{
    public class ExtractImagesTask : ImuTaskBase, ITask
    {
        public void Run()
        {
            using (Log.Logger.BeginTimedOperation($"{GetType().Name} starting", $"{GetType().Name}.Run"))
            {
                // Cache Narrative Irns
                Log.Logger.Information("Caching narrative irns");
                var cachedNarrativeIrns = this.CacheIrns("enarratives", BuildNarrativeSearchTerms());
                
                var cachedMultimediaIrns = new List<long>();
                using (var imuSession = ImuSessionProvider.CreateInstance("enarratives"))
                {
                    imuSession.FindKeys(cachedNarrativeIrns.ToList());
                    var result = imuSession.Fetch("start", 0, -1, NarrativeColumns);
                    
                    cachedMultimediaIrns.AddRange(result.Rows.SelectMany(row => row.GetMaps("media").Select(mm => mm.GetLong("irn"))));
                                        
                    Log.Logger.Information("Found {Count} emultimedia records", cachedMultimediaIrns.Count);
                }

                var offset = 0;
                Log.Logger.Information("Fetching images");
                while (true)
                {
                    if (Program.ImportCanceled)
                    {
                        return;
                    }

                    using (var imuSession = ImuSessionProvider.CreateInstance("emultimedia"))
                    {
                        var multimediaIrnsBatch = cachedMultimediaIrns
                            .Skip(offset)
                            .Take(Constants.DataBatchSize)
                            .ToList();
                        
                        if (multimediaIrnsBatch.Count == 0)
                            break;

                        imuSession.FindKeys(multimediaIrnsBatch);

                        var results = imuSession.Fetch("start", 0, -1, this.MultimediaColumns);
                        
                        Log.Logger.Debug("Fetched {RecordCount} emultimedia records from Imu", multimediaIrnsBatch.Count);
                        
                        foreach (var row in results.Rows)
                        {
                            var fileName = $"{GlobalOptions.Options.Ei.Destination}{row.GetLong("irn")}.jpg";
                            var resource = row.GetMap("resource");
                            
                            if (File.Exists(fileName))
                            {
                                continue;
                            }
                            
                            using (var fileStream = resource["file"] as FileStream)
                            using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write))
                            using (var image = new MagickImage(fileStream))
                            {
                                image.Format = MagickFormat.Jpg;
                                image.Quality = 96;
                                image.FilterType = FilterType.Triangle;
                                image.ColorSpace = ColorSpace.sRGB;
                                image.Resize(new MagickGeometry(2048) { Greater = true });
                                image.UnsharpMask(0.25, 0.08, 8.3, 0.045);
                                
                                image.Write(file);
                            }
                        }
                        
                        offset += results.Count;

                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset, cachedMultimediaIrns.Count);
                    }
                }
            }
            

        }

        private Terms BuildNarrativeSearchTerms()
        {
            var searchTerms = new Terms();

            searchTerms.Add("DetNarrativeIdentifier", "2021 extras1");

            return searchTerms;
        }

        private string[] NarrativeColumns => new[]
        {
            "media=MulMultiMediaRef_tab.(irn)",
        };

        private string[] MultimediaColumns => new[]
        {
            "resource"
        };
    }
}