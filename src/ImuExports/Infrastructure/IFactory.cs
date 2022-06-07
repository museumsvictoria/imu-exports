using IMu;

namespace ImuExports.Infrastructure;

public interface IFactory<out T>
{
    T Make(Map map);

    IEnumerable<T> Make(IEnumerable<Map> maps);
}