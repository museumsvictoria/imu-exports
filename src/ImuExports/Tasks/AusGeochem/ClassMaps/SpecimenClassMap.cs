using CsvHelper.Configuration;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.ClassMaps
{
    public sealed class SpecimenClassMap : ClassMap<Specimen>
    {
        public SpecimenClassMap()
        {
            Map(m => m.SampleId).Name("SampleId");
        }
    }
}