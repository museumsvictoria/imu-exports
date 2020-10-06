using System;
using System.Collections.Generic;
using ImuExports.Infrastructure;
using ImuExports.Config;
using ImuExports.Extensions;
using IMu;
using ImuExports.Tasks.AtlasOfLivingAustralia.Helpers;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config
{
    class TaxonomyModuleSearchConfig : IModuleSearchConfig
    {
        string IModuleSearchConfig.ModuleName => "etaxonomy";
        
        string IModuleSearchConfig.ModuleSelectName => "catalogue";

        string[] IModuleSearchConfig.Columns => new[]
        {
            "irn",
            "cat=<ecatalogue:TaxTaxonomyRef_tab>.(irn,MdaDataSets_tab,AdmPublishWebNoPassword,identifications=[IdeTypeStatus_tab,IdeCurrentNameLocal_tab,taxa=TaxTaxonomyRef_tab.(irn)])"
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

        Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map =>
        {
            var irns = new List<long>();
            foreach (var cat in map.GetMaps("cat"))
            {
                if (cat != null &&
                    cat.GetTrimStrings("MdaDataSets_tab").Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString) &&
                    string.Equals(cat.GetTrimString("AdmPublishWebNoPassword"), "yes",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var identification = MapSearches.GetIdentification(cat);
                    var taxonomy = identification?.GetMap("taxa");

                    if (taxonomy != null && taxonomy.GetLong("irn") == map.GetLong("irn"))
                    {
                        irns.Add(cat.GetLong("irn"));
                    }
                }
            }
            return irns;
        };
    }
}
