using Microsoft.Extensions.Options;
namespace ImuExports.Infrastructure;

public class TaskRunner : BackgroundService
{
    private readonly AppSettings _appSettings;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ITask _task;

    public TaskRunner(IOptions<AppSettings> appSettings,
        IHostApplicationLifetime appLifetime,
        ITask task)
    {
        _appSettings = appSettings.Value;
        _appLifetime = appLifetime;
        _task = task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Logger.Information("TaskRunner starting up");

        try
        {
            // Initialize task
            await CommandOptions.TaskOptions.Initialize(_appSettings);

            // Run task
            await _task.Run(stoppingToken);
            
            // Cleanup task
            await CommandOptions.TaskOptions.CleanUp(_appSettings);
        }
        catch
        {
            // Attempt to cleanup
            await CommandOptions.TaskOptions.CleanUp(_appSettings);

            throw;
        }

        if (!stoppingToken.IsCancellationRequested)
            Log.Logger.Information("TaskRunner finished successfully");

        _appLifetime.StopApplication();
    }
}