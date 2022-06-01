using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImuExports.Configuration;

public static class ServicesConfig
{
    public static IServiceCollection AddCommandOptions(this IServiceCollection services)
    {
        if (CommandOptions.TaskOptions != null)
        {
            services.TryAdd(new ServiceDescriptor(typeof(ITask), CommandOptions.TaskOptions.TypeOfTask,
                ServiceLifetime.Singleton));
            services.TryAdd(new ServiceDescriptor(typeof(ITaskOptions), CommandOptions.TaskOptions.GetType(),
                ServiceLifetime.Singleton));
        }

        return services;
    }
}