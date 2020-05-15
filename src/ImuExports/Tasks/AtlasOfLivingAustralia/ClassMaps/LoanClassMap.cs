using CsvHelper.Configuration;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps
{
    public sealed class LoanClassMap : ClassMap<Loan>
    {
        public LoanClassMap()
        {
            Map(m => m.CoreId).Name("coreID");
            Map(m => m.Blocked).Name("blocked");
            Map(m => m.Disposition).Name("disposition");
        }
    }
}