using System.Text;
using Data.Utilities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Cli.Services.RuntimeMigrations;

internal sealed class V1UseHostNetworkForApiMigration : IRuntimeMigration
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public int FromVersion => 0;

    public int ToVersion => 1;

    public string Name => "Use host networking for API";

    public async Task UpAsync(
        RuntimeMigrationContext context,
        CancellationToken cancellationToken)
    {
        var apiPort = await RuntimeMigrationFileHelpers.GetApiPortAsync(context.Paths, cancellationToken);
        await MigrateComposeUpAsync(context, cancellationToken);
        await RuntimeMigrationFileHelpers.SetEnvValueAsync(context.Paths.EnvConfig, "POSTGRES_HOST_API", "localhost", cancellationToken);
        await RuntimeMigrationFileHelpers.SetEnvValueAsync(context.Paths.EnvConfig, "POSTGRES_CONTAINER_PORT", "5432", cancellationToken);
        await RuntimeMigrationFileHelpers.SetEnvValueAsync(context.Paths.EnvConfig, "PORT", apiPort, cancellationToken);

        await JsonConfig.UpdateAsync(
            context.Paths.JsonConfig,
            config =>
            {
                JsonConfig.Remove(config, "Kestrel");
                JsonConfig.Set(config, "Runtime:SchemaVersion", ToVersion);
            },
            cancellationToken);
    }

    public async Task DownAsync(
        RuntimeMigrationContext context,
        CancellationToken cancellationToken)
    {
        var hasCertificate = RuntimeMigrationFileHelpers.HasCertificate(context.Paths);

        await MigrateComposeDownAsync(context, hasCertificate, cancellationToken);
        await RuntimeMigrationFileHelpers.SetEnvValueAsync(context.Paths.EnvConfig, "POSTGRES_HOST_API", "postgres", cancellationToken);
        await RuntimeMigrationFileHelpers.SetEnvValueAsync(context.Paths.EnvConfig, "POSTGRES_CONTAINER_PORT", "5432", cancellationToken);

        await JsonConfig.UpdateAsync(
            context.Paths.JsonConfig,
            config =>
            {
                JsonConfig.Remove(config, "Kestrel");
                JsonConfig.Set(config, "Runtime:SchemaVersion", FromVersion);
            },
            cancellationToken);
    }

    private async Task MigrateComposeUpAsync(
        RuntimeMigrationContext context,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(context.Paths.DockerCompose))
        {
            return;
        }

        var compose = await LoadComposeAsync(context.Paths.DockerCompose, cancellationToken);
        var api = GetApiService(compose);
        var environment = GetOrCreateMap(api, "environment");

        api["network_mode"] = "host";
        api.Remove("ports");
        environment.Remove("API_PORT");
        environment.Remove("ASPNETCORE_URLS");
        environment["PORT"] = "${PORT:-5000}";
        environment["ConnectionStrings__Default"] = "Host=${POSTGRES_HOST_API:-localhost};Port=${POSTGRES_PORT:-5432};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Database=${POSTGRES_DB}";

        await SaveComposeAsync(context.Paths.DockerCompose, compose, cancellationToken);
    }

    private async Task MigrateComposeDownAsync(
        RuntimeMigrationContext context,
        bool hasCertificate,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(context.Paths.DockerCompose))
        {
            return;
        }

        var compose = await LoadComposeAsync(context.Paths.DockerCompose, cancellationToken);
        var api = GetApiService(compose);
        var environment = GetOrCreateMap(api, "environment");

        api.Remove("network_mode");
        api["ports"] = new[]
        {
            hasCertificate
                ? "${PORT:-5000}:8443"
                : "${PORT:-5000}:8080"
        };
        environment.Remove("API_PORT");
        environment.Remove("PORT");
        environment["ASPNETCORE_URLS"] = hasCertificate
            ? "https://+:8443"
            : "http://+:8080";
        environment["ConnectionStrings__Default"] = "Host=${POSTGRES_HOST_API:-postgres};Port=${POSTGRES_CONTAINER_PORT:-5432};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Database=${POSTGRES_DB}";

        await SaveComposeAsync(context.Paths.DockerCompose, compose, cancellationToken);
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

    private static Dictionary<object, object?> GetApiService(Dictionary<object, object?> compose)
    {
        var services = GetOrCreateMap(compose, "services");

        return GetOrCreateMap(services, "api");
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
