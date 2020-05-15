using CsvHelper.Configuration;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps
{
    public sealed class MaterialSampleClassMap : ClassMap<MaterialSample>
    {
        public MaterialSampleClassMap()
        {
            Map(m => m.CoreId).Name("coreID");
            Map(m => m.MaterialSampleType).Name("materialSampleType");
        }
    }
}