using System;
using System.Collections.Generic;
using System.Linq;
using ImuExports.Infrastructure;
using ImuExports.Config;
using ImuExports.Extensions;
using IMu;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config
{
    class SiteModuleSearchConfig : IModuleSearchConfig
    {
        string IModuleSearchConfig.ModuleName => "esites";

        string[] IModuleSearchConfig.Columns => new[]
        {
            "cat=<ecatalogue:SitSiteRef>.(irn,MdaDataSets_tab,AdmPublishWebNoPassword)",
            "colevent=<ecollectionevents:ColSiteRef>.(cat=<ecatalogue:SitSiteRef>.(irn,MdaDataSets_tab,AdmPublishWebNoPassword))"
        };

        Terms IModuleSearchConfig.Terms
        {
            get
            {
                var terms = new Terms();
                if (GlobalOptions.Options.Ala.ParsedModifiedAfterDate.HasValue)
                {
                    terms.Add("AdmDateModified", GlobalOptions.Options.Ala.ParsedModifiedAfterDate.Value.ToString("MMM dd yyyy"), ">=");
                }
                if (GlobalOptions.Options.Ala.ParsedModifiedBeforeDate.HasValue)
                {
                    terms.Add("AdmDateModified", GlobalOptions.Options.Ala.ParsedModifiedBeforeDate.Value.ToString("MMM dd yyyy"), "<=");
                }

                return terms;
            }
        }

        Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map => map
            .GetMaps("cat")
            .Where(x => x != null && x.GetEncodedStrings("MdaDataSets_tab").Contains(AtlasOfLivingAustraliaConstants.QueryString) && string.Equals(x.GetEncodedString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase))
            .Select(x => long.Parse(x.GetString("irn")))
            .Concat(map
                .GetMaps("colevent")
                .SelectMany(x => x.GetMaps("cat"))
                .Where( x => x != null && x.GetEncodedStrings("MdaDataSets_tab").Contains(AtlasOfLivingAustraliaConstants.QueryString) && string.Equals(x.GetEncodedString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase))
                .Select(x => long.Parse(x.GetString("irn"))))
            .Distinct();
    }
}
