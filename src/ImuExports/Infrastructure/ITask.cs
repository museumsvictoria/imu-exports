namespace ImuExports.Infrastructure
{
    public interface ITask
    {
        Task Run(CancellationToken stoppingToken);
    }
}