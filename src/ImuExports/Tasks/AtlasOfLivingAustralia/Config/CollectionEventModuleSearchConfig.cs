using System;
using System.Collections.Generic;
using System.Linq;
using ImuExports.Infrastructure;
using ImuExports.Config;
using ImuExports.Extensions;
using IMu;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config
{
    class CollectionEventModuleSearchConfig : IModuleSearchConfig
    {
        string IModuleSearchConfig.ModuleName => "ecollectionevents";
        
        string IModuleSearchConfig.ModuleSelectName => "catalogue";

        string[] IModuleSearchConfig.Columns => new[]
        {
            "cat=<ecatalogue:ColCollectionEventRef>.(irn,MdaDataSets_tab,AdmPublishWebNoPassword)"
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
            .Where(x => x != null && x.GetTrimStrings("MdaDataSets_tab").Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString) && string.Equals(x.GetTrimString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.GetLong("irn"));
    }
}
