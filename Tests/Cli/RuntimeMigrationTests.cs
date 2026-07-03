using Cli.Services.RuntimeMigrations;
using Contracts.Values;
using Data.Utilities;
using Test.Infrastructure;

namespace Test.Cli;

public sealed class RuntimeMigrationTests
{
    [Fact]
    public async Task V1Migration_UpdatesRuntimeFilesForHostNetwork()
    {
        using var workspace = new TestWorkspace();
        var paths = new ProjectPaths(workspace.Root);
        await File.WriteAllTextAsync(paths.EnvConfig, "API_PORT=5001");
        await File.WriteAllTextAsync(
            paths.JsonConfig,
            """{"Kestrel":{"Endpoints":{"Http":{"Url":"http://+:8080"}}},"Runtime":{"SchemaVersion":0}}""");
        await File.WriteAllTextAsync(
            paths.DockerCompose,
            """
            services:
              api:
                ports:
                - ${API_PORT:-5000}:8080
                environment:
                  API_PORT: ${API_PORT:-5000}
                  ASPNETCORE_URLS: http://+:8080
            """);

        var migration = new V1UseHostNetworkForApiMigration();

        await migration.UpAsync(new RuntimeMigrationContext(paths), CancellationToken.None);

        var compose = await File.ReadAllTextAsync(paths.DockerCompose);
        compose.Should().Contain("network_mode: host");
        compose.Should().NotContain("ASPNETCORE_URLS");
        compose.Should().Contain("PORT: ${PORT:-5000}");
        (await EnvConfig.GetAsync("POSTGRES_HOST_API", paths.EnvConfig)).Should().Be("localhost");
        (await EnvConfig.GetAsync("PORT", paths.EnvConfig)).Should().Be("5001");
    }
}
