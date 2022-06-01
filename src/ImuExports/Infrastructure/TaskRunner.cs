using Microsoft.Extensions.Options;

namespace ImuExports.Infrastructure;

public class TaskRunner : BackgroundService
{
    private readonly AppSettings _appSettings;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ITask _task;
    private readonly ITaskOptions _taskOptions;

    public TaskRunner(IOptions<AppSettings> appSettings,
        IHostApplicationLifetime appLifetime,
        ITask task,
        ITaskOptions taskOptions)
    {
        _appSettings = appSettings.Value;
        _appLifetime = appLifetime;
        _task = task;
        _taskOptions = taskOptions;
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