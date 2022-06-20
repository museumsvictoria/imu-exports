using CsvHelper.Configuration;
using ImuExports.NetFramework472.Tasks.AusGeochem.Models;

namespace ImuExports.NetFramework472.Tasks.AusGeochem.ClassMaps
{
    public sealed class SpecimenClassMap : ClassMap<Specimen>
    {
        public SpecimenClassMap()
        {
            Map(m => m.SampleId).Name("SampleID");
            Map(m => m.SampleTypeId).Name("sample type id");
            Map(m => m.LithologyTypeId).Name("lithology_type_id");
            Map(m => m.LithologyComment).Name("lithology comment");
            Map(m => m.MineralId).Name("mineral_id");
            Map(m => m.MineralComment).Name("mineral comment");
            Map(m => m.DecimalLatitude).Name("lat (WGS84, decDegrees)");
            Map(m => m.DecimalLongitude).Name("lon (WGS84, decDegrees)");
            Map(m => m.LatLongPrecision).Name("lat/long precision_m");
            Map(m => m.GeoreferencedBy).Name("georeferencedBy");
            Map(m => m.DateGeoreferenced).Name("dateGeoreferenced");
            Map(m => m.LocationKindId).Name("location_kind_id");
            Map(m => m.LocationName).Name("loc_name");
            Map(m => m.LocationNotes).Name("locationNotes");
            Map(m => m.SampleKind).Name("sample_kind");
            Map(m => m.SampleTypeId).Name("Sample_type_id");
            Map(m => m.UnitName).Name("Unit (name)");
            Map(m => m.UnitAge).Name("unit_age");
            Map(m => m.DepthMin).Name("depth_min");
            Map(m => m.DepthMax).Name("depth_max");
            Map(m => m.ArchiveLocation).Name("Last known sample archive location");
            Map(m => m.ArchiveContact).Name("Archive contact");
            Map(m => m.DateCollectedMin).Name("Date Collected (minimum)");     
            Map(m => m.DateCollectedMax).Name("Date Collected (maximum)");
            Map(m => m.Collector).Name("Collector");
            Map(m => m.PreviousNumber).Name("PreviousNumber");
        }
    }
}