using SimpleInjector;

namespace ImuExports.Configuration;

public static class ContainerConfig
{
    public static Container Initialize(this Container container)
    {
        // Register task
        container.Register(typeof(ITask), CommandOptions.TaskOptions.TypeOfTask, Lifestyle.Singleton);

        // Register module search configs
        container.Collection.Register<IModuleSearchConfig>(typeof(IModuleSearchConfig).Assembly);
        container.Collection.Register<IModuleDeletionsConfig>(typeof(IModuleDeletionsConfig).Assembly);

        // Register factories
        container.Register(typeof(IFactory<>), typeof(IFactory<>).Assembly, Lifestyle.Singleton);

        return container;
    }
}