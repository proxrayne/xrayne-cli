namespace Cli.Services;

public static class RuntimeSchemaCatalog
{
    public const int LatestSchemaVersion = 1;

    public static int ResolveForRelease(string releaseTag)
    {
        var version = NormalizeVersion(releaseTag);
        if (version is null)
        {
            return LatestSchemaVersion;
        }

        return version >= new Version(0, 0, 15)
            ? 1
            : 0;
    }

    private static Version? NormalizeVersion(string releaseTag)
    {
        var value = releaseTag.Trim();
        if (value.StartsWith('v') || value.StartsWith('V'))
        {
            value = value[1..];
        }

        var metadataIndex = value.IndexOf('+', StringComparison.Ordinal);
        if (metadataIndex >= 0)
        {
            value = value[..metadataIndex];
        }

        var prereleaseIndex = value.IndexOf('-', StringComparison.Ordinal);
        if (prereleaseIndex >= 0)
        {
            value = value[..prereleaseIndex];
        }

        return Version.TryParse(value, out var version) ? version : null;
    }
}
