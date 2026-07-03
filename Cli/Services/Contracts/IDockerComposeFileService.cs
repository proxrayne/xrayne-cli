using Contracts.Values;

namespace Cli.Services.Contracts;

public interface IDockerComposeFileService
{
    Task WriteApiComposeAsync(
        ProjectPaths paths,
        string imageTag,
        CancellationToken cancellationToken);
}
