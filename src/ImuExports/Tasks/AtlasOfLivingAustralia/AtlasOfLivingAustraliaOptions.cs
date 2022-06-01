using CommandLine;

namespace ImuExports.Tasks.AtlasOfLivingAustralia
{
    [Verb("ala", HelpText = "Export records for the Atlas of Living Australia.")]
    public class AtlasOfLivingAustraliaOptions : ITaskOptions
    {
        [Option('d', "dest", HelpText = "Destination directory for csv and images.")]
        public string Destination { get; set; }

        [Option('a', "modified-after", HelpText = "Get all records after modified date >=")]
        public string ModifiedAfterDate { get; set; }

        [Option('b', "modified-before", HelpText = "Get all records before modified date <=")]
        public string ModifiedBeforeDate { get; set; }

        public Type TypeOfTask => typeof (AtlasOfLivingAustraliaTask);

        public DateTime DateStarted { get; }
        
        public AtlasOfLivingAustraliaOptions()
        {
            DateStarted = DateTime.Now;
        }

        public void Initialize()
        {
            Log.Logger.Information("Initializing {TypeOfTask}", TypeOfTask);
        }
    }
}