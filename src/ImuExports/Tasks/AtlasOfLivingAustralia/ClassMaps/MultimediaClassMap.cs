using CsvHelper.Configuration;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps;

public sealed class MultimediaClassMap : ClassMap<Multimedia>
{
    public MultimediaClassMap()
    {
        Map(m => m.CoreId).Index(0).Name("coreID");
        Map(m => m.Type).Index(1).Name("type");
        Map(m => m.Format).Index(2).Name("format");
        Map(m => m.Identifier).Index(3).Name("identifier");
        Map(m => m.References).Index(4).Name("references");
        Map(m => m.Title).Index(5).Name("title");
        Map(m => m.Description).Index(6).Name("description");
        Map(m => m.Creator).Index(7).Name("creator");
        Map(m => m.Publisher).Index(8).Name("publisher");
        Map(m => m.Source).Index(9).Name("source");
        Map(m => m.License).Index(10).Name("license");
        Map(m => m.RightsHolder).Index(11).Name("rightsHolder");
        Map(m => m.AltText).Index(12).Name("altText");
    }
}