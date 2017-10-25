using System.Collections.Generic;

namespace ImuExports.Tasks.FieldMIO.Models
{
    public class MIOSpecies
    {

        public long irn { get; set; }

        public string scene { get; set; }

        public int priority { get; set; }

        public string title { get; set; }

        public string description { get; set; }

        public string externalUrl { get; set; }

        public string audioTranscript { get; set; }

        public string audioFilename { get; set; }

        public string thumbnailClass { get; set; }

        public string thumbnailFilename { get; set; }

        public string imageFilename { get; set; }
    }
}