using Contracts.Enums;

namespace Contracts.Models;

public abstract record CursorQuery
{
    public string? Search { get; init; }

    public string? Cursor { get; init; }

    public int Limit { get; init; } = 50;

    public SortOrder Order { get; init; } = SortOrder.Asc;
}
