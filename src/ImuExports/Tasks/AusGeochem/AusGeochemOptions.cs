using CommandLine;

namespace ImuExports.Tasks.AusGeochem;

[Verb("agn", HelpText = "Export records for AusGeochem.")]
public class AusGeochemOptions : ITaskOptions
{
    [Option('d', "dest", HelpText = "Destination directory for csv and images.")]
    public string Destination { get; set; }

    public Type TypeOfTask => typeof(AusGeochemTask);
}