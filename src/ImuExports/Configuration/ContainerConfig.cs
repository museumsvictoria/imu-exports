using System.Text.Json;
using System.Text.Json.Serialization;
using ImuExports.Tasks.AusGeochem;
using ImuExports.Tasks.AusGeochem.Endpoints;
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
            .Where(type => type.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IImuFactory<>) || i.GetGenericTypeDefinition() == typeof(IFactory<>))))
            .Select(type => new { Service = type.GetInterfaces().Single(), Implementation = type })
            .ToList();
        
        foreach (var registration in factoryRegistrations)
        {
            container.Register(registration.Service, registration.Implementation, Lifestyle.Singleton);
        }
        
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
            
            container.Register<IAusGeochemClient, AusGeochemClient>(Lifestyle.Singleton);
            container.Register<IAuthenticateEndpoint, AuthenticateEndpoint>(Lifestyle.Singleton);
            container.Register<ISampleEndpoint, SampleEndpoint>(Lifestyle.Singleton);
            container.Register<IImageEndpoint, ImageEndpoint>(Lifestyle.Singleton);
            container.Register<ILocationKindEndpoint, LocationKindEndpoint>(Lifestyle.Singleton);
            container.Register<IMaterialEndpoint, MaterialEndpoint>(Lifestyle.Singleton);
            container.Register<ISampleKindEndpoint, SampleKindEndpoint>(Lifestyle.Singleton);
        }
        
        return container;
    }
}