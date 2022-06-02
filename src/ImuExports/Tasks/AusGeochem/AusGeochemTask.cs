namespace ImuExports.Tasks.AusGeochem;

public class AusGeochemTask : ITask
{
    public async Task Run(CancellationToken stoppingToken)
    {
        await Task.Run(() => { Log.Logger.Information("Run AusGeochemTask"); }, stoppingToken);
    }
}