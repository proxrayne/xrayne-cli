namespace Cli.Services.Contracts;

public interface IRuntimeMigrationService
{
    Task<RuntimeMigrationResult> MigrateToAsync(
        int targetSchemaVersion,
        CancellationToken cancellationToken);
}
