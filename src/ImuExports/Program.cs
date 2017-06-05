using System;
using ImuExports.Config;
using ImuExports.Infrastructure;
using Serilog;

namespace ImuExports
{
    static class Program
    {
        public static volatile bool ImportCanceled = false;

        static void Main(string[] args)
        {
            // Configure Program
            ProgramConfig.Initialize();

            // Parse command line options
            GlobalOptions.Initialize(args);

            // Configure serilog
            SerilogConfig.Initialize();
            
            // Wire up ioc
            var container = ContainerConfig.Initialize();

            // Begin export
            container.GetInstance<TaskRunner>().RunAllTasks();
        }
    }
}