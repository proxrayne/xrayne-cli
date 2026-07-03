using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Cli;
using Cli.Commands;
using Contracts;
using Contracts.Configurations;
using Contracts.Values;
using Infrastructure;
using Data;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args);

    host.ConfigureAppConfiguration((context, configuration) =>
    {
        configuration.SetBasePath(AppContext.BaseDirectory);
        configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        configuration.AddJsonFile(
            $"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: true);
        configuration.AddJsonFile(PathProvider.Paths.JsonConfig, optional: true, reloadOnChange: true);
        configuration.AddEnvFile(PathProvider.Paths.EnvConfig, optional: true);

        configuration.AddEnvironmentVariables();
    });

    host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Map(
            logEvent => logEvent.Timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            (date, writeTo) => writeTo.File(
                path: Path.Combine(PathProvider.Paths.LogsDirectory, $"cli-{date}.log"),
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"),
            sinkMapCountLimit: 1));

    host.ConfigureServices((context, services) =>
    {
        services.AddSingleton(PanelSettings.Parse(BuildPanelBootstrapConfiguration()));
        services.AddCliInfrastructure(context.Configuration);
        services.AddData(GetEnvConnectionString(context.Configuration) ?? context.Configuration.GetConnectionString("Default"));
        services.AddContracts(context.Configuration);

        services.AddCliActions();
    });

    using var app = host.Build();

    Log.Information("XRayne CLI started.");

    var rootCommand = app.Services.GetRequiredService<RootCommandFactory>().Create();

    var configuration = new CommandLineConfiguration(rootCommand);

    return await configuration.InvokeAsync(args);
}
catch (Exception exception)
{
    Log.Fatal(exception, "XRayne CLI terminated unexpectedly.");

    return 1;
}
finally
{
    Log.CloseAndFlush();
}

// TODO: move to utilities
string? GetEnvConnectionString(IConfiguration configuration)
{
    var user = configuration["POSTGRES_USER"];
    var password = configuration["POSTGRES_PASSWORD"];
    var database = configuration["POSTGRES_DB"];
    if (string.IsNullOrWhiteSpace(user)
        || string.IsNullOrWhiteSpace(password)
        || string.IsNullOrWhiteSpace(database))
    {
        return null;
    }

    var port = configuration["POSTGRES_PORT"];
    if (string.IsNullOrWhiteSpace(port))
    {
        port = "5432";
    }

    return $"Host=localhost;Port={port};Username={user};Password={password};Database={database}";
}

IConfiguration BuildPanelBootstrapConfiguration()
{
    return new ConfigurationBuilder()
        .AddEnvFile(PathProvider.Paths.EnvConfig, optional: true)
        .AddEnvironmentVariables()
        .Build();
}
