using System.Collections.Generic;

namespace ImuExports.Tasks.FieldGuideGunditjmara.Models
{
    public class GunditjmaraSpecies
    {
        public GunditjmaraSpecies()
        {
            Depths = new List<string>();
            WaterColumnLocations = new List<string>();
            Colours = new List<string>();
            Habitats = new List<string>();
            Authors = new List<string>();
        }

        public long Irn { get; set; }

        public string Lsid { get; set; }

        public string CommonName { get; set; }

        public string OtherCommonNames { get; set; }

        public string Group { get; set; }

        public string AnimalType { get; set; }

        public string AnimalSubType { get; set; }

        public string BriefDescription { get; set; }

        public string IdentifyingCharacteristics { get; set; }
        
        public string Habitat { get; set; }
        
        public string Distribution { get; set; }
        
        public string Biology { get; set; }
        
        public string Diet { get; set; }
        
        public string TraditionalKnowledge { get; set; }
        
        public string HazardousToHumans { get; set; }

        public IList<string> CallingMonths { get; set; }
        
        public string FlightMonthFrom { get; set; }
        
        public string FlightMonthTo { get; set; }
        
        public IList<string> Depths { get; set; }
        
        public IList<string> WaterColumnLocations { get; set; }

        public IList<string> Colours { get; set; }

        public IList<string> Habitats { get; set; }
        
        public string MaximumSize { get; set; }

        public IList<string> Authors { get; set; }

        public IList<GunditjmaraImage> Images { get; set; }

        public IList<GunditjmaraAudio> Audios { get; set; }
    }
}