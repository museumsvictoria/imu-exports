using System;
using System.Linq;
using IMu;
using ImuExports.NetFramework472.Extensions;
using ImuExports.NetFramework472.Tasks.AtlasOfLivingAustralia.Config;

namespace ImuExports.NetFramework472.Tasks.AtlasOfLivingAustralia.Helpers
{
    public static class Assertions
    {
        public static bool IsMultimedia(Map map)
        {
            return map != null &&
                   string.Equals(map.GetTrimString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                   map.GetTrimStrings("MdaDataSets_tab").Any(x => x.Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString)) &&
                   string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase);
        }
    }
}