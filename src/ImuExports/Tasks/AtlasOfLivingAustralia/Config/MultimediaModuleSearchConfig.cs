using System;
using System.Collections.Generic;
using System.Linq;
using ImuExports.Infrastructure;
using ImuExports.Config;
using ImuExports.Extensions;
using IMu;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config
{
    class MultimediaModuleSearchConfig : IModuleSearchConfig
    {
        string IModuleSearchConfig.ModuleName => "emultimedia";

        string[] IModuleSearchConfig.Columns => new[]
        {
            "cat=<ecatalogue:MulMultiMediaRef_tab>.(irn,MdaDataSets_tab)"
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
                terms.Add("MdaDataSets_tab", "Atlas of Living Australia");
                terms.Add("AdmPublishWebNoPassword", "Yes");

                return terms;
            }
        }

        Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map => map
            .GetMaps("cat")
            .Where(x => x != null && x.GetEncodedStrings("MdaDataSets_tab").Contains(AtlasOfLivingAustraliaConstants.QueryString))
            .Select(x => long.Parse(x.GetString("irn")));
    }
}
