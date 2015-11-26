namespace ImuExports.Infrastructure
{
    public interface IImuSessionProvider
    {
        ImuSession CreateInstance(string moduleName);
    }
}