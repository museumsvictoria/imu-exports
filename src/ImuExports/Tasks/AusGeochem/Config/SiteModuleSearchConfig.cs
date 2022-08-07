using IMu;

namespace ImuExports.Tasks.AusGeochem.Config;

public class SiteModuleSearchConfig : IModuleSearchConfig, IWithTermFilter
{
    private readonly AusGeochemOptions _options = (AusGeochemOptions)CommandOptions.TaskOptions;

    string IModuleSearchConfig.ModuleName => "esites";

    string IModuleSearchConfig.ModuleSelectName => "catalogue";

    string[] IModuleSearchConfig.Columns => new[]
    {
        "irn",
        "cat=<ecatalogue:SitSiteRef>.(irn,ColDiscipline,MdaDataSets_tab,AdmPublishWebNoPassword)"
    };

    public IList<KeyValuePair<string, string>> TermFilters { get; set; }

    Terms IModuleSearchConfig.Terms
    {
        get
        {
            var terms = new Terms();

            if (_options.Application.PreviousDateRun.HasValue)
                terms.Add("AdmDateModified", _options.Application.PreviousDateRun.Value.ToString("MMM dd yyyy"), ">=");

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
                    .Contains(AusGeochemConstants.ImuDataSetsQueryString) &&
                TermFilters.All(filter => string.Equals(catalogue.GetTrimString(filter.Key), filter.Value)))
                sitSiteRefIrns.Add(catalogue.GetLong("irn"));

        return sitSiteRefIrns.Distinct();
    };
}