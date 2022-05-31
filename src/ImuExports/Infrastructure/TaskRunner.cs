using ImuExports.Configuration;
using Microsoft.Extensions.Options;
using Serilog;

namespace ImuExports.Infrastructure;

public class TaskRunner : BackgroundService
{
    private readonly AppSettings _appSettings;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IEnumerable<ITask> _tasks;

    public TaskRunner(IOptions<AppSettings> appSettings,
        IHostApplicationLifetime appLifetime,
        IEnumerable<ITask> tasks)
    {
        _appSettings = appSettings.Value;
        _appLifetime = appLifetime;
        _tasks = tasks;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Log.Logger.Debug("Worker running at: {Time}", DateTimeOffset.Now);
            
            await Task.Delay(1000, stoppingToken);
        }
    }
}