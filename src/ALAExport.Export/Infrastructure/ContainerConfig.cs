using ALAExport.Export.Factories;
using ALAExport.Export.Tasks;
using SimpleInjector;

namespace ALAExport.Export.Infrastructure
{
    public static class ContainerConfig
    {
        public static Container Initialize()
        {
            var container = new Container();

            // Register task runner
            container.Register<TaskRunner>();
            
            // Register tasks
            container.RegisterCollection<ITask>(new[]
            {
                typeof(ALAExportTask)
            });

            // Register factories
            container.Register(typeof(IFactory<>), new[] { typeof(IFactory<>).Assembly });

            // Register everything else
            container.Register<IImuSessionProvider, ImuSessionProvider>();

            // Verify registrations
            container.Verify();

            return container;
        }
    }
}
