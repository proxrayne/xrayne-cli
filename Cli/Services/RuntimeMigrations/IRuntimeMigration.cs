namespace Cli.Services.RuntimeMigrations;

internal interface IRuntimeMigration
{
    int FromVersion { get; }

    int ToVersion { get; }

    string Name { get; }

    Task UpAsync(RuntimeMigrationContext context, CancellationToken cancellationToken);

    Task DownAsync(RuntimeMigrationContext context, CancellationToken cancellationToken);
}
