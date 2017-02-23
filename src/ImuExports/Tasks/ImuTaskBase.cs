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
        protected IList<long> CacheIrns(string moduleName, Terms searchTerms)
        {
            var cachedIrns = new List<long>();
            var offset = 0;

            // Cache Irns
            using (var imuSession = ImuSessionProvider.CreateInstance(moduleName))
            {
                Log.Logger.Information("Caching {moduleName} irns", moduleName);

                var hits = imuSession.FindTerms(searchTerms);

                Log.Logger.Information("Found {Hits} {moduleName} records where {@Terms}", hits, moduleName, searchTerms.List);

                while (true)
                {
                    if (Program.ImportCanceled)
                        return cachedIrns;

                    var results = imuSession.Fetch("start", offset, Constants.CachedDataBatchSize, new[] { "irn" });

                    if (results.Count == 0)
                        break;

                    var irns = results.Rows.Select(x => x.GetLong("irn")).ToList();

                    cachedIrns.AddRange(irns);

                    offset += results.Count;

                    Log.Logger.Information("{Name} {moduleName} cache progress... {Offset}/{TotalResults}", this.GetType().Name, moduleName, offset, hits);
                }
            }

            return cachedIrns;
        }

        protected IList<long> CacheIrns(string moduleName, Terms searchTerms, string[] columns, Func<Map, IEnumerable<long>> irnSelectFunc)
        {
            var cachedIrns = new List<long>();
            var offset = 0;

            // Cache Irns
            using (var imuSession = ImuSessionProvider.CreateInstance(moduleName))
            {
                Log.Logger.Information("Caching {moduleName} irns", moduleName);

                var hits = imuSession.FindTerms(searchTerms);

                Log.Logger.Information("Found {Hits} {moduleName} records where {@Terms}", hits, moduleName, searchTerms.List);

                while (true)
                {
                    if (Program.ImportCanceled)
                        return cachedIrns;

                    var results = imuSession.Fetch("start", offset, Constants.CachedDataBatchSize, columns);

                    if (results.Count == 0)
                        break;

                    var irns = results.Rows.SelectMany(irnSelectFunc);

                    cachedIrns.AddRange(irns);

                    offset += results.Count;

                    Log.Logger.Information("{Name} {moduleName} cache progress... {Offset}/{TotalResults}", this.GetType().Name, moduleName, offset, hits);
                }
            }

            Log.Logger.Information("Found {CachedIrns} Cached irns in {moduleName}", cachedIrns.Count, moduleName);

            return cachedIrns;
        }
    }
}
