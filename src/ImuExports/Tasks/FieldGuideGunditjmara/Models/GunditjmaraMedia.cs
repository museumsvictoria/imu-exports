﻿using System.Collections.Generic;

namespace ImuExports.Tasks.FieldGuideGunditjmara.Models
{
    public abstract class GunditjmaraMedia
    {
        protected GunditjmaraMedia()
        {
            Creators = new List<string>();
            Sources = new List<string>();
        }

        public string Filename { get; set; }

        public string Caption { get; set; }

        public string AlternateText { get; set; }

        public IList<string> Creators { get; set; }

        public IList<string> Sources { get; set; }

        public string Acknowledgment { get; set; }

        public string CopyrightStatus { get; set; }

        public string CopyrightStatement { get; set; }

        public string Licence { get; set; }

        public string LicenceDetails { get; set; }
    }
}