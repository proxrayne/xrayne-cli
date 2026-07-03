using Cli.Services;

namespace Tests.Infrastructure;

public sealed class GitHubReleaseClientTests
{
    [Theory]
    [InlineData("VanyaKrotov/xrayne", "VanyaKrotov/xrayne")]
    [InlineData("https://github.com/VanyaKrotov/xrayne", "VanyaKrotov/xrayne")]
    [InlineData("https://github.com/VanyaKrotov/xrayne/", "VanyaKrotov/xrayne")]
    public void NormalizeRepositoryFullNameSupportsFullNameAndGitHubUrl(string value, string expected)
    {
        var result = GitHubReleaseClient.NormalizeRepositoryFullName(value);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://example.com/VanyaKrotov/xrayne")]
    [InlineData("VanyaKrotov")]
    [InlineData("VanyaKrotov/xrayne/extra")]
    public void NormalizeRepositoryFullNameRejectsUnsupportedRepositoryValues(string value)
    {
        var action = () => GitHubReleaseClient.NormalizeRepositoryFullName(value);

        action.Should().Throw<ArgumentException>();
    }
}
