using CsvHelper.Configuration;
using ImuExports.Tasks.AusGeochem.Models;
// ReSharper disable VirtualMemberCallInConstructor

namespace ImuExports.Tasks.AusGeochem.ClassMaps;

public class SampleClassMap : ClassMap<Sample>
{
    protected SampleClassMap()
    {
        Map(m => m.SampleId).Index(0).Name("SampleID");
        Map(m => m.ArchiveNotes).Index(1).Name("archive_notes");
        Map(m => m.Latitude).Index(2).Name("lat (WGS84, decDegrees)");
        Map(m => m.Longitude).Index(3).Name("lon (WGS84, decDegrees)");
        Map(m => m.LatLongPrecision).Index(4).Name("lat/long precision_m");
        Map(m => m.DateGeoreferenced).Index(5).Name("dateGeoreferenced");
        Map(m => m.LocationName).Index(6).Name("loc_name");
        Map(m => m.LocationNotes).Index(7).Name("locationNotes");
        Map(m => m.UnitName).Index(8).Name("Unit (name)");
        Map(m => m.UnitAge).Index(9).Name("unit_age");
        Map(m => m.LocationKind).Index(10).Name("locationKind");
        Map(m => m.DepthMin).Index(11).Name("depth_min");
        Map(m => m.DepthMax).Index(12).Name("depth_max");
        Map(m => m.Collector).Index(13).Name("Collector");
        Map(m => m.PersonRole).Index(14).Name("person_role");
        Map(m => m.DateCollectedMin).Index(15).Name("Date Collected (minimum)");
        Map(m => m.DateCollectedMax).Index(16).Name("Date Collected (maximum)");
        Map(m => m.SampleKind).Index(17).Name("sample_kind");
        Map(m => m.SpecimenState).Index(18).Name("specimen_state");
        Map(m => m.MineralId).Index(19).Name("mineral_id");
        Map(m => m.LastKnownLocation).Index(21).Name("Last known sample archive location");
    }
}