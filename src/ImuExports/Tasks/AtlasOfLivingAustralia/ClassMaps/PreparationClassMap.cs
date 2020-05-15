using CsvHelper.Configuration;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps
{
    public sealed class PreparationClassMap : ClassMap<Preparation>
    {
        public PreparationClassMap()
        {
            Map(m => m.CoreId).Name("coreID");
            Map(m => m.PreparationType).Name("preparationType");
            Map(m => m.PreparationMaterials).Name("preparationMaterials");
            Map(m => m.PreparedBy).Name("preparedBy");
            Map(m => m.PreparationDate).Name("preparationDate");
        }
    }
}