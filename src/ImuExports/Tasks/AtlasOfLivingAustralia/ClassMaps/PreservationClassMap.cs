using CsvHelper.Configuration;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps
{
    public sealed class PreservationClassMap : ClassMap<Preservation>
    {
        public PreservationClassMap()
        {
            Map(m => m.CoreId).Name("coreID");
            Map(m => m.PreservationType).Name("preservationType");
            Map(m => m.PreservationTemperature).Name("preservationTemperature");
            Map(m => m.PreservationDateBegin).Name("preservationDateBegin");
        }
    }
}