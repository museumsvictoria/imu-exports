using CsvHelper.Configuration;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.CsvMaps
{
    public sealed class ImageCsvMap : CsvClassMap<Image>
    {
        public ImageCsvMap()
        {
            Map(m => m.CoreID).Name("coreID");
            Map(m => m.Identifier).Name("identifier");
            Map(m => m.Title).Name("title");
            Map(m => m.Description).Name("description");
            Map(m => m.Format).Name("format");
            Map(m => m.Creator).Name("creator");
            Map(m => m.License).Name("license");
            Map(m => m.RightsHolder).Name("rightsHolder");            
        }
    }
}