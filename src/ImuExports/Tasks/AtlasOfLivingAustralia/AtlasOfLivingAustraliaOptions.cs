using System.ComponentModel;
using System.Globalization;
using CommandLine;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using LiteDB;

namespace ImuExports.Tasks.AtlasOfLivingAustralia;

[Verb("ala", HelpText = "Export records for the Atlas of Living Australia.")]
public class AtlasOfLivingAustraliaOptions : ITaskOptions
{
    [Option('d', "dest", HelpText = "Destination directory for Darwin Core Archive")]
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
                Destination = $"{AppContext.BaseDirectory}{Utils.RandomString(8)}";
                Log.Logger.Debug("No destination specified... assuming task is automated");
            }
            else
            {
                Log.Logger.Debug("Destination specified... assuming task is being run manually");
            }

            // Add backslash if it doesnt exist to our destination directory
            if (!Destination.EndsWith(@"\")) Destination += @"\";

            if (!Path.IsPathFullyQualified(Destination))
            {
                Destination = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, Destination));
            }
            
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
            
            Log.Logger.Information("Exporting to directory {Destination}", Destination);

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
                    Log.Logger.Fatal(ex, "Error parsing ModifiedAfterDate {ModifiedAfterDate}, ensure string is in the format yyyy-MM-dd",
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
                    Log.Logger.Fatal(ex, "Error parsing ModifiedBeforeDate {ModifiedBeforeDate}, ensure string is in the format yyyy-MM-dd",
                        ModifiedBeforeDate);
                    Environment.Exit(Constants.ExitCodeError);
                }
            
            // Attempt to access network
            try
            {
                NetworkShareAccesser.Access(appSettings.AtlasOfLivingAustralia.WebSiteComputer,
                    appSettings.AtlasOfLivingAustralia.WebSiteDomain,
                    appSettings.AtlasOfLivingAustralia.WebSiteUser,
                    appSettings.AtlasOfLivingAustralia.WebSitePassword);
            }
            catch (Win32Exception exception)
            {
                // Continue if exception is "Multiple connections to a server or shared resource by the same user..."
                if(exception.NativeErrorCode != 1219)
                    throw;
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