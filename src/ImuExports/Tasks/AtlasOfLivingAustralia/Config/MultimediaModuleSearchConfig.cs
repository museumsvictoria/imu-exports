using IMu;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config;

public class MultimediaModuleSearchConfig : IModuleSearchConfig
{
    private readonly AtlasOfLivingAustraliaOptions _options = (AtlasOfLivingAustraliaOptions)CommandOptions.TaskOptions;
    
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
            
            if (_options.ParsedModifiedAfterDate.HasValue)
                terms.Add("AdmDateModified", _options.ParsedModifiedAfterDate.Value.ToString("MMM dd yyyy"), ">=");
            if (_options.ParsedModifiedBeforeDate.HasValue)
                terms.Add("AdmDateModified", _options.ParsedModifiedBeforeDate.Value.ToString("MMM dd yyyy"), "<=");
            terms.Add("MdaDataSets_tab", AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString);
            terms.Add("AdmPublishWebNoPassword", "Yes");

            return terms;
        }
    }

    Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map => map
        .GetMaps("cat")
        .Where(x => x != null && x.GetTrimStrings("MdaDataSets_tab")
            .Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString))
        .Select(x => x.GetLong("irn"));
}