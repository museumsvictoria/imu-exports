using CsvHelper.Configuration;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps
{
    public sealed class MultimediaClassMap : ClassMap<Multimedia>
    {
        public MultimediaClassMap()
        {
            Map(m => m.CoreID).Name("coreID");
            Map(m => m.Type).Name("type");
            Map(m => m.Format).Name("format");
            Map(m => m.Identifier).Name("identifier");
            Map(m => m.References).Name("references");
            Map(m => m.Title).Name("title");
            Map(m => m.Description).Name("description");
            Map(m => m.Creator).Name("creator");
            Map(m => m.Publisher).Name("publisher");
            Map(m => m.Source).Name("source");
            Map(m => m.License).Name("license");
            Map(m => m.RightsHolder).Name("rightsHolder");
            Map(m => m.AltText).Name("altText");
        }
    }
}