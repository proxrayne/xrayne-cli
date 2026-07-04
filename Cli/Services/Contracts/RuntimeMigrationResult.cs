namespace Cli.Services.Contracts;

public sealed record RuntimeMigrationResult(
    int CurrentSchemaVersion,
    int TargetSchemaVersion,
    string? BackupDirectory,
    IReadOnlyList<string> AppliedMigrations)
{
    public bool Changed => AppliedMigrations.Count > 0;
}
