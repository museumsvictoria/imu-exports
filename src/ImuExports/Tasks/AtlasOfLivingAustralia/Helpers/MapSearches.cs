using System.Linq;
using IMu;
using ImuExports.Extensions;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Helpers
{
    public static class MapSearches
    {
        public static Map GetIdentification(Map map)
        {
            var types = new[] {"holotype", "lectotype", "neotype", "paralectotype", "paratype", "syntype", "type"};
            var identification =
                (map.GetMaps("identifications")
                     .FirstOrDefault(x =>
                         x.GetTrimString("IdeTypeStatus_tab") != null &&
                         types.Contains(x.GetTrimString("IdeTypeStatus_tab").Trim().ToLower())) ??
                 map.GetMaps("identifications")
                     .FirstOrDefault(x =>
                         x.GetTrimString("IdeCurrentNameLocal_tab") != null &&
                         x.GetTrimString("IdeCurrentNameLocal_tab").Trim().ToLower() == "yes")) ??
                map.GetMaps("identifications").FirstOrDefault(x => x != null);
            return identification;
        }
    }
}