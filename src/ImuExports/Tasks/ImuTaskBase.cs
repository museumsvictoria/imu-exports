using System;
using System.Collections.Generic;
using System.Linq;
using IMu;
using ImuExports.Config;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using Serilog;

namespace ImuExports.Tasks
{
    public abstract class ImuTaskBase
    {
        public IList<long> CacheIrns(string moduleName, Terms searchTerms)
        {
            var cachedIrns = new List<long>();
            var offset = 0;

            // Cache Irns
            using (var imuSession = ImuSessionProvider.CreateInstance(moduleName))
            {
                Log.Logger.Information("Caching irns");

                var hits = imuSession.FindTerms(searchTerms);

                Log.Logger.Information("Found {Hits} records where {@Terms}", hits, searchTerms.List);

                while (true)
                {
                    if (Program.ImportCanceled)
                        return cachedIrns;

                    var results = imuSession.Fetch("start", offset, Constants.CachedDataBatchSize, new[] { "irn" });

                    if (results.Count == 0)
                        break;

                    var irns = results.Rows.Select(x => long.Parse(x.GetEncodedString("irn"))).ToList();

                    cachedIrns.AddRange(irns);

                    offset += results.Count;

                    Log.Logger.Information("{Name} cache progress... {Offset}/{TotalResults}", this.GetType().Name, offset, hits);
                }
            }

            return cachedIrns;
        }
    }
}
