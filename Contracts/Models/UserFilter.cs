using Xray.Config.Enums;
using Contracts.Enums;

namespace Contracts.Models;

public sealed record UserFilter : CursorQuery
{
    public IReadOnlyCollection<Protocol>? Protocol { get; init; }

    public IReadOnlyCollection<UserStatus>? Status { get; init; }

    public IReadOnlyCollection<LimitResetStrategy>? LimitResetStrategy { get; init; }
}
