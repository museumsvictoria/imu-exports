using System;
using System.IO;
using CommandLine;
using ImuExports.NetFramework472.Infrastructure;
using Serilog;

namespace ImuExports.NetFramework472.Tasks.FieldGuideGippsland
{
    public class FieldGuideGippslandOptions : ITaskOptions
    {
        [Option('d', "dest", HelpText = "Destination directory for json and images.", Required = true)]
        public string Destination { get; set; }

        public Type TypeOfTask => typeof(FieldGuideGippslandTask);

        public void Initialize()
        {
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