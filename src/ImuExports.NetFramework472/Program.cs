using ImuExports.NetFramework472.Config;
using ImuExports.NetFramework472.Infrastructure;

namespace ImuExports.NetFramework472
{
    static class Program
    {
        public static volatile bool ImportCanceled = false;

        static void Main(string[] args)
        {
            // Configure serilog
            SerilogConfig.Initialize();
            
            // Configure Program
            ProgramConfig.Initialize();

            // Parse command line options and run any task initialization steps
            GlobalOptions.Initialize(args);

            // Wire up ioc
            var container = ContainerConfig.Initialize();

            // Begin export
            container.GetInstance<TaskRunner>().RunAllTasks();
        }
    }
}