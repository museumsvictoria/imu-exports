using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using CommandLine;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using LiteDB;
using Serilog;

namespace ImuExports.Tasks.AtlasOfLivingAustralia
{
    public class AtlasOfLivingAustraliaOptions : ITaskOptions
    {
        [Option('d', "dest", HelpText = "Destination directory for csv and images.")]
        public string Destination { get; set; }

        [Option('a', "modified-after", HelpText = "Get all records after modified date >=")]
        public string ModifiedAfterDate { get; set; }

        [Option('b', "modified-before", HelpText = "Get all records before modified date <=")]
        public string ModifiedBeforeDate { get; set; }

        public Type TypeOfTask => typeof (AtlasOfLivingAustraliaTask);

        public DateTime? ParsedModifiedAfterDate { get; set; }

        public DateTime? ParsedModifiedBeforeDate { get; set; }

        public bool IsAutomated { get; set; }
        
        public AtlasOfLivingAustraliaApplication Application { get; set; }

        public DateTime DateStarted { get; private set; }
        
        public AtlasOfLivingAustraliaOptions()
        {
            DateStarted = DateTime.Now;
        }

        public void Initialize()
        {
            Log.Logger.Information("Initializing {TypeOfTask}", TypeOfTask);
            
            // Task is automated if no destination specified, create random temporary directory
            if (string.IsNullOrWhiteSpace(this.Destination))
            {
                this.Destination = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\{Utils.RandomString(8)}";
                this.IsAutomated = true;

                Log.Logger.Information("No destination specified... assuming task is automated");
                Log.Logger.Information("Exporting to directory {Destination}", Destination);

                // Check for last import date
                using (var db = new LiteRepository(ConfigurationManager.ConnectionStrings["LiteDB"].ConnectionString))
                {
                    this.Application = db.Query<AtlasOfLivingAustraliaApplication>().FirstOrDefault();

                    if (this.Application == null)
                    {
                        this.Application = new AtlasOfLivingAustraliaApplication();
                        Log.Logger.Information("No AtlasOfLivingAustralia Application found... creating new application");
                    }
                    else
                    {
                        ParsedModifiedAfterDate = this.Application.PreviousDateRun;
                        Log.Logger.Information("AtlasOfLivingAustralia Application found");
                        
                        Log.Logger.Information("Setting ParsedModifiedAfterDate to application.PreviousDateRun {ParsedModifiedAfterDate}", ParsedModifiedAfterDate?.ToString("yyyy-MM-dd"));
                    }
                }
            }
            else
            {
                Log.Logger.Information("Destination specified... assuming task is being run manually");
                Log.Logger.Information("Exporting to directory {Destination}", Destination);
            }

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

            // Parse ModifiedAfterDate
            if (!string.IsNullOrWhiteSpace(this.ModifiedAfterDate))
            {
                try
                {
                    this.ParsedModifiedAfterDate = DateTime.ParseExact(this.ModifiedAfterDate, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None);
                    Log.Logger.Information("ModifiedAfterDate flag found... Setting ParsedModifiedAfterDate to {ParsedModifiedAfterDate}", ParsedModifiedAfterDate?.ToString("yyyy-MM-dd"));
                }
                catch (Exception ex)
                {
                    Log.Logger.Fatal(ex, "Error parsing ModifiedAfterDate, ensure string is in the format yyyy-MM-dd",
                        this.ModifiedAfterDate);
                    Environment.Exit(Parser.DefaultExitCodeFail);
                }
            }

            // Parse ModifiedBeforeDate
            if (!string.IsNullOrWhiteSpace(this.ModifiedBeforeDate))
            {
                try
                {
                    this.ParsedModifiedBeforeDate = DateTime.ParseExact(this.ModifiedBeforeDate, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None);
                    Log.Logger.Information("ModifiedBeforeDate flag found...Setting ParsedModifiedBeforeDate to {ParsedModifiedBeforeDate}", ParsedModifiedBeforeDate?.ToString("yyyy-MM-dd"));
                }
                catch (Exception ex)
                {
                    Log.Logger.Fatal(ex, "Error parsing ModifiedBeforeDate, ensure string is in the format yyyy-MM-dd",
                        this.ModifiedBeforeDate);
                    Environment.Exit(Parser.DefaultExitCodeFail);
                }
            }
        }
    }
}