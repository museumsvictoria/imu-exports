global using ImuExports.Configuration;
global using ImuExports.Extensions;
global using ImuExports.Infrastructure;
global using ImuExports.Utilities;
global using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SimpleInjector;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json",
        true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs/log.txt"), rollingInterval: RollingInterval.Day)
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .CreateLogger();

try
{
    // Parse command line options
    CommandOptions.Initialize(args);

    Log.Debug("ImuExports starting up...");
    
    // Create DI container as we need to add it while configuring host
    var container = new Container();

    // Configure host
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            var configSection = context.Configuration.GetSection(AppSettings.SECTION_NAME);

            services
                .Configure<AppSettings>(configSection)
                .Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(20))
                .AddSimpleInjector(container, options =>
                {
                    options.AddHostedService<TaskRunner>();
                    options.AddLogging();
                });
        })
        .UseConsoleLifetime()
        .UseSerilog()
        .Build()
        .UseSimpleInjector(container);

    // Configure and verify DI container
    container
        .Initialize()
        .Verify();
    
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ImuExports startup failed...");
}
finally
{
    Log.CloseAndFlush();
}