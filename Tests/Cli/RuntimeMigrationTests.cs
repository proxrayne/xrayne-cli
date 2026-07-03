using Cli.Services.RuntimeMigrations;
using Cli.Services;
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

    [Fact]
    public async Task DockerComposeFileService_WritesStandaloneUiService()
    {
        using var workspace = new TestWorkspace();
        var paths = new ProjectPaths(workspace.Root);
        var service = new DockerComposeFileService();

        await service.WriteApiComposeAsync(paths, "0.0.16", CancellationToken.None);

        var compose = await File.ReadAllTextAsync(paths.DockerCompose);
        compose.Should().Contain("ui:");
        compose.Should().Contain("image: ${UI_IMAGE:-xrayne-ui-image-0.0.16}");
        compose.Should().Contain("- ${UI_PORT:-8080}:80");
        compose.Should().Contain("API_UPSTREAM: http://host.docker.internal:${PORT:-5000}");
        compose.Should().Contain("- host.docker.internal:host-gateway");
    }

    [Fact]
    public async Task V2Migration_AddsStandaloneUiRuntimeFiles()
    {
        using var workspace = new TestWorkspace();
        var paths = new ProjectPaths(workspace.Root);
        await File.WriteAllTextAsync(paths.EnvConfig, "API_IMAGE=xrayne-api-image-0.0.16");
        await File.WriteAllTextAsync(
            paths.DockerCompose,
            """
            services:
              api:
                image: ${API_IMAGE:-xrayne-api-image-0.0.16}
              postgres:
                image: postgres:16-alpine
            """);

        var migration = new V2AddStandaloneUiServiceMigration();

        await migration.UpAsync(new RuntimeMigrationContext(paths), CancellationToken.None);

        var compose = await File.ReadAllTextAsync(paths.DockerCompose);
        compose.Should().Contain("ui:");
        compose.Should().Contain("container_name: xrayne-ui");
        compose.Should().Contain("image: ${UI_IMAGE:-xrayne-ui-image-0.0.16}");
        (await EnvConfig.GetAsync("UI_IMAGE", paths.EnvConfig)).Should().Be("xrayne-ui-image-0.0.16");
        (await EnvConfig.GetAsync("UI_PORT", paths.EnvConfig)).Should().Be("8080");
    }

    [Fact]
    public async Task V2Migration_RemovesStandaloneUiRuntimeFilesOnDowngrade()
    {
        using var workspace = new TestWorkspace();
        var paths = new ProjectPaths(workspace.Root);
        await File.WriteAllTextAsync(
            paths.EnvConfig,
            """
            API_IMAGE=xrayne-api-image-0.0.16
            UI_IMAGE=xrayne-ui-image-0.0.16
            UI_PORT=8080
            """);
        await File.WriteAllTextAsync(
            paths.DockerCompose,
            """
            services:
              api:
                image: ${API_IMAGE:-xrayne-api-image-0.0.16}
              ui:
                image: ${UI_IMAGE:-xrayne-ui-image-0.0.16}
            """);

        var migration = new V2AddStandaloneUiServiceMigration();

        await migration.DownAsync(new RuntimeMigrationContext(paths), CancellationToken.None);

        var compose = await File.ReadAllTextAsync(paths.DockerCompose);
        compose.Should().NotContain("ui:");
        (await EnvConfig.GetAsync("UI_IMAGE", paths.EnvConfig)).Should().BeNull();
        (await EnvConfig.GetAsync("UI_PORT", paths.EnvConfig)).Should().BeNull();
    }
}
