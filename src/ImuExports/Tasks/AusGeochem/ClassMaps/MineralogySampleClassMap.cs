namespace ImuExports.Tasks.AusGeochem.ClassMaps;

public sealed class MineralogySampleClassMap : SampleClassMap
{
    public MineralogySampleClassMap()
    {
        Map(m => m.Comment).Index(19).Name("mineral comment");
    }
}