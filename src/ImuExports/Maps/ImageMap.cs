using CsvHelper.Configuration;
using ImuExports.Models;

namespace ImuExports.Maps
{
    public sealed class ImageMap : CsvClassMap<Image>
    {
        public ImageMap()
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