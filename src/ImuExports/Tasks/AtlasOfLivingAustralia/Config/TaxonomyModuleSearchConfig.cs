using IMu;
using ImuExports.Extensions;
using ImuExports.Tasks.AtlasOfLivingAustralia.Helpers;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config;

public class TaxonomyModuleSearchConfig : IModuleSearchConfig
{
    private readonly AtlasOfLivingAustraliaOptions _options = (AtlasOfLivingAustraliaOptions)CommandOptions.TaskOptions;

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
            
            if (_options.ParsedModifiedAfterDate.HasValue)
                terms.Add("AdmDateModified", _options.ParsedModifiedAfterDate.Value.ToString("MMM dd yyyy"), ">=");
            if (_options.ParsedModifiedBeforeDate.HasValue)
                terms.Add("AdmDateModified", _options.ParsedModifiedBeforeDate.Value.ToString("MMM dd yyyy"), "<=");

            return terms;
        }
    }

    Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map =>
    {
        var irns = new List<long>();
        foreach (var cat in map.GetMaps("cat"))
            if (cat != null &&
                cat.GetTrimStrings("MdaDataSets_tab")
                    .Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString) &&
                string.Equals(cat.GetTrimString("AdmPublishWebNoPassword"), "yes",
                    StringComparison.OrdinalIgnoreCase))
            {
                var identification = MapSearches.GetIdentification(cat);
                var taxonomy = identification?.GetMap("taxa");

                if (taxonomy != null && taxonomy.GetLong("irn") == map.GetLong("irn")) irns.Add(cat.GetLong("irn"));
            }

        return irns;
    };
}