using System.Text;
using Cli.Values;
using Data.Utilities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Cli.Services.RuntimeMigrations;

internal sealed class V2AddStandaloneUiServiceMigration : IRuntimeMigration
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public int FromVersion => 1;

    public int ToVersion => 2;

    public string Name => "Add standalone UI service";

    public async Task UpAsync(
        RuntimeMigrationContext context,
        CancellationToken cancellationToken)
    {
        var apiImage = File.Exists(context.Paths.EnvConfig)
            ? await EnvConfig.GetAsync(CliDefaults.ApiImageVariable, context.Paths.EnvConfig, cancellationToken)
            : null;
        var version = CliDefaults.ExtractApiImageVersion(apiImage ?? string.Empty) ?? CliDefaults.LatestVersion;

        await RuntimeMigrationFileHelpers.SetEnvValueAsync(
            context.Paths.EnvConfig,
            CliDefaults.UiImageVariable,
            CliDefaults.GetUiImageName(version),
            cancellationToken);
        await RuntimeMigrationFileHelpers.SetEnvValueAsync(
            context.Paths.EnvConfig,
            "UI_PORT",
            CliDefaults.DefaultUiPort.ToString(),
            cancellationToken);

        if (!File.Exists(context.Paths.DockerCompose))
        {
            return;
        }

        var compose = await LoadComposeAsync(context.Paths.DockerCompose, cancellationToken);
        var services = GetOrCreateMap(compose, "services");
        services["ui"] = CreateUiService(version);

        await SaveComposeAsync(context.Paths.DockerCompose, compose, cancellationToken);
    }

    public async Task DownAsync(
        RuntimeMigrationContext context,
        CancellationToken cancellationToken)
    {
        await EnvConfig.RemoveAsync(CliDefaults.UiImageVariable, context.Paths.EnvConfig, cancellationToken);
        await EnvConfig.RemoveAsync("UI_PORT", context.Paths.EnvConfig, cancellationToken);

        if (!File.Exists(context.Paths.DockerCompose))
        {
            return;
        }

        var compose = await LoadComposeAsync(context.Paths.DockerCompose, cancellationToken);
        var services = GetOrCreateMap(compose, "services");
        services.Remove("ui");

        await SaveComposeAsync(context.Paths.DockerCompose, compose, cancellationToken);
    }

    private static Dictionary<string, object?> CreateUiService(string imageTag)
    {
        return new Dictionary<string, object?>
        {
            ["image"] = $"${{UI_IMAGE:-{CliDefaults.GetUiImageName(imageTag)}}}",
            ["container_name"] = "xrayne-ui",
            ["ports"] = new[] { "${UI_PORT:-8080}:80" },
            ["environment"] = new Dictionary<string, object?>
            {
                ["API_UPSTREAM"] = "http://host.docker.internal:${PORT:-5000}"
            },
            ["extra_hosts"] = new[] { "host.docker.internal:host-gateway" },
            ["depends_on"] = new Dictionary<string, object?>
            {
                ["api"] = new Dictionary<string, object?>
                {
                    ["condition"] = "service_started"
                }
            },
            ["restart"] = "unless-stopped"
        };
    }

    private async Task<Dictionary<object, object?>> LoadComposeAsync(
        string composePath,
        CancellationToken cancellationToken)
    {
        var yaml = await File.ReadAllTextAsync(composePath, cancellationToken);

        return _deserializer.Deserialize<Dictionary<object, object?>>(yaml)
            ?? throw new InvalidOperationException($"Compose file '{composePath}' is empty.");
    }

    private async Task SaveComposeAsync(
        string composePath,
        Dictionary<object, object?> compose,
        CancellationToken cancellationToken)
    {
        var yaml = _serializer.Serialize(compose);

        await File.WriteAllTextAsync(composePath, yaml, Encoding.UTF8, cancellationToken);
    }

    private static Dictionary<object, object?> GetOrCreateMap(
        Dictionary<object, object?> map,
        string key)
    {
        if (map.TryGetValue(key, out var value) && value is Dictionary<object, object?> child)
        {
            return child;
        }

        child = new Dictionary<object, object?>();
        map[key] = child;

        return child;
    }
}
