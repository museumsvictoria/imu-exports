using System;
using System.Globalization;
using IMu;
using ImuExports.Infrastructure;
using Serilog;

namespace ImuExports.Tasks.FieldGuideGippsland
{
    public class FieldGuideGippslandTask : ImuTaskBase, ITask
    {
        public void Run()
        {
            using (Log.Logger.BeginTimedOperation(string.Format("{0} starting", GetType().Name), string.Format("{0}.Run", GetType().Name)))
            {
                // Cache Irns
                var cachedIrns = this.CacheIrns("ecatalogue", BuildSearchTerms());
            }
        }

        private Terms BuildSearchTerms()
        {
            var searchTerms = new Terms();

            return searchTerms;
        }
    }
}