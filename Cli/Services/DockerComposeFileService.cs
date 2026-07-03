using System.Text;
using Cli.Services.Contracts;
using Cli.Values;
using Contracts.Values;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Cli.Services;

public sealed class DockerComposeFileService : IDockerComposeFileService
{
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public async Task WriteApiComposeAsync(
        ProjectPaths paths,
        string imageTag,
        CancellationToken cancellationToken)
    {
        var compose = CreateApiCompose(imageTag);
        var yaml = _serializer.Serialize(compose);

        await File.WriteAllTextAsync(paths.DockerCompose, yaml, Encoding.UTF8, cancellationToken);
    }

    private static Dictionary<string, object?> CreateApiCompose(string imageTag)
    {
        return new Dictionary<string, object?>
        {
            ["services"] = new Dictionary<string, object?>
            {
                ["api"] = new Dictionary<string, object?>
                {
                    ["image"] = $"${{API_IMAGE:-{CliDefaults.GetApiImageName(imageTag)}}}",
                    ["container_name"] = "xrayne-api",
                    ["network_mode"] = "host",
                    ["env_file"] = new[] { ".env" },
                    ["environment"] = new Dictionary<string, object?>
                    {
                        ["PORT"] = "${PORT:-5000}",
                        ["PROJECT_PATH"] = "/app/shared",
                        ["ConnectionStrings__Default"] = "Host=${POSTGRES_HOST_API:-localhost};Port=${POSTGRES_PORT:-5432};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Database=${POSTGRES_DB}"
                    },
                    ["volumes"] = new[]
                    {
                        "${PROJECT_PATH:-/opt/xrayne}:/app/shared",
                        "${PROJECT_PATH:-/opt/xrayne}/logs:/app/logs",
                        "${PROJECT_PATH:-/opt/xrayne}/xray:/app/xray"
                    },
                    ["depends_on"] = new Dictionary<string, object?>
                    {
                        ["postgres"] = new Dictionary<string, object?>
                        {
                            ["condition"] = "service_healthy"
                        }
                    },
                    ["restart"] = "unless-stopped"
                },
                ["ui"] = new Dictionary<string, object?>
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
                },
                ["postgres"] = new Dictionary<string, object?>
                {
                    ["image"] = "postgres:16-alpine",
                    ["container_name"] = "xrayne-postgres",
                    ["env_file"] = new[] { ".env" },
                    ["environment"] = new Dictionary<string, object?>
                    {
                        ["POSTGRES_DB"] = "${POSTGRES_DB}",
                        ["POSTGRES_USER"] = "${POSTGRES_USER}",
                        ["POSTGRES_PASSWORD"] = "${POSTGRES_PASSWORD}"
                    },
                    ["ports"] = new[] { "${POSTGRES_PORT:-5432}:5432" },
                    ["volumes"] = new[] { "${PROJECT_PATH:-/opt/xrayne}/postgres:/var/lib/postgresql/data" },
                    ["healthcheck"] = new Dictionary<string, object?>
                    {
                        ["test"] = new[] { "CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}" },
                        ["interval"] = "10s",
                        ["timeout"] = "5s",
                        ["retries"] = 5
                    },
                    ["restart"] = "unless-stopped"
                }
            }
        };
    }
}
