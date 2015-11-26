using ImuExports.Tasks.AtlasOfLivingAustralia;
using SimpleInjector;

namespace ImuExports.Infrastructure
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
                typeof(AtlasOfLivingAustraliaTask)
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
