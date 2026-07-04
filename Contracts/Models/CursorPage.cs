namespace Contracts.Models;

public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasNextPage, int TotalCount);
