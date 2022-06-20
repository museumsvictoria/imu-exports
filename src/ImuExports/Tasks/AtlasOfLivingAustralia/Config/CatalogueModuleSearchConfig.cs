using IMu;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config;

public class CatalogueModuleSearchConfig : IModuleSearchConfig
{
    private readonly AtlasOfLivingAustraliaOptions _options = (AtlasOfLivingAustraliaOptions)CommandOptions.TaskOptions;
    
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
            
            if (_options.ParsedModifiedAfterDate.HasValue)
                terms.Add("AdmDateModified", _options.ParsedModifiedAfterDate.Value.ToString("MMM dd yyyy"), ">=");
            if (_options.ParsedModifiedBeforeDate.HasValue)
                terms.Add("AdmDateModified", _options.ParsedModifiedBeforeDate.Value.ToString("MMM dd yyyy"), "<=");
            terms.Add("ColCategory", "Natural Sciences");
            terms.Add("MdaDataSets_tab", AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString);
            terms.Add("AdmPublishWebNoPassword", "Yes");

            return terms;
        }
    }

    Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map => new[] { map.GetLong("irn") }.ToList();
}