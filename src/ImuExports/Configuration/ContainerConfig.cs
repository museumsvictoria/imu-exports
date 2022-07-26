using ImuExports.Tasks.AusGeochem;
using SimpleInjector;

namespace ImuExports.Configuration;

public static class ContainerConfig
{
    public static Container Initialize(this Container container)
    {
        // Register task
        container.Register(typeof(ITask), CommandOptions.TaskOptions.TypeOfTask, Lifestyle.Singleton);

        // Register module search configs
        var moduleSearchTypes = typeof(IModuleSearchConfig).Assembly.GetExportedTypes()
            .Where(type => type.Namespace != null &&
                           type.Namespace.StartsWith(CommandOptions.TaskOptions.GetType().Namespace ?? string.Empty))
            .Where(type => type.GetInterfaces().Contains(typeof(IModuleSearchConfig)))
            .ToList();
        
        if(moduleSearchTypes.Any())
            container.Collection.Register<IModuleSearchConfig>(moduleSearchTypes);

        // Register factories
        var factoryRegistrations = typeof(IFactory<>).Assembly.GetExportedTypes()
            .Where(type => type.Namespace != null &&
                           type.Namespace.StartsWith(CommandOptions.TaskOptions.GetType().Namespace ?? string.Empty))
            .Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFactory<>)))
            .Select(type => new { Service = type.GetInterfaces().Single(), Implementation = type });

        foreach (var registration in factoryRegistrations)
        {
            container.Register(registration.Service, registration.Implementation, Lifestyle.Singleton);
        }
        
        // AusGeochemTask specific
        if (CommandOptions.TaskOptions.TypeOfTask == typeof(AusGeochemTask))
        {
            container.Register<IAusGeochemClient, AusGeochemClient>(Lifestyle.Singleton);            
        }
        
        return container;
    }
}