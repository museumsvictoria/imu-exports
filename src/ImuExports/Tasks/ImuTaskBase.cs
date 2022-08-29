using IMu;
using Microsoft.Extensions.Options;

namespace ImuExports.Tasks;

public abstract class ImuTaskBase
{
    private readonly AppSettings _appSettings;
    
    protected ImuTaskBase(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }
    
    protected async Task<IList<long>> CacheIrns(string moduleName, Terms searchTerms, CancellationToken stoppingToken)
    {
        return await Task.Run<IList<long>>(() =>
        {
            var cachedIrns = new List<long>();
            var offset = 0;
            
            // Create session
            using var imuSession = new ImuSession(moduleName, _appSettings.Imu.Host, _appSettings.Imu.Port);
            Log.Logger.Information("Caching {ModuleName} irns", moduleName);
            
            stoppingToken.ThrowIfCancellationRequested();

            // Find records
            var hits = imuSession.FindTerms(searchTerms);
            Log.Logger.Information("Found {Hits} {ModuleName} records where {@Terms}", hits, moduleName,
                searchTerms.List);
            
            stoppingToken.ThrowIfCancellationRequested();

            // Retrieve records
            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();

                var results = imuSession.Fetch("start", offset, Constants.CachedDataBatchSize, new[] { "irn" });

                if (results.Count == 0)
                    break;

                var irns = results.Rows.Select(x => x.GetLong("irn")).ToList();

                cachedIrns.AddRange(irns);

                offset += results.Count;

                Log.Logger.Information("{Name} {ModuleName} cache progress... {Offset}/{TotalResults}", GetType().Name,
                    moduleName, offset, hits);
            }

            return cachedIrns;
        }, stoppingToken);
    }

    protected async Task<IList<long>> CacheIrns(string moduleName, string moduleSearchName, Terms searchTerms, string[] columns,
        Func<Map, IEnumerable<long>> irnSelectFunc, CancellationToken stoppingToken)
    {
        return await Task.Run<IList<long>>(() =>
        {
            var cachedIrns = new List<long>();
            var offset = 0;

            // Create session
            using var imuSession = new ImuSession(moduleName, _appSettings.Imu.Host, _appSettings.Imu.Port);
            Log.Logger.Information("Caching {ModuleSearchName} irns by searching {ModuleName}", moduleSearchName,
                moduleName);

            // Find records
            var hits = imuSession.FindTerms(searchTerms);
            Log.Logger.Information("Found {Hits} {ModuleName} records where {@Terms}", hits, moduleName,
                searchTerms.List);

            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();

                var results = imuSession.Fetch("start", offset, Constants.CachedDataBatchSize, columns);

                if (results.Count == 0)
                    break;

                var irns = results.Rows.SelectMany(irnSelectFunc).ToList();

                Log.Logger.Debug("Selected {Irns} {ModuleSearchName} irns, adding to cached irns", irns.Count,
                    moduleSearchName);

                cachedIrns.AddRange(irns);

                offset += results.Count;

                Log.Logger.Information("{ModuleName} cache progress... {Offset}/{TotalResults}", moduleName, offset,
                    hits);
            }

            Log.Logger.Information(
                "Completed caching {CachedIrns} {ModuleSearchName} irns found by searching {ModuleName}",
                cachedIrns.Count, moduleSearchName, moduleName);

            return cachedIrns;
        }, stoppingToken);
    }
}