using System;
using System.Collections.Generic;
using System.Linq;
using IMu;
using ImuExports.NetFramework472.Config;
using ImuExports.NetFramework472.Extensions;
using ImuExports.NetFramework472.Infrastructure;

namespace ImuExports.NetFramework472.Tasks.AtlasOfLivingAustralia.Config
{
    public class MultimediaModuleSearchConfig : IModuleSearchConfig
    {
        string IModuleSearchConfig.ModuleName => "emultimedia";

        string IModuleSearchConfig.ModuleSelectName => "catalogue";

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
                terms.Add("MdaDataSets_tab", AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString);
                terms.Add("AdmPublishWebNoPassword", "Yes");

                return terms;
            }
        }

        Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map => map
            .GetMaps("cat")
            .Where(x => x != null && x.GetTrimStrings("MdaDataSets_tab").Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString))
            .Select(x => x.GetLong("irn"));
    }
}
