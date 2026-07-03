using Contracts.Values;

namespace Cli.Services.RuntimeMigrations;

internal sealed class RuntimeMigrationContext
{
    public RuntimeMigrationContext(ProjectPaths paths)
    {
        Paths = paths;
    }

    public ProjectPaths Paths { get; }
}
