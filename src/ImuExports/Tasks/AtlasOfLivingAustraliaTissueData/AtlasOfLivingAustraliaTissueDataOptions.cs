using System;
using System.IO;
using CommandLine;
using ImuExports.Infrastructure;
using Serilog;

namespace ImuExports.Tasks.AtlasOfLivingAustraliaTissueData
{
    public class AtlasOfLivingAustraliaTissueDataOptions : ITaskOptions
    {
        [Option('d', "dest", HelpText = "Destination directory for csv and images.", Required = true)]
        public string Destination { get; set; }

        [Option('a', "modified-after", HelpText = "Get all records after modified date >=")]
        public string ModifiedAfterDate { get; set; }

        [Option('b', "modified-before", HelpText = "Get all records before modified date <=")]
        public string ModifiedBeforeDate { get; set; }

        public Type TypeOfTask { get { return typeof (AtlasOfLivingAustraliaTissueDataTask); }}

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