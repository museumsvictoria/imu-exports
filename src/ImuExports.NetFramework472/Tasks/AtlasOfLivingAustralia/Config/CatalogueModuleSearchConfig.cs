using System;
using System.Collections.Generic;
using System.Linq;
using IMu;
using ImuExports.NetFramework472.Config;
using ImuExports.NetFramework472.Infrastructure;

namespace ImuExports.NetFramework472.Tasks.AtlasOfLivingAustralia.Config
{
    class CatalogueModuleSearchConfig : IModuleSearchConfig
    {
        string IModuleSearchConfig.ModuleName => "ecatalogue";

        string IModuleSearchConfig.ModuleSelectName => "catalogue";

        string[] IModuleSearchConfig.Columns => new[]
        {
            "irn"
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
                terms.Add("ColCategory", "Natural Sciences");
                terms.Add("MdaDataSets_tab", AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString);
                terms.Add("AdmPublishWebNoPassword", "Yes");

                return terms;
            }
        }

        Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map => new[] { map.GetLong("irn") }.ToList();
    }
}
