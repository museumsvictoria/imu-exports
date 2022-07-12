using CsvHelper.Configuration;
using ImuExports.Tasks.AusGeochem.Models.Api;

namespace ImuExports.Tasks.AusGeochem.ClassMaps;

public sealed class MaterialsClassMap : ClassMap<MaterialDto>
{
    public MaterialsClassMap()
    {
        Map(m => m.Id).Name("Material Id");
        Map(m => m.Name).Name("Material Name");
        Map(m => m.Description).Name("Description");
    }
}