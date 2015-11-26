using CommandLine;
using CommandLine.Text;
using ImuExports.Tasks.AtlasOfLivingAustralia;
using ImuExports.Tasks.FieldGuide;

namespace ImuExports.Infrastructure
{
    public class CommandLineOptions
    {
        [VerbOption("ala", HelpText = "Export records for the Atlas of Living Australia.")]
        public AtlasOfLivingAustraliaOptions Ala { get; set; }

        [VerbOption("fieldguide", HelpText = "Export records for Field Guide.")]
        public FieldGuideOptions FieldGuide { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}