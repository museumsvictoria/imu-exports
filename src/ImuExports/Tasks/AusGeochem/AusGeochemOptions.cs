using CommandLine;

namespace ImuExports.Tasks.AusGeochem;

[Verb("agn", HelpText = "Export records for AusGeochem.")]
public class AusGeochemOptions : ITaskOptions
{
    [Option('d', "dest", HelpText = "Destination directory for csv and images.")]
    public string Destination { get; set; }

    public Type TypeOfTask => typeof(AusGeochemTask);

    public async Task Initialize(AppSettings appSettings)
    {
        await Task.Run(() =>
        {
            Log.Logger.Information("Initializing {TypeOfTask}", TypeOfTask);
            
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
        });
    }
}