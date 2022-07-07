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
        Map(m => m.LocationNotes).Index(6).Name("locationNotes");
        Map(m => m.UnitName).Index(7).Name("Unit (name)");
        Map(m => m.UnitAge).Index(8).Name("unit_age");
        Map(m => m.LocationKind).Index(9).Name("locationKind");
        Map(m => m.DepthMin).Index(10).Name("depth_min");
        Map(m => m.DepthMax).Index(11).Name("depth_max");
        Map(m => m.PersonRole).Index(12).Name("person_role");
        Map(m => m.DateCollectedMin).Index(13).Name("Date Collected (minimum)");
        Map(m => m.DateCollectedMax).Index(14).Name("Date Collected (maximum)");
        Map(m => m.SampleKind).Index(15).Name("sample_kind");
        Map(m => m.SpecimenState).Index(16).Name("specimen_state");
        Map(m => m.MineralId).Index(17).Name("mineral_id");
        Map(m => m.LastKnownLocation).Index(18).Name("Last known sample archive location");
    }
}