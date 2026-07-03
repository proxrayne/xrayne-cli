using Contracts.Enums;

namespace Contracts.Models;

public sealed class UpdateCommit
{
    public required string Field { get; init; }
    
    public required string ConfigKey { get; init; }

    public UpdateTarget Target { get; init; } = UpdateTarget.Json;

    public required object? Value { get; init; }

    public RestartImpact Impact { get; init; } = RestartImpact.HotReload;
}