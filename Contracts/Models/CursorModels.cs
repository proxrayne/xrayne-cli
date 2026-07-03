namespace Contracts.Models;

public enum SortOrder
{
    Asc,
    Desc,
}

public abstract record CursorQuery
{
    public string? Search { get; init; }

    public string? Cursor { get; init; }

    public int Limit { get; init; } = 50;

    public SortOrder Order { get; init; } = SortOrder.Asc;
}

public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasNextPage, int TotalCount);

public sealed record CursorPosition(DateTimeOffset CreatedAt, string Id);
