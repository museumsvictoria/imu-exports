using CommandLine;
using CommandLine.Text;
using ImuExports.Tasks.AtlasOfLivingAustralia;
using ImuExports.Tasks.FieldGuideGippsland;

namespace ImuExports.Config
{
    public class Options
    {
        [VerbOption("ala", HelpText = "Export records for the Atlas of Living Australia.")]
        public AtlasOfLivingAustraliaOptions Ala { get; set; }

        [VerbOption("fgg", HelpText = "Export records for Gippsland Field Guide.")]
        public FieldGuideGippslandOptions Fgg { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}