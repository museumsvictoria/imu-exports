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

            container.Register<TaskRunner>();
            container.RegisterCollection<ITask>(new[]
            {
                typeof(ALAExportTask)
            });
            container.Register(typeof(IFactory<>), new[] { typeof(IFactory<>).Assembly });

            container.Verify();

            return container;
        }
    }
}
