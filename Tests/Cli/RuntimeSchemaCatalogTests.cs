using Cli.Services;

namespace Test.Cli;

public sealed class RuntimeSchemaCatalogTests
{
    [Theory]
    [InlineData("v0.0.14", 0)]
    [InlineData("v0.0.15", 1)]
    [InlineData("v0.0.16-beta", 1)]
    [InlineData("not-a-version", RuntimeSchemaCatalog.LatestSchemaVersion)]
    public void ResolveForRelease_ReturnsExpectedSchemaVersion(
        string releaseTag,
        int expectedSchemaVersion)
    {
        RuntimeSchemaCatalog.ResolveForRelease(releaseTag).Should().Be(expectedSchemaVersion);
    }
}
