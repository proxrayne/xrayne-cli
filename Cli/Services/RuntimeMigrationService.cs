using Cli.Services.Contracts;
using Cli.Services.RuntimeMigrations;
using Contracts.Values;

namespace Cli.Services;

public sealed class RuntimeMigrationService : IRuntimeMigrationService
{
    private readonly IReadOnlyList<IRuntimeMigration> _migrations =
    [
        new V1UseHostNetworkForApiMigration()
    ];

    public async Task<RuntimeMigrationResult> MigrateToAsync(
        int targetSchemaVersion,
        CancellationToken cancellationToken)
    {
        if (!IsRuntimeInstalled())
        {
            return new RuntimeMigrationResult(
                CurrentSchemaVersion: 0,
                TargetSchemaVersion: targetSchemaVersion,
                BackupDirectory: null,
                AppliedMigrations: []);
        }

        var currentSchemaVersion = await RuntimeMigrationFileHelpers.ReadSchemaVersionAsync(
            PathProvider.Paths,
            cancellationToken);
        if (currentSchemaVersion == targetSchemaVersion)
        {
            return new RuntimeMigrationResult(
                currentSchemaVersion,
                targetSchemaVersion,
                BackupDirectory: null,
                AppliedMigrations: []);
        }

        var backupDirectory = await CreateBackupAsync(currentSchemaVersion, targetSchemaVersion, cancellationToken);
        var context = new RuntimeMigrationContext(PathProvider.Paths);
        var appliedMigrations = new List<string>();
        var cursor = currentSchemaVersion;

        while (cursor < targetSchemaVersion)
        {
            var migration = _migrations.SingleOrDefault(item => item.FromVersion == cursor)
                ?? throw new InvalidOperationException($"No runtime migration from schema {cursor} was found.");

            await migration.UpAsync(context, cancellationToken);
            cursor = migration.ToVersion;
            appliedMigrations.Add($"{migration.FromVersion}->{migration.ToVersion}: {migration.Name}");
        }

        while (cursor > targetSchemaVersion)
        {
            var migration = _migrations.SingleOrDefault(item => item.ToVersion == cursor)
                ?? throw new InvalidOperationException($"No runtime migration down from schema {cursor} was found.");

            await migration.DownAsync(context, cancellationToken);
            cursor = migration.FromVersion;
            appliedMigrations.Add($"{migration.ToVersion}->{migration.FromVersion}: {migration.Name}");
        }

        await RuntimeMigrationFileHelpers.SetSchemaVersionAsync(
            PathProvider.Paths,
            targetSchemaVersion,
            cancellationToken);

        return new RuntimeMigrationResult(
            currentSchemaVersion,
            targetSchemaVersion,
            backupDirectory,
            appliedMigrations);
    }

    private static bool IsRuntimeInstalled()
    {
        return File.Exists(PathProvider.Paths.EnvConfig)
            || File.Exists(PathProvider.Paths.JsonConfig)
            || File.Exists(PathProvider.Paths.DockerCompose);
    }

    private static async Task<string> CreateBackupAsync(
        int currentSchemaVersion,
        int targetSchemaVersion,
        CancellationToken cancellationToken)
    {
        var backupDirectory = Path.Combine(
            PathProvider.Paths.Root,
            "backups",
            "runtime-migrations",
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"),
            $"schema-{currentSchemaVersion}-to-{targetSchemaVersion}");

        Directory.CreateDirectory(backupDirectory);

        foreach (var path in new[]
        {
            PathProvider.Paths.EnvConfig,
            PathProvider.Paths.JsonConfig,
            PathProvider.Paths.DockerCompose
        })
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var destinationPath = Path.Combine(backupDirectory, Path.GetFileName(path));
            await using var source = File.OpenRead(path);
            await using var destination = File.Create(destinationPath);
            await source.CopyToAsync(destination, cancellationToken);
        }

        return backupDirectory;
    }
}
