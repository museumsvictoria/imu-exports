using IMu;

namespace ImuExports.Infrastructure;

public interface IImuFactory<out T>
{
    T Make(Map map, CancellationToken stoppingToken);

    IEnumerable<T> Make(IEnumerable<Map> maps, CancellationToken stoppingToken);
}