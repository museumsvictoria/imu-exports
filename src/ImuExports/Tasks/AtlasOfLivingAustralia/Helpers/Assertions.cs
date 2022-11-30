using IMu;
using ImuExports.Tasks.AtlasOfLivingAustralia.Config;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Helpers;

public static class Assertions
{
    public static bool IsCollectionsOnlineImage(Map map)
    {
        return map != null &&
            string.Equals(map.GetTrimString("AdmPublishWebNoPassword"), "yes",
                StringComparison.OrdinalIgnoreCase) &&
            map.GetTrimStrings("MdaDataSets_tab")
                .Contains(AtlasOfLivingAustraliaConstants.ImuCollectionsOnlineMultimediaQueryString) &&
            string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase);
    }
    
    public static bool IsAtlasOfLivingAustraliaImage(Map map)
    {
        return map != null &&
               string.Equals(map.GetTrimString("AdmPublishWebNoPassword"), "yes",
                   StringComparison.OrdinalIgnoreCase) &&
               map.GetTrimStrings("MdaDataSets_tab")
                   .Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString) &&
               string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase);
    }
}