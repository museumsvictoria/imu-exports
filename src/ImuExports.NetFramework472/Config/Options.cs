using CommandLine;
using CommandLine.Text;
using ImuExports.NetFramework472.Tasks.AtlasOfLivingAustralia;
using ImuExports.NetFramework472.Tasks.AusGeochem;
using ImuExports.NetFramework472.Tasks.FieldGuideGippsland;
using ImuExports.NetFramework472.Tasks.FieldGuideGunditjmara;

namespace ImuExports.NetFramework472.Config
{
    public class Options
    {
        [VerbOption("ala", HelpText = "Export records for the Atlas of Living Australia.")]
        public AtlasOfLivingAustraliaOptions Ala { get; set; }

        [VerbOption("gip", HelpText = "Export records for Gippsland Field Guide.")]
        public FieldGuideGippslandOptions Gip { get; set; }

        [VerbOption("gun", HelpText = "Export records for Gunditjmara Field Guide.")]
        public FieldGuideGunditjmaraOptions Gun { get; set; }

        [VerbOption("agn", HelpText = "Export records for AusGeochem.")]
        public AusGeochemOptions Agn { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}