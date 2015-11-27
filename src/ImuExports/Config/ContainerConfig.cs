using System;
using System.Collections.Generic;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia;
using SimpleInjector;

namespace ImuExports.Config
{
    public static class ContainerConfig
    {
        public static Container Initialize()
        {
            var container = new Container();

            // Register task runner
            container.Register<TaskRunner>();
            
            // Register all tasks
            var serviceTasks = new List<Type>();

            // Add invoked task
            var taskOptions = Config.TaskOptions as ITaskOptions;
            if (taskOptions != null)
                serviceTasks.Add(taskOptions.TypeOfTask);

            container.RegisterCollection<ITask>(serviceTasks);

            // Register factories
            container.Register(typeof(IFactory<>), new[] { typeof(IFactory<>).Assembly });

            // Verify registrations
            container.Verify();

            return container;
        }
    }
}
