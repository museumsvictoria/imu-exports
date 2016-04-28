using System.IO;
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
                    
                    using (var imuSession = ImuSessionProvider.CreateInstance("emultimedia"))
                    {
                        imuSession.FindKey(cachedIrn);

                        var result = imuSession.Fetch("start", 0, -1, Columns).Rows[0];

                        var fileName = result.GetEncodedString("MulIdentifier");
                        var resource = result.GetMap("resource");

                        if (resource == null)
                            throw new IMuException("MultimediaResourceNotFound");

                        using (var fileStream = resource["file"] as FileStream)
                        using (var file = File.Open(string.Format("{0}{1}", Config.Config.Options.Ei.Destination, fileName), FileMode.Create, FileAccess.Write))
                        {
                            fileStream.CopyTo(file);
                        }

                        Log.Logger.Information("Import progress... {Offset}/{TotalResults}", offset++, cachedIrns.Count);
                    }
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