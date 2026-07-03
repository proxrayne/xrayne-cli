using Github;
using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cli.Output;
using Cli.Services;
using Cli.Services.Contracts;
using Cli.Values;
using Contracts.Values;
using Infrastructure.Utilities;

namespace Cli.Commands;

public sealed class InfoCommand : Command
{
    public InfoCommand(IServiceProvider serviceProvider)
        : base("info", "Print XRayne CLI, project, and API runtime information")
    {
        SetAction(async (_, cancellationToken) =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            return await ExecuteAsync(scope.ServiceProvider, cancellationToken);
        });
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var console = serviceProvider.GetRequiredService<ICliConsole>();
        var logger = serviceProvider.GetRequiredService<ILogger<InfoCommand>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var apiInstallationService = serviceProvider.GetRequiredService<IApiInstallationService>();
        var repository = new GitHubRepository(CliDefaults.XRayneRepositoryUrl);

        try
        {
            var apiStatus = await GetApiStatusAsync(apiInstallationService, cancellationToken);
            var apiPort = GetConfigurationValue(configuration, "PORT", CliDefaults.DefaultApiPort.ToString());
            var serverIp = NetworkAddress.GetLocalServerIpAddress();
            var apiEndpoint = GetApiEndpoint(configuration, serverIp, apiPort);
            var cliVersion = GetVersion();
            var apiVersion = CliDefaults.ExtractApiImageVersion(configuration[CliDefaults.ApiImageVariable] ?? string.Empty);
            var updateStatus = await GetUpdateStatusAsync(
                repository,
                cliVersion,
                apiVersion,
                cancellationToken);

            console.Header("XRayne CLI information");
            console.Value("CLI version", cliVersion);
            console.Value("CLI directory", PathProvider.GetCliDirectory()?.FullName ?? AppContext.BaseDirectory);
            console.Value("Project directory", PathProvider.GetProjectDirectory());

            console.Section("API");
            console.Value("Status", apiStatus);
            console.Value("Server IP", serverIp);
            console.Value("Panel URL", $"{apiEndpoint}/");
            console.Value("API URL", $"{apiEndpoint}/api");
            console.Value("Docker image", GetConfigurationValue(configuration, CliDefaults.ApiImageVariable, "(unknown)"));

            console.Section("Updates");
            console.Value("Latest release", updateStatus.LatestRelease);
            console.Value("CLI update", updateStatus.CliUpdate);
            console.Value("API update", updateStatus.ApiUpdate);

            console.Section("Project files");
            console.Value("Environment file", FormatPathState(PathProvider.Paths.EnvConfig));
            console.Value("Config file", FormatPathState(PathProvider.Paths.JsonConfig));
            console.Value("Compose file", FormatPathState(PathProvider.Paths.DockerCompose));
            console.Value("Runtime schema", GetRuntimeSchemaStatus(configuration));
            console.Value("Logs directory", FormatPathState(PathProvider.Paths.LogsDirectory));
            console.Value("Xray directory", FormatPathState(PathProvider.Paths.XrayDirectory));
            console.Value("PostgreSQL data", FormatPathState(PathProvider.Paths.PostgresDirectory));

            return 0;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "CLI information lookup failed.");
            console.Error(exception.Message);

            return 1;
        }
    }

    private static async Task<UpdateStatus> GetUpdateStatusAsync(
        GitHubRepository gitHubRepository,
        string cliVersion,
        string? apiVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            var release = await gitHubRepository.GetReleaseAsync(CliDefaults.LatestVersion, cancellationToken);
            var latestApiVersion = SanitizeDockerTag(release.TagName);

            var cliUpdate = string.Equals(cliVersion, release.TagName, StringComparison.Ordinal)
                ? "not available"
                : $"available ({cliVersion} -> {release.TagName})";

            var apiUpdate = string.IsNullOrWhiteSpace(apiVersion)
                ? $"not installed (latest: {latestApiVersion})"
                : string.Equals(apiVersion, latestApiVersion, StringComparison.Ordinal)
                    ? "not available"
                    : $"available ({apiVersion} -> {latestApiVersion})";

            return new UpdateStatus(release.TagName, cliUpdate, apiUpdate);
        }
        catch (Exception exception)
        {
            var message = exception.GetBaseException().Message;

            return new UpdateStatus(
                $"unavailable ({message})",
                "unknown",
                string.IsNullOrWhiteSpace(apiVersion) ? "not installed" : "unknown");
        }
    }

    private static async Task<string> GetApiStatusAsync(
        IApiInstallationService apiInstallationService,
        CancellationToken cancellationToken)
    {
        try
        {
            return await apiInstallationService.IsApiRunningAsync(cancellationToken)
                ? "running"
                : "stopped";
        }
        catch
        {
            return "not installed";
        }
    }

    private static string GetVersion()
    {
        var assembly = typeof(InfoCommand).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+', 2)[0];
        }

        return assembly.GetName().Version?.ToString() ?? "unknown";
    }

    private static string GetConfigurationValue(
        IConfiguration configuration,
        string key,
        string fallback)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string GetApiEndpoint(
        IConfiguration configuration,
        string serverIp,
        string apiPort)
    {
        var certificateMode = configuration["CERTIFICATE_MODE"];
        var certificateIdentifier = configuration["CERTIFICATE_IDENTIFIER"];
        var isHttps = !string.IsNullOrWhiteSpace(configuration["CERT_PUBLIC_KEY_PATH"])
            && !string.IsNullOrWhiteSpace(configuration["CERT_PRIVATE_KEY_PATH"]);
        var host = string.Equals(certificateMode, "domain", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(certificateIdentifier)
                ? certificateIdentifier
                : serverIp;
        var scheme = isHttps ? "https" : "http";

        return $"{scheme}://{host}:{apiPort}";
    }

    private static string SanitizeDockerTag(string value)
    {
        var chars = value.Select(character =>
            char.IsAsciiLetterOrDigit(character) || character is '_' or '.' or '-'
                ? character
                : '-').ToArray();
        var tag = new string(chars).Trim('-');

        return string.IsNullOrWhiteSpace(tag) ? "latest" : tag;
    }

    private static string FormatPathState(string path)
    {
        return File.Exists(path) || Directory.Exists(path)
            ? path
            : $"{path} (missing)";
    }

    private static string GetRuntimeSchemaStatus(IConfiguration configuration)
    {
        var current = configuration.GetValue("Runtime:SchemaVersion", 0);

        return current == RuntimeSchemaCatalog.LatestSchemaVersion
            ? current.ToString()
            : $"{current} (latest: {RuntimeSchemaCatalog.LatestSchemaVersion})";
    }

    private sealed record UpdateStatus(
        string LatestRelease,
        string CliUpdate,
        string ApiUpdate);
}
