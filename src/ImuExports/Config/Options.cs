using CommandLine;
using CommandLine.Text;
using ImuExports.Tasks.AtlasOfLivingAustralia;
using ImuExports.Tasks.ExtractImages;
using ImuExports.Tasks.FieldGuideGippsland;
using ImuExports.Tasks.FieldGuideGunditjmara;
using ImuExports.Tasks.WikimediaCommons;
using ImuExports.Tasks.InsideOut;

namespace ImuExports.Config
{
    public class Options
    {
        [VerbOption("ala", HelpText = "Export records for the Atlas of Living Australia.")]
        public AtlasOfLivingAustraliaOptions Ala { get; set; }

        [VerbOption("gip", HelpText = "Export records for Gippsland Field Guide.")]
        public FieldGuideGippslandOptions Gip { get; set; }

        [VerbOption("gun", HelpText = "Export records for Gunditjmara Field Guide.")]
        public FieldGuideGunditjmaraOptions Gun { get; set; }

        [VerbOption("wc", HelpText = "Export records for the Wikimedia commons.")]
        public WikimediaCommonsOptions Wc { get; set; }

        [VerbOption("ei", HelpText = "Extract images for the Ursula.")]
        public ExtractImagesOptions Ei { get; set; }

        [VerbOption("io", HelpText = "Extract records for Inside Out.")]
        public InsideOutOptions Io { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}