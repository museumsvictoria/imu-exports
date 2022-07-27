using CsvHelper.Configuration;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.ClassMaps;

public sealed class MaterialNamePairClassMap : ClassMap<MaterialNamePair>
{
    public MaterialNamePairClassMap()
    {
        Map(m => m.MvName).Name("MV Material Name");
        Map(m => m.AusGeochemName).Name("AusGeoChem Material Name");
    }
}