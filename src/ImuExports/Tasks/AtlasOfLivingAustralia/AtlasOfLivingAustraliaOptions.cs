using System.Globalization;
using CommandLine;
using ImuExports.Extensions;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using LiteDB;

namespace ImuExports.Tasks.AtlasOfLivingAustralia;

[Verb("ala", HelpText = "Export records for the Atlas of Living Australia.")]
public class AtlasOfLivingAustraliaOptions : ITaskOptions
{
    [Option('d', "dest", HelpText = "Destination directory for csv and images.")]
    public string Destination { get; set; }

    [Option('a', "modified-after", HelpText = "Get all records after modified date >=")]
    public string ModifiedAfterDate { get; set; }

    [Option('b', "modified-before", HelpText = "Get all records before modified date <=")]
    public string ModifiedBeforeDate { get; set; }

    public Type TypeOfTask => typeof(AtlasOfLivingAustraliaTask);

    public DateTime? ParsedModifiedAfterDate { get; set; }

    public DateTime? ParsedModifiedBeforeDate { get; set; }

    public bool IsAutomated { get; set; }

    public AtlasOfLivingAustraliaApplication Application { get; set; }

    public DateTime DateStarted { get; }

    public AtlasOfLivingAustraliaOptions()
    {
        DateStarted = DateTime.Now;
    }

    public async Task Initialize(AppSettings appSettings)
    {
        await Task.Run(() =>
        {
            Log.Logger.Information("Initializing {TypeOfTask}", TypeOfTask);

            // Task is automated if no destination specified, create random temporary directory
            if (string.IsNullOrWhiteSpace(Destination))
            {
                IsAutomated = true;
                Log.Logger.Debug("No destination specified... assuming task is automated");

                Destination = $"{AppContext.BaseDirectory}\\{Utils.RandomString(8)}";
                Log.Logger.Information("Exporting to directory {Destination}", Destination);

                // Check for last import date
                using var db = new LiteRepository(new ConnectionString()
                {
                    Filename = $"{AppContext.BaseDirectory}\\{appSettings.LiteDbFilename}",
                    Upgrade = true
                });

                Application = db.Query<AtlasOfLivingAustraliaApplication>().FirstOrDefault();

                if (Application == null)
                {
                    Application = new AtlasOfLivingAustraliaApplication();
                    Log.Logger.Debug("No AtlasOfLivingAustralia Application found... creating new application");
                }
                else
                {
                    ParsedModifiedAfterDate = Application.PreviousDateRun;
                    Log.Logger.Debug("AtlasOfLivingAustralia Application found");

                    Log.Logger.Debug(
                        "Setting ParsedModifiedAfterDate to application.PreviousDateRun {ParsedModifiedAfterDate}",
                        ParsedModifiedAfterDate?.ToString("yyyy-MM-dd"));
                }
            }
            else
            {
                Log.Logger.Debug("Destination specified... assuming task is being run manually");
                Log.Logger.Information("Exporting to directory {Destination}", Destination);
            }

            // Add backslash if it doesnt exist to our destination directory
            if (!Destination.EndsWith(@"\")) Destination += @"\";

            // Make sure destination directory exists
            if (!Directory.Exists(Destination))
                try
                {
                    Directory.CreateDirectory(Destination);
                }
                catch (Exception ex)
                {
                    Log.Logger.Fatal(ex, "Error creating {Destination} directory", Destination);
                    Environment.Exit(Constants.ExitCodeError);
                }

            // Parse ModifiedAfterDate
            if (!string.IsNullOrWhiteSpace(ModifiedAfterDate))
                try
                {
                    ParsedModifiedAfterDate = DateTime.ParseExact(ModifiedAfterDate, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None);
                    Log.Logger.Debug(
                        "ModifiedAfterDate flag found... Setting ParsedModifiedAfterDate to {ParsedModifiedAfterDate}",
                        ParsedModifiedAfterDate?.ToString("yyyy-MM-dd"));
                }
                catch (Exception ex)
                {
                    Log.Logger.Fatal(ex, "Error parsing ModifiedAfterDate, ensure string is in the format yyyy-MM-dd",
                        ModifiedAfterDate);
                    Environment.Exit(Constants.ExitCodeError);
                }

            // Parse ModifiedBeforeDate
            if (!string.IsNullOrWhiteSpace(ModifiedBeforeDate))
                try
                {
                    ParsedModifiedBeforeDate = DateTime.ParseExact(ModifiedBeforeDate, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None);
                    Log.Logger.Debug(
                        "ModifiedBeforeDate flag found...Setting ParsedModifiedBeforeDate to {ParsedModifiedBeforeDate}",
                        ParsedModifiedBeforeDate?.ToString("yyyy-MM-dd"));
                }
                catch (Exception ex)
                {
                    Log.Logger.Fatal(ex, "Error parsing ModifiedBeforeDate, ensure string is in the format yyyy-MM-dd",
                        ModifiedBeforeDate);
                    Environment.Exit(Constants.ExitCodeError);
                }
        });
    }

    public async Task CleanUp(AppSettings appSettings)
    {
        await Task.Run(() =>
        {
            // Remove any temporary files and directory if running automated export
            if (IsAutomated)
            {
                Log.Logger.Information("Deleting temporary directory {Destination}", Destination);
                Directory.Delete(Destination, true);
            }
        });
    }
}