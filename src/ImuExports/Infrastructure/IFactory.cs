namespace ImuExports.Infrastructure;

public interface IFactory<T>
{
    Task<T> Make(CancellationToken stoppingToken);
}