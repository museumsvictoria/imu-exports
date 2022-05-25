using System;
using System.Collections.Generic;

namespace ImuExports.NetFramework472.Tasks.WikimediaCommons.Models
{
    public class Media
    {
        public DateTime DateModified { get; set; }

        public string Caption { get; set; }

        public List<string> Creators { get; set; }

        public List<string> Sources { get; set; }

        public string Credit { get; set; }

        public string RightsStatement { get; set; }

        public string RightsStatus { get; set; }

        public string Licence { get; set; }

        public string LicenceDetails { get; set; }

        public bool PermissionRequired { get; set; }

        public string AlternativeText { get; set; }

        public ImageMediaFile Original { get; set; }

        public ImageMediaFile Thumbnail { get; set; }
    }

    public class ImageMediaFile
    {
        public string Uri { get; set; }
    }
}