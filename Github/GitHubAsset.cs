using System.Text.Json.Serialization;

namespace Github;

/// <summary>
/// GitHub release asset metadata.
/// </summary>
public sealed record GitHubAsset(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("node_id")] string NodeId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("label")] string? Label,
    [property: JsonPropertyName("uploader")] GitHubUser? Uploader,
    [property: JsonPropertyName("content_type")] string ContentType,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("download_count")] long DownloadCount,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
