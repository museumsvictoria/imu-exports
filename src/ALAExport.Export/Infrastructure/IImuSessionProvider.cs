namespace ALAExport.Export.Infrastructure
{
    public interface IImuSessionProvider
    {
        ImuSession CreateInstance(string moduleName);
    }
}
