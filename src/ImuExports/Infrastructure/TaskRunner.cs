using ImuExports.Configuration;
using Microsoft.Extensions.Options;

namespace ImuExports.Infrastructure;

public class TaskRunner : BackgroundService
{
    private readonly AppSettings _appSettings;
    private readonly IHostApplicationLifetime _appLifetime;

    public TaskRunner(IOptions<AppSettings> appSettings,
        IHostApplicationLifetime appLifetime)
    {
        _appSettings = appSettings.Value;
        _appLifetime = appLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}