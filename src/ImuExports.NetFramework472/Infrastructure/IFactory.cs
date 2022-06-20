using System.Collections.Generic;
using IMu;

namespace ImuExports.NetFramework472.Infrastructure
{
    public interface IFactory<out T>
    {
        T Make(Map map);

        IEnumerable<T> Make(IEnumerable<Map> maps);
    }
}