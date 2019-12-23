using System;
using System.Collections.Generic;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.Config;
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
            var taskOptions = GlobalOptions.TaskOptions as ITaskOptions;
            if (taskOptions != null)
                serviceTasks.Add(taskOptions.TypeOfTask);

            container.RegisterCollection<ITask>(serviceTasks);

            // Register module search configs
            container.RegisterCollection<IModuleSearchConfig>(new[] { typeof(IModuleSearchConfig).Assembly });
            container.RegisterCollection<IModuleDeletionsConfig>(new[] { typeof(IModuleDeletionsConfig).Assembly });

            // Register factories
            container.Register(typeof(IFactory<>), new[] { typeof(IFactory<>).Assembly });

            // Verify registrations
            container.Verify();

            return container;
        }
    }
}
