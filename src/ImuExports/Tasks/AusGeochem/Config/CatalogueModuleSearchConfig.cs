using IMu;

namespace ImuExports.Tasks.AusGeochem.Config;

public class CatalogueModuleSearchConfig : IModuleSearchConfig, IWithTermFilter
{
    private readonly AusGeochemOptions _options = (AusGeochemOptions)CommandOptions.TaskOptions;
    
    string IModuleSearchConfig.ModuleName => "ecatalogue";

    string IModuleSearchConfig.ModuleSelectName => "catalogue";

    string[] IModuleSearchConfig.Columns => new[]
    {
        "irn"
    };

    public IList<KeyValuePair<string, string>> TermFilters { get; set; }

    Terms IModuleSearchConfig.Terms
    {
        get
        {
            var terms = new Terms();
            
            terms.Add("MdaDataSets_tab", AusGeochemConstants.ImuDataSetsQueryString);

            foreach (var termFilter in TermFilters)
            {
                terms.Add(termFilter.Key, termFilter.Value);
            }
            
            if (_options.Application.PreviousDateRun.HasValue)
                terms.Add("AdmDateModified", _options.Application.PreviousDateRun.Value.ToString("MMM dd yyyy"), ">=");

            return terms;
        }
    }

    Func<Map, IEnumerable<long>> IModuleSearchConfig.IrnSelectFunc => map => new[] { map.GetLong("irn") }.ToList();
}