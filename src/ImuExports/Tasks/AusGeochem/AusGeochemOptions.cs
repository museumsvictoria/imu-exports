using CommandLine;
using ImuExports.Tasks.AusGeochem.Models;
using LiteDB;

namespace ImuExports.Tasks.AusGeochem;

[Verb("agn", HelpText = "Export records for AusGeochem.")]
public class AusGeochemOptions : ITaskOptions
{
    [Option('d', "dest", HelpText = "Destination directory for csv and images.")]
    public string Destination { get; set; }

    public Type TypeOfTask => typeof(AusGeochemTask);
    
    public AusGeochemApplication Application { get; set; }
    
    public DateTime DateStarted { get; }

    public AusGeochemOptions()
    {
        DateStarted = DateTime.Now;
    }

    public async Task Initialize(AppSettings appSettings)
    {
        await Task.Run(() =>
        {
            Log.Logger.Information("Initializing {TypeOfTask}", TypeOfTask);

            if (!string.IsNullOrWhiteSpace(Destination))
            {
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
            }
            else
            {
                // No destination specified so we will be sending samples via API and need to know the last date export was run
                using var db = new LiteRepository(new ConnectionString()
                {
                    Filename = $"{AppContext.BaseDirectory}{appSettings.LiteDbFilename}",
                    Upgrade = true
                });

                Application = db.Query<AusGeochemApplication>().FirstOrDefault();

                if (Application == null)
                {
                    Application = new AusGeochemApplication();
                    Log.Logger.Debug("No AusGeochem Application found... creating new application");
                }
                else
                {
                    Log.Logger.Debug("AusGeochem Application found {PreviousDateRun}", Application.PreviousDateRun);
                }
            }
        });
    }
}