using CommandLine;
using ImuExports.Tasks.AusGeochem.Models;
using LiteDB;

namespace ImuExports.Tasks.AusGeochem;

[Verb("agn", HelpText = "Export records for AusGeochem.")]
public class AusGeochemOptions : ITaskOptions
{
    [Option('d', "delete-all", Required = false, HelpText = "Delete all MV records in AusGeochem")]
    public bool DeleteAll { get; set; }
    
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
        });
    }
}