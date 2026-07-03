using Xray.Config.Enums;

namespace Contracts.Models;

public sealed record InboundFilter : CursorQuery
{
    public IReadOnlyCollection<Protocol>? Protocol { get; init; }

    public IReadOnlyCollection<StreamNetwork>? Network { get; init; }

    public IReadOnlyCollection<StreamSecurity>? Security { get; init; }

    public bool? Enabled { get; init; }
}
