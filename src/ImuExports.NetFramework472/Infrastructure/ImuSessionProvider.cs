using System.Configuration;

namespace ImuExports.NetFramework472.Infrastructure
{
    public static class ImuSessionProvider
    {
        public static ImuSession CreateInstance(string moduleName)
        {
            return new ImuSession(moduleName, ConfigurationManager.AppSettings["EmuServerHost"], int.Parse(ConfigurationManager.AppSettings["EmuServerPort"]));
        }
    }
}