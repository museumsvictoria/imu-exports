using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImuExports.Configuration;

public static class ServicesConfig
{
    public static IServiceCollection AddTask(this IServiceCollection services)
    {
        services.TryAdd(new ServiceDescriptor(typeof(ITask), CommandOptions.TaskOptions.TypeOfTask,
            ServiceLifetime.Singleton));

        return services;
    }
}