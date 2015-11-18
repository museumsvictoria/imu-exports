using System;
using Serilog;

namespace ALAExport.Export.Infrastructure
{
    public static class SerilogConfig
    {
        public static void Initialize()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .WriteTo.ColoredConsole()
                .WriteTo.RollingFile(string.Format("{0}\\logs\\{{Date}}.txt", AppDomain.CurrentDomain.BaseDirectory))
                .CreateLogger();
        }
    }
}
