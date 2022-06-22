namespace ImuExports.Tasks.AusGeochem.ClassMaps;

public sealed class PetrologySampleClassMap : SampleClassMap
{
    public PetrologySampleClassMap()
    {
        Map(m => m.Comment).Index(20).Name("lithology comment");
    }
}