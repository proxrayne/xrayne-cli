namespace Contracts.Models;

public sealed class SettingsUpdateState
{
    public IReadOnlyList<string> ChangedFields { get; init; } = [];
    public bool RequiresRestart { get; init; } = false;
    public bool PendingRestart { get; init; } = false;
}

