using Github;
using System.Net;
using System.Text;

namespace Test.Infrastructure;

public sealed class GitHubRepositoryTests
{
    [Theory]
    [InlineData("VanyaKrotov/xrayne", "VanyaKrotov/xrayne")]
    [InlineData("https://github.com/VanyaKrotov/xrayne", "VanyaKrotov/xrayne")]
    [InlineData("https://github.com/VanyaKrotov/xrayne/", "VanyaKrotov/xrayne")]
    public void Constructor_NormalizesRepositoryUrl(
        string repositoryUrl,
        string expectedFullName)
    {
        using var repository = new GitHubRepository(repositoryUrl, new HttpClient(new FakeHandler()));

        Assert.Equal(expectedFullName, repository.FullName);
        Assert.Equal($"https://github.com/{expectedFullName}", repository.Url);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://example.com/VanyaKrotov/xrayne")]
    [InlineData("https://github.com/VanyaKrotov")]
    [InlineData("VanyaKrotov")]
    public void Constructor_RejectsInvalidRepositoryUrl(string repositoryUrl)
    {
        Assert.Throws<ArgumentException>(() => new GitHubRepository(repositoryUrl));
    }

    [Fact]
    public async Task GetReleasesAsync_LoadsReleaseList()
    {
        var handler = new FakeHandler(
            request =>
            {
                Assert.Equal("https://api.github.com/repos/VanyaKrotov/xrayne/releases", request.RequestUri?.ToString());

                return JsonResponse(
                    """
                    [
                      {
                        "url": "https://api.github.com/repos/VanyaKrotov/xrayne/releases/1",
                        "assets_url": "https://api.github.com/repos/VanyaKrotov/xrayne/releases/1/assets",
                        "upload_url": "https://uploads.github.com/repos/VanyaKrotov/xrayne/releases/1/assets{?name,label}",
                        "html_url": "https://github.com/VanyaKrotov/xrayne/releases/tag/0.0.1",
                        "tarball_url": "https://api.github.com/repos/VanyaKrotov/xrayne/tarball/0.0.1",
                        "zipball_url": "https://api.github.com/repos/VanyaKrotov/xrayne/zipball/0.0.1",
                        "id": 1,
                        "node_id": "R_1",
                        "tag_name": "0.0.1",
                        "target_commitish": "main",
                        "name": "0.0.1",
                        "body": "release body",
                        "draft": false,
                        "prerelease": false,
                        "created_at": "2026-05-07T00:00:00Z",
                        "published_at": "2026-05-07T00:00:00Z",
                        "assets": []
                      }
                    ]
                    """);
            });
        using var repository = new GitHubRepository("VanyaKrotov/xrayne", new HttpClient(handler));

        var releases = await repository.GetReleasesAsync();

        var release = Assert.Single(releases);
        Assert.Equal("0.0.1", release.TagName);
        Assert.False(release.PreRelease);
    }

    [Fact]
    public async Task GetReleasesAsync_WithFilter_AppendsPagingQuery()
    {
        var handler = new FakeHandler(
            request =>
            {
                Assert.Equal("https://api.github.com/repos/VanyaKrotov/xrayne/releases?per_page=100&page=2", request.RequestUri?.ToString());

                return JsonResponse("[]");
            });
        using var repository = new GitHubRepository("VanyaKrotov/xrayne", new HttpClient(handler));

        var releases = await repository.GetReleasesAsync(new GitHubReleasesFilter(100, 2));

        Assert.Empty(releases);
    }

    [Theory]
    [InlineData("latest", "https://api.github.com/repos/VanyaKrotov/xrayne/releases/latest")]
    [InlineData("0.0.1", "https://api.github.com/repos/VanyaKrotov/xrayne/releases/tags/0.0.1")]
    public async Task GetReleaseAsync_UsesExpectedEndpoint(
        string version,
        string expectedUrl)
    {
        var handler = new FakeHandler(
            request =>
            {
                Assert.Equal(expectedUrl, request.RequestUri?.ToString());

                return JsonResponse(
                    """
                    {
                      "url": "https://api.github.com/repos/VanyaKrotov/xrayne/releases/1",
                      "assets_url": "https://api.github.com/repos/VanyaKrotov/xrayne/releases/1/assets",
                      "upload_url": "https://uploads.github.com/repos/VanyaKrotov/xrayne/releases/1/assets{?name,label}",
                      "html_url": "https://github.com/VanyaKrotov/xrayne/releases/tag/0.0.1",
                      "tarball_url": "https://api.github.com/repos/VanyaKrotov/xrayne/tarball/0.0.1",
                      "zipball_url": "https://api.github.com/repos/VanyaKrotov/xrayne/zipball/0.0.1",
                      "id": 1,
                      "node_id": "R_1",
                      "tag_name": "0.0.1",
                      "target_commitish": "main",
                      "name": "0.0.1",
                      "body": "release body",
                      "draft": false,
                      "prerelease": false,
                      "created_at": "2026-05-07T00:00:00Z",
                      "published_at": "2026-05-07T00:00:00Z",
                      "assets": [
                        {
                          "url": "https://api.github.com/repos/VanyaKrotov/xrayne/releases/assets/10",
                          "id": 10,
                          "node_id": "A_10",
                          "name": "xrayne-api-image-0.0.1.tar.gz",
                          "label": null,
                          "content_type": "application/gzip",
                          "state": "uploaded",
                          "size": 123,
                          "download_count": 2,
                          "created_at": "2026-05-07T00:00:00Z",
                          "updated_at": "2026-05-07T00:00:00Z",
                          "browser_download_url": "https://github.com/VanyaKrotov/xrayne/releases/download/0.0.1/xrayne-api-image-0.0.1.tar.gz"
                        }
                      ]
                    }
                    """);
            });
        using var repository = new GitHubRepository("VanyaKrotov/xrayne", new HttpClient(handler));

        var release = await repository.GetReleaseAsync(version);

        Assert.Equal("0.0.1", release.TagName);
        Assert.Equal("xrayne-api-image-0.0.1.tar.gz", Assert.Single(release.Assets).Name);
    }

    [Fact]
    public async Task DownloadAssetAsync_DownloadsAssetToDirectory()
    {
        using var workspace = new TestWorkspace();
        var handler = new FakeHandler(
            request =>
            {
                Assert.Equal("https://api.github.com/repos/VanyaKrotov/xrayne/releases/assets/10", request.RequestUri?.ToString());
                Assert.Contains(request.Headers.Accept, header => header.MediaType == "application/octet-stream");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("asset-bytes"))
                };
            });
        using var repository = new GitHubRepository("VanyaKrotov/xrayne", new HttpClient(handler));
        var asset = new GitHubAsset(
            "https://api.github.com/repos/VanyaKrotov/xrayne/releases/assets/10",
            10,
            "A_10",
            "asset.txt",
            null,
            null,
            "text/plain",
            "uploaded",
            11,
            1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "https://github.com/VanyaKrotov/xrayne/releases/download/0.0.1/asset.txt");

        var destinationPath = await repository.DownloadAssetAsync(asset, workspace.Root);

        Assert.Equal(Path.Combine(workspace.Root, "asset.txt"), destinationPath);
        Assert.Equal("asset-bytes", await File.ReadAllTextAsync(destinationPath));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHandler()
            : this(_ => new HttpResponseMessage(HttpStatusCode.OK))
        {
        }

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
