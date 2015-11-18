using System;
using ALAExport.Export.Infrastructure;
using Serilog;

namespace ALAExport.Export
{
    class Program
    {
        public static volatile bool ImportCanceled = false;

        static void Main(string[] args)
        {
            // Parse command line options
            CommandLineConfig.Initialize(args);

            // Configure serilog
            SerilogConfig.Initialize();

            // Set up Ctrl+C handling 
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Log.Logger.Warning("Canceling export");

                eventArgs.Cancel = true;
                ImportCanceled = true;
            };

            // Log any exceptions that are not handled
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => Log.Logger.Fatal((Exception)eventArgs.ExceptionObject, "Unhandled Exception occured in export");
            
            // Wire up ioc
            var container = ContainerConfig.Initialize();

            // Begin export
            container.GetInstance<TaskRunner>().RunAllTasks();
        }
    }
}