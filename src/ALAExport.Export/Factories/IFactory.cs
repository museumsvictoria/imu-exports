using IMu;

namespace ALAExport.Export.Factories
{
    public interface IFactory<T>
    {
        T Make(Map map);
    }
}