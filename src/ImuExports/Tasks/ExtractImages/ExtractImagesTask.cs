using System;
using System.IO;
using ImageMagick;
using ImuExports.Config;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using Serilog;

namespace ImuExports.Tasks.ExtractImages
{
    public class ExtractImagesTask : ImuTaskBase, ITask
    {
        public void Run()
        {
            using (Log.Logger.BeginTimedOperation(string.Format("{0} starting", GetType().Name), string.Format("{0}.Run", GetType().Name)))
            {
                // Cache Irns
                Log.Logger.Information("Caching data");
                var cachedIrns = this.CacheIrns("emultimedia", BuildSearchTerms());

                // Fetch data
                Log.Logger.Information("Fetching data");

                var offset = 0;
                foreach (var cachedIrn in cachedIrns)
                {
                    if (Program.ImportCanceled)
                        return;

                    try
                    {
                        using (var imuSession = ImuSessionProvider.CreateInstance("emultimedia"))
                        {
                            imuSession.FindKey(cachedIrn);
                            var result = imuSession.Fetch("start", 0, -1, Columns).Rows[0];
                            var fileName = string.Format("{0}{1}.jpg", GlobalOptions.Options.Ei.Destination, Path.GetFileNameWithoutExtension(result.GetTrimString("MulIdentifier")));
                            var resource = result.GetMap("resource");

                            if (File.Exists(fileName) && new FileInfo(fileName).Length > 0)
                            {
                                Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset++, cachedIrns.Count);
                                continue;
                            }

                            using (var fileStream = resource["file"] as FileStream)
                            using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write))
                            using (var image = new MagickImage(fileStream))
                            {
                                image.Format = MagickFormat.Jpg;
                                image.Quality = 96;
                                image.Write(file);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        using (var imuSession = ImuSessionProvider.CreateInstance("emultimedia"))
                        {
                            imuSession.FindKey(cachedIrn);
                            var result = imuSession.Fetch("start", 0, -1, Columns).Rows[0];
                            var fileName = string.Format("{0}{1}", GlobalOptions.Options.Ei.Destination, result.GetTrimString("MulIdentifier"));
                            var resource = result.GetMap("resource");

                            Log.Logger.Error("Error converting image with filename {fileName} saving image to disk instead {ex}", fileName, ex);

                            using (var fileStream = resource["file"] as FileStream)
                            using (var file = File.Open(fileName, FileMode.Create, FileAccess.Write))
                            {
                                fileStream.CopyTo(file);
                            }
                        }                        
                    }

                    Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset++, cachedIrns.Count);
                }
            }
        }

        private Terms BuildSearchTerms()
        {
            var searchTerms = new Terms();

            searchTerms.Add("MdaDataSets_tab", "Google Cultural Institute");

            return searchTerms;
        }

        public string[] Columns
        {
            get
            {
                return new[]
                {
                    "MulIdentifier",
                    "resource"
                };
            }
        }
    }
}