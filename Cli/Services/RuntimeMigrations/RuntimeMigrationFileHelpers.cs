using System.Text.Json.Nodes;
using Cli.Values;
using Contracts.Values;
using Data.Utilities;

namespace Cli.Services.RuntimeMigrations;

internal static class RuntimeMigrationFileHelpers
{
    public static async Task<int> ReadSchemaVersionAsync(
        ProjectPaths paths,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(paths.JsonConfig))
        {
            return 0;
        }

        try
        {
            var json = await File.ReadAllTextAsync(paths.JsonConfig, cancellationToken);
            var root = JsonNode.Parse(json);
            var version = root?["Runtime"]?["SchemaVersion"]?.GetValue<int>();

            return version ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public static async Task SetSchemaVersionAsync(
        ProjectPaths paths,
        int schemaVersion,
        CancellationToken cancellationToken)
    {
        await JsonConfig.SetAsync(
            "Runtime:SchemaVersion",
            schemaVersion,
            paths.JsonConfig,
            cancellationToken);
    }

    public static async Task<string> GetApiPortAsync(
        ProjectPaths paths,
        CancellationToken cancellationToken)
    {
        var apiPort = File.Exists(paths.EnvConfig)
            ? await EnvConfig.GetAsync("PORT", paths.EnvConfig, cancellationToken)
            : null;

        if (string.IsNullOrWhiteSpace(apiPort) && File.Exists(paths.EnvConfig))
        {
            apiPort = await EnvConfig.GetAsync("API_PORT", paths.EnvConfig, cancellationToken);
        }

        return !string.IsNullOrWhiteSpace(apiPort)
            ? apiPort
            : CliDefaults.DefaultApiPort.ToString();
    }

    public static bool HasCertificate(ProjectPaths paths)
    {
        if (!File.Exists(paths.JsonConfig))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(paths.JsonConfig);
            var root = JsonNode.Parse(json);

            var envLines = File.Exists(paths.EnvConfig)
                ? File.ReadAllLines(paths.EnvConfig)
                : [];

            return !string.IsNullOrWhiteSpace(EnvConfig.Get(envLines, "CERTIFICATE_CERT_NAME"))
                || !string.IsNullOrWhiteSpace(EnvConfig.Get(envLines, "CERT_PUBLIC_KEY_PATH"))
                || !string.IsNullOrWhiteSpace(root?["Certificate"]?["CertName"]?.GetValue<string>())
                || !string.IsNullOrWhiteSpace(root?["Kestrel"]?["Endpoints"]?["Https"]?["Url"]?.GetValue<string>());
        }
        catch
        {
            return false;
        }
    }

    public static async Task SetEnvValueAsync(
        string envPath,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        await EnvConfig.SetAsync(key, value, envPath, cancellationToken);
    }
}
