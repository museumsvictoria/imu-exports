using IMu;
using ImuExports.Extensions;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config;

public class CollectionEventModuleSearchConfig : IModuleSearchConfig
{
    private readonly AtlasOfLivingAustraliaOptions _options = (AtlasOfLivingAustraliaOptions)CommandOptions.TaskOptions;

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
            
            if (_options.ParsedModifiedAfterDate.HasValue)
                terms.Add("AdmDateModified", _options.ParsedModifiedAfterDate.Value.ToString("MMM dd yyyy"), ">=");
            if (_options.ParsedModifiedBeforeDate.HasValue)
                terms.Add("AdmDateModified", _options.ParsedModifiedBeforeDate.Value.ToString("MMM dd yyyy"), "<=");

            return terms;
        }
    }

    Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map => map
        .GetMaps("cat")
        .Where(x => x != null &&
                    x.GetTrimStrings("MdaDataSets_tab")
                        .Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString) &&
                    string.Equals(x.GetTrimString("AdmPublishWebNoPassword"), "yes",
                        StringComparison.OrdinalIgnoreCase))
        .Select(x => x.GetLong("irn"));
}