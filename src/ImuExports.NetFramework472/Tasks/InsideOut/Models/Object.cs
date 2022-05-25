using System.Collections.Generic;

namespace ImuExports.NetFramework472.Tasks.InsideOut.Models
{
    public class Object
    {
        public long Irn { get; set; }

        public string Scene { get; set; }

        public int Priority { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string ExternalUrl { get; set; }

        public Thumbnail Thumbnail { get; set; }

        public Image Image { get; set; }

        public IList<Audio> Audio { get; set; }
    }
}