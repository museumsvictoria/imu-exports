using IMu;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config;

public class SiteModuleSearchConfig : IModuleSearchConfig
{
    private readonly AtlasOfLivingAustraliaOptions _options = (AtlasOfLivingAustraliaOptions)CommandOptions.TaskOptions;

    string IModuleSearchConfig.ModuleName => "esites";

    string IModuleSearchConfig.ModuleSelectName => "catalogue";

    string[] IModuleSearchConfig.Columns => new[]
    {
        "irn",
        "cat=<ecatalogue:SitSiteRef>.(irn,MdaDataSets_tab,AdmPublishWebNoPassword)",
        "colevent=<ecollectionevents:ColSiteRef>.(irn,cat=<ecatalogue:ColCollectionEventRef>.(irn,MdaDataSets_tab,AdmPublishWebNoPassword))"
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
        // Find catalogue irns joined directly to site module 
        var sitSiteRefIrns = new List<long>();
        foreach (var catalogue in map.GetMaps("cat"))
            if (catalogue != null &&
                catalogue.GetTrimStrings("MdaDataSets_tab")
                    .Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString) &&
                string.Equals(catalogue.GetTrimString("AdmPublishWebNoPassword"), "yes",
                    StringComparison.OrdinalIgnoreCase))
                sitSiteRefIrns.Add(catalogue.GetLong("irn"));

        // Find catalogue irns indirectly linked to sites via collection event module
        var colSiteRefIrns = new List<long>();
        foreach (var collectionEvent in map.GetMaps("colevent"))
        {
            var colEventIrns = new List<long>();

            foreach (var cat in collectionEvent.GetMaps("cat"))
                if (cat != null &&
                    cat.GetTrimStrings("MdaDataSets_tab")
                        .Contains(AtlasOfLivingAustraliaConstants.ImuAtlasOfLivingAustraliaQueryString) &&
                    string.Equals(cat.GetTrimString("AdmPublishWebNoPassword"), "yes",
                        StringComparison.OrdinalIgnoreCase))
                    colEventIrns.Add(cat.GetLong("irn"));

            colSiteRefIrns.AddRange(colEventIrns);
        }

        return sitSiteRefIrns.Concat(colSiteRefIrns).Distinct();
    };
}