using ImuExports.Configuration;
using ImuExports.Infrastructure;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json",
        optional: true)
    .AddEnvironmentVariables()
    .Build();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var configSection = context.Configuration.GetSection(AppSettings.SectionName);
        services.Configure<AppSettings>(configSection);
        services.AddHostedService<TaskRunner>();
    })
    .UseConsoleLifetime()
    .Build();

await host.RunAsync();

