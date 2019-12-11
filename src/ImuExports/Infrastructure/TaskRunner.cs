using System;
using System.Collections.Generic;
using Serilog;

namespace ImuExports.Infrastructure
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

            using (Log.Logger.BeginTimedOperation("Imu export tasks starting", "TaskRunner.RunAllTasks"))
            {
                try
                {
                    // Run all tasks
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
                    Log.Logger.Error(ex, "Exception occured running export");
                }

                if (Program.ImportCanceled || importHasFailed)
                    Log.Logger.Information("Imu export tasks have been stopped prematurely {@Reason}", new { Program.ImportCanceled, importHasFailed });
                else
                    Log.Logger.Information("All Imu export tasks finished successfully");
            }

            Log.CloseAndFlush();
        }
    }
}