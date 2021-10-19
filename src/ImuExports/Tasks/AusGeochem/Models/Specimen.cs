namespace ImuExports.Tasks.AusGeochem.Models
{
    public class Specimen
    {
        public string SampleId { get; set; }

        public string SampleTypeId { get; set; }
        
        public string LithologyTypeId { get; set; }
        
        public string LithologyComment { get; set; }
        
        public string MineralId { get; set; }
        
        public string MineralComment { get; set; }
        
        public string DecimalLatitude { get; set; }
        
        public string DecimalLongitude { get; set; }
        
        public string LatLongPrecision { get; set; }
        
        public string GeoreferencedBy { get; set; }
        
        public string DateGeoreferenced { get; set; }
        
        public string Elevation { get; set; }
        
        public string VerticalDatumId { get; set; }
        
        public string LocationKindId { get; set; }
        
        public string LocationName { get; set; }
        
        public string LocationNotes { get; set; }
        
        public string SampleKind { get; set; }
        
        public string UnitName { get; set; }
        
        public string UnitAge { get; set; }
        
        public string DepthMin { get; set; }
        
        public string DepthMax { get; set; }
        
        public string ArchiveLocation { get; set; }
        
        public string ArchiveContact { get; set; }
        
        public string DateCollectedMin { get; set; }
        
        public string DateCollectedMax { get; set; }
        
        public string Collector { get; set; }
        
        public string PreviousNumber { get; set; }
    }
}