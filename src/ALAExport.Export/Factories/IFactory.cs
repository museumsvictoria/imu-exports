using System.Collections.Generic;
using IMu;

namespace ALAExport.Export.Factories
{
    public interface IFactory<T>
    {
        T Make(Map map);

        IEnumerable<T> Make(IEnumerable<Map> maps);
    }
}