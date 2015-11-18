using System;
using System.Collections.Generic;
using ALAExport.Export.Tasks;
using Serilog;

namespace ALAExport.Export.Infrastructure
{
    public class TaskRunner
    {
        private readonly IEnumerable<ITask> tasks;

        public TaskRunner(IEnumerable<ITask> tasks)
        {
            this.tasks = tasks;
        }

        public void RunAllTasks()
        {
            var importHasFailed = false;

            using (Log.Logger.BeginTimedOperation("Emu data Import starting", "ImportRunner.Run"))
            {
                try
                {
                    // Run all imports
                    foreach (var task in tasks)
                    {
                        if (Program.ImportCanceled)
                            break;

                        task.Run();
                    }
                }
                catch (Exception ex)
                {
                    importHasFailed = true;
                    Log.Logger.Error(ex, "Exception occured running import");
                }

                // Imports have run, finish up, need a fresh session as we may have been waiting a while for imports to complete.
                if (Program.ImportCanceled || importHasFailed)
                    Log.Logger.Information("Import has been stopped prematurely {@Reason}", new { Program.ImportCanceled, importHasFailed });
                else
                    Log.Logger.Information("All imports finished successfully");
            }
        }
    }
}
