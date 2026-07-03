using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Github;

/// <summary>
/// Provides release and release asset access for public GitHub Data.
/// </summary>
public sealed class GitHubRepository : IDisposable
{
    private const string LatestVersion = "latest";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    /// <summary>
    /// Initializes a new GitHub repository client.
    /// </summary>
    public GitHubRepository(
        string repositoryUrl,
        HttpClient? httpClient = null)
    {
        FullName = NormalizeRepositoryName(repositoryUrl);
        _httpClient = httpClient ?? new HttpClient();
        _disposeHttpClient = httpClient is null;

        ConfigureHeaders(_httpClient);
    }

    /// <summary>
    /// Gets the normalized repository full name in owner/repository format.
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// Gets the public GitHub repository URL.
    /// </summary>
    public string Url => $"https://github.com/{FullName}";

    /// <summary>
    /// Gets repository releases from the GitHub API.
    /// </summary>
    public async Task<ICollection<GitHubRelease>> GetReleasesAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<GitHubRelease[]>(
            $"https://api.github.com/repos/{FullName}/releases",
            cancellationToken) ?? [];
    }

    /// <summary>
    /// Gets repository releases from the GitHub API with paging options.
    /// </summary>
    public async Task<ICollection<GitHubRelease>> GetReleasesAsync(
        GitHubReleasesFilter filter,
        CancellationToken cancellationToken = default)
    {
        var url = AddQueryString(
            $"https://api.github.com/repos/{FullName}/releases",
            new Dictionary<string, string?>
            {
                ["per_page"] = filter.PerPage?.ToString(),
                ["page"] = filter.Page?.ToString()
            });

        return await GetJsonAsync<GitHubRelease[]>(
            url,
            cancellationToken) ?? [];
    }

    /// <summary>
    /// Gets a release by tag or the latest stable release when version is latest.
    /// </summary>
    public async Task<GitHubRelease> GetReleaseAsync(
        string version = LatestVersion,
        CancellationToken cancellationToken = default)
    {
        var url = string.Equals(version, LatestVersion, StringComparison.OrdinalIgnoreCase)
            ? $"https://api.github.com/repos/{FullName}/releases/latest"
            : $"https://api.github.com/repos/{FullName}/releases/tags/{Uri.EscapeDataString(version)}";

        return await GetJsonAsync<GitHubRelease>(url, cancellationToken)
            ?? throw new InvalidOperationException($"GitHub release '{version}' response was empty.");
    }

    /// <summary>
    /// Downloads a release asset into the destination directory using the asset name.
    /// </summary>
    public Task<string> DownloadAssetAsync(
        GitHubAsset asset,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    => DownloadAssetAsync(asset, destinationDirectory, asset.Name, cancellationToken);

    /// <summary>
    /// Downloads a release asset into the destination directory using a custom file name.
    /// </summary>
    public async Task<string> DownloadAssetAsync(
        GitHubAsset asset,
        string destinationDirectory,
        string assetName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(asset.Name))
        {
            throw new InvalidOperationException("GitHub asset name is empty.");
        }

        Directory.CreateDirectory(destinationDirectory);

        var destinationPath = Path.Combine(destinationDirectory, assetName);
        using var request = new HttpRequestMessage(HttpMethod.Get, asset.Url);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(destinationPath);
        await input.CopyToAsync(output, cancellationToken);

        return destinationPath;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<T?> GetJsonAsync<T>(
        string url,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    private static void ConfigureHeaders(HttpClient client)
    {
        if (!client.DefaultRequestHeaders.UserAgent.Any())
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("xrayne", "1.0"));
        }

        if (!client.DefaultRequestHeaders.Accept.Any())
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        if (!client.DefaultRequestHeaders.Contains("X-GitHub-Api-Version"))
        {
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        }
    }

    private static string AddQueryString(
        string url,
        IReadOnlyDictionary<string, string?> query)
    {
        var builder = new StringBuilder(url);
        var hasQuery = url.Contains('?', StringComparison.Ordinal);

        foreach (var (key, value) in query)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.Append(hasQuery ? '&' : '?');
            builder.Append(Uri.EscapeDataString(key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
            hasQuery = true;
        }

        return builder.ToString();
    }

    private static string NormalizeRepositoryName(string repositoryUrl)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
        {
            throw new ArgumentException("GitHub repository URL cannot be empty.", nameof(repositoryUrl));
        }

        var value = repositoryUrl.Trim().TrimEnd('/');
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            return ValidateFullName(value);
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("GitHub repository URL must point to github.com.", nameof(repositoryUrl));
        }

        var segments = uri.AbsolutePath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2)
        {
            throw new ArgumentException("GitHub repository URL must include owner and repository name.", nameof(repositoryUrl));
        }

        return ValidateFullName($"{segments[0]}/{segments[1]}");
    }

    private static string ValidateFullName(string value)
    {
        var segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2 || segments.Any(segment => segment.Length == 0))
        {
            throw new ArgumentException("GitHub repository must be in 'owner/repository' format.", nameof(value));
        }

        return $"{segments[0]}/{segments[1]}";
    }
}
