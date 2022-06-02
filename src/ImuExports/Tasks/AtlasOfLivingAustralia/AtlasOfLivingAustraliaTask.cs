namespace ImuExports.Tasks.AtlasOfLivingAustralia;

public class AtlasOfLivingAustraliaTask : ITask
{
    public async Task Run(CancellationToken stoppingToken)
    {
        await Task.Run(() =>
        {
            Log.Logger.Information("Run AtlasOfLivingAustraliaTask");
        }, stoppingToken);
    }
}