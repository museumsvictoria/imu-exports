using IMu;
using ImuExports.Tasks.AtlasOfLivingAustralia.Config;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Helpers;

public static class Assertions
{
    public static bool IsMultimedia(Map map)
    {
        return map != null &&
               string.Equals(map.GetTrimString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
               map.GetTrimStrings("MdaDataSets_tab").Any(x =>
                   x.Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString)) &&
               string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase);
    }
}