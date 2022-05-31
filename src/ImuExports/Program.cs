global using ImuExports.Configuration;
global using ImuExports.Infrastructure;
using Serilog;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json",
        optional: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs/log.txt"),
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("CollectionsOnline Tasks starting up...");
    
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            var configSection = context.Configuration.GetSection(AppSettings.SectionName);

            services.Configure<AppSettings>(configSection);
            services.AddHostedService<TaskRunner>();
        })
        .UseConsoleLifetime()
        .UseSerilog()
        .Build();
    
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CollectionsOnline Tasks startup failed...");
}
finally
{
    Log.CloseAndFlush();
}
