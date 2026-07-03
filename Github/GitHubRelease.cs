using System.Text.Json.Serialization;

namespace Github;

/// <summary>
/// GitHub release metadata returned by the releases API.
/// </summary>
public sealed record GitHubRelease(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("assets_url")] string AssetsUrl,
    [property: JsonPropertyName("upload_url")] string UploadUrl,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    [property: JsonPropertyName("tarball_url")] string TarballUrl,
    [property: JsonPropertyName("zipball_url")] string ZipballUrl,
    [property: JsonPropertyName("discussion_url")] string? DiscussionUrl,
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("node_id")] string NodeId,
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("target_commitish")] string TargetCommitish,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("draft")] bool Draft,
    [property: JsonPropertyName("prerelease")] bool PreRelease,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("published_at")] DateTimeOffset? PublishedAt,
    [property: JsonPropertyName("author")] GitHubUser? Author,
    [property: JsonPropertyName("assets")] IReadOnlyCollection<GitHubAsset> Assets);
