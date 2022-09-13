namespace ImuExports.Tasks.AusGeochem.Models;

public class Sample
{
    public Sample()
    {
        Images = new List<Image>();
        Properties = new List<SampleProperty>();
    }
    
    public IList<Image> Images { get; set; }
    
    public IList<SampleProperty> Properties { get; set; }
    
    public string Irn { get; set; }
    
    public string Name { get; set; }
    
    public string ArchiveNotes { get; set; }
    
    public string Latitude { get; set; }

    public string Longitude { get; set; }
    
    public string LatLongPrecision { get; set; }
    
    public string DateGeoreferenced { get; set; }

    public string LocationDescription { get; set; }
    
    public string UnitName { get; set; }

    public string UnitAge { get; set; }
    
    public string LocationKind { get; set; }
    
    public string DepthMin { get; set; }

    public string DepthMax { get; set; }
    
    public string DateCollectedMin { get; set; }

    public string DateCollectedMax { get; set; }
    
    public string SampleKind { get; set; }
    
    public string MineralId { get; set; }
    
    public string Comment { get; set; }
    
    public string LastKnownLocation { get; set; }
    
    public bool Deleted { get; set; }
}