using System;
using System.Collections.Generic;
using System.Linq;
using ImuExports.Infrastructure;
using ImuExports.Config;
using IMu;
using ImuExports.Extensions;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Config
{
    class CatalogueModuleDeletionsConfig : IModuleDeletionsConfig
    {
        string IModuleDeletionsConfig.ModuleName => "ecatalogue";

        string[] IModuleDeletionsConfig.Columns => new[]
        {
            "ColRegPrefix",
            "ColRegNumber",
            "ColRegPart",
            "ColDiscipline",
        };

        Terms IModuleDeletionsConfig.Terms
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
                terms.Add("ColCategory", "Natural Sciences");
                terms.Add("MdaDataSets_tab", "Atlas of Living Australia");
                terms.Add("AdmPublishWebNoPassword", "No");

                return terms;
            }
        }

        Func<Map, IEnumerable<string>> IModuleDeletionsConfig.SelectFunc => map => new[]
        {
            string.IsNullOrWhiteSpace(map.GetTrimString("ColRegPart"))
                ? $"urn:lsid:ozcam.taxonomy.org.au:NMV:{map.GetTrimString("ColDiscipline")}:PreservedSpecimen:{map.GetTrimString("ColRegPrefix")}{map.GetTrimString("ColRegNumber")}"
                : $"urn:lsid:ozcam.taxonomy.org.au:NMV:{map.GetTrimString("ColDiscipline")}:PreservedSpecimen:{map.GetTrimString("ColRegPrefix")}{map.GetTrimString("ColRegNumber")}-{map.GetTrimString("ColRegPart")}"
        }.ToList();
    }
}
