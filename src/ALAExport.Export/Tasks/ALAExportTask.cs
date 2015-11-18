using ALAExport.Export.Factories;
using ALAExport.Export.Models;

namespace ALAExport.Export.Tasks
{
    public class ALAExportTask : ITask
    {
        private readonly IFactory<Occurrence> occurrenceFactory;

        public ALAExportTask(IFactory<Occurrence> occurrenceFactory)
        {
            this.occurrenceFactory = occurrenceFactory;
        }

        public void Run()
        {
        }
    }
}