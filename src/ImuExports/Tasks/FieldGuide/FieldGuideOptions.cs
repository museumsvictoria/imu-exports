using CommandLine;

namespace ImuExports.Tasks.FieldGuide
{
    public class FieldGuideOptions
    {
        [Option('d', "dest", HelpText = "Destination directory for json and images.", Required = true)]
        public string Destination { get; set; }
    }
}