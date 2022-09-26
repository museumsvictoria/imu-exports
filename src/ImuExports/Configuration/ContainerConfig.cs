using System.Text.Json;
using System.Text.Json.Serialization;
using ImuExports.Tasks.AusGeochem;
using RestSharp;
using RestSharp.Serializers.Json;
using SimpleInjector;

namespace ImuExports.Configuration;

public static class ContainerConfig
{
    public static Container Initialize(this Container container, AppSettings appSettings)
    {
        // Register task
        container.Register(typeof(ITask), CommandOptions.TaskOptions.TypeOfTask, Lifestyle.Singleton);

        // Register all search configs
        container.RegisterAllByInterface(typeof(IModuleSearchConfig), Lifestyle.Singleton);

        // Register Factories
        container.RegisterAll(type => type.Name.Contains("Factory") && !type.Name.Contains("ImuFactory"), Lifestyle.Singleton);

        // Register IMu factories
        container.RegisterAll(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IImuFactory<>), Lifestyle.Singleton);

        // AusGeochemTask specific
        if (CommandOptions.TaskOptions.TypeOfTask == typeof(AusGeochemTask))
        {
            container.Register(() =>
            {
                var client = new RestClient(appSettings.AusGeochem.BaseUrl);

                client.UseSystemTextJson(new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                return client;
            }, Lifestyle.Singleton);

            container.Register<IAusGeochemApiClient, AusGeochemApiApiClient>(Lifestyle.Singleton);
            
            // Register Endpoints
            container.RegisterAll(type => type.Name.Contains("Endpoint"), Lifestyle.Singleton);
            
            // Register API Handlers
            container.RegisterAll(type => type.Name.Contains("ApiHandler"), Lifestyle.Singleton);
        }

        return container;
    }

    private static void RegisterAllByInterface(this Container container, Type type, Lifestyle lifestyle = null)
    {
        var types = typeof(ContainerConfig).Assembly.GetExportedTypes()
            .Where(t => t.Namespace != null &&
                        t.Namespace.StartsWith(CommandOptions.TaskOptions.GetType().Namespace ?? string.Empty))
            .Where(t => t.GetInterfaces().Contains(type))
            .ToList();

        if (types.Any())
            container.Collection.Register(type, types, lifestyle ?? Lifestyle.Transient);
    }

    private static void RegisterAll(this Container container, Func<Type, bool> interfaceFilter = null,
        Lifestyle lifestyle = null)
    {
        var registrations = typeof(ContainerConfig).Assembly.GetExportedTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith(CommandOptions.TaskOptions.GetType().Namespace ?? string.Empty));

        if (interfaceFilter != null)
        {
            registrations = registrations.Where(t => t.GetInterfaces().Any(interfaceFilter));
        }

        foreach (var registration in registrations.Select(t => new { Service = t.GetInterfaces().Single(), Implementation = t }))
        {
            container.Register(registration.Service, registration.Implementation, lifestyle ?? Lifestyle.Transient);
        }
    }
}