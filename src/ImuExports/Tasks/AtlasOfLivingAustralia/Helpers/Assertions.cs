using System;
using System.Linq;
using IMu;
using ImuExports.Extensions;
using ImuExports.Tasks.AtlasOfLivingAustralia.Config;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Helpers
{
    public static class Assertions
    {
        public static bool IsMultimedia(Map map)
        {
            return map != null &&
                   string.Equals(map.GetTrimString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                   map.GetTrimStrings("MdaDataSets_tab").Any(x => x.Contains(AtlasOfLivingAustraliaConstants.QueryString)) &&
                   string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase);
        }
    }
}