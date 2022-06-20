using System;
using System.IO;
using CommandLine;
using ImuExports.NetFramework472.Infrastructure;
using Serilog;

namespace ImuExports.NetFramework472.Tasks.AusGeochem
{
    public class AusGeochemOptions : ITaskOptions
    {
        [Option('d', "dest", HelpText = "Destination directory for csv and images.")]
        public string Destination { get; set; }

        public Type TypeOfTask => typeof (AusGeochemTask);

        public void Initialize()
        {
            Log.Logger.Information("Initializing {TypeOfTask}", TypeOfTask);
            
            // Add backslash if it doesnt exist to our destination directory
            if (!this.Destination.EndsWith(@"\"))
            {
                this.Destination += @"\";
            }

            // Make sure destination directory exists
            if (!Directory.Exists(this.Destination))
            {
                try
                {
                    Directory.CreateDirectory(this.Destination);
                }
                catch (Exception ex)
                {
                    Log.Logger.Fatal(ex, "Error creating {Destination} directory", this.Destination);
                    Environment.Exit(Parser.DefaultExitCodeFail);
                }
            }
        }
    }
}