using System;
using System.Collections.Generic;
using System.Linq;
using IMu;
using ImuExports.Config;
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

        protected IList<long> CacheIrns(string moduleName, string moduleSearchName, Terms searchTerms, string[] columns, Func<Map, IEnumerable<long>> irnSelectFunc)
        {
            var cachedIrns = new List<long>();
            var offset = 0;

            // Cache Irns
            using (var imuSession = ImuSessionProvider.CreateInstance(moduleName))
            {
                Log.Logger.Information("Caching {moduleSearchName} irns by searching {moduleName}", moduleSearchName, moduleName);

                var hits = imuSession.FindTerms(searchTerms);

                Log.Logger.Information("Found {Hits} {moduleName} records where {@Terms}", hits, moduleName, searchTerms.List);

                while (true)
                {
                    if (Program.ImportCanceled)
                        return cachedIrns;

                    var results = imuSession.Fetch("start", offset, Constants.CachedDataBatchSize, columns);

                    if (results.Count == 0)
                        break;

                    var irns = results.Rows.SelectMany(irnSelectFunc).ToList();
                    
                    Log.Logger.Debug("Selected {irns} {moduleSearchName} irns, adding to cached irns", irns.Count, moduleSearchName);

                    cachedIrns.AddRange(irns);

                    offset += results.Count;

                    Log.Logger.Information("{moduleName} cache progress... {Offset}/{TotalResults}", moduleName, offset, hits);
                }
                
                Log.Logger.Information("Completed caching {cachedIrns} {moduleSearchName} irns found by searching {moduleName}", cachedIrns.Count, moduleSearchName, moduleName);
            }
            
            return cachedIrns;
        }
    }
}
