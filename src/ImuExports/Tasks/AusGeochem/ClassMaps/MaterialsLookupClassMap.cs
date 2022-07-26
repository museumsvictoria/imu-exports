using CsvHelper.Configuration;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.ClassMaps;

public sealed class MaterialsLookupClassMap : ClassMap<MaterialLookup>
{
    public MaterialsLookupClassMap()
    {
        Map(m => m.MvName).Name("MV Material Name");
        Map(m => m.AusGeochemName).Name("AusGeoChem Material Name");
    }
}