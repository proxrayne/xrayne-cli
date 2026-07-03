namespace Github;

/// <summary>
/// Paging options for GitHub release list requests.
/// </summary>
public sealed record GitHubReleasesFilter(int? PerPage, int? Page);
