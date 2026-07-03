using Cli.Services;

namespace Tests.Infrastructure;

public sealed class GitHubReleaseClientTests
{
    [Theory]
    [InlineData("proxrayne/xrayne-cli", "proxrayne/xrayne-cli")]
    [InlineData("https://github.com/proxrayne/xrayne-cli", "proxrayne/xrayne-cli")]
    [InlineData("https://github.com/proxrayne/xrayne-cli/", "proxrayne/xrayne-cli")]
    public void NormalizeRepositoryFullNameSupportsFullNameAndGitHubUrl(string value, string expected)
    {
        var result = GitHubReleaseClient.NormalizeRepositoryFullName(value);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://example.com/proxrayne/xrayne-cli")]
    [InlineData("proxrayne")]
    [InlineData("proxrayne/xrayne-cli/extra")]
    public void NormalizeRepositoryFullNameRejectsUnsupportedRepositoryValues(string value)
    {
        var action = () => GitHubReleaseClient.NormalizeRepositoryFullName(value);

        action.Should().Throw<ArgumentException>();
    }
}
