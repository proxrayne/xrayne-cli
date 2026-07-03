namespace Cli.Values;

public static class CliDefaults
{
    public const string LatestVersion = "latest";
    public const string ApiImageNamePrefix = "xrayne-api-image-";
    public const string XRayneRepositoryUrl = "https://github.com/VanyaKrotov/xrayne";
    public const int DefaultApiPort = 5000;
    public const string PostgresUser = "postgres";
    public const string PostgresDatabase = "xrayne";
    public const string ApiImageVariable = "API_IMAGE";

    public static string GetApiImageName(string version)
    {
        return $"{ApiImageNamePrefix}{version}";
    }

    public static string GetApiImageArchiveName(string version)
    {
        return $"{GetApiImageName(version)}.tar.gz";
    }

    public static string GetApiImageTarName(string version)
    {
        return $"{GetApiImageName(version)}.tar";
    }

    public static string? ExtractApiImageVersion(string image)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            return null;
        }

        var normalized = image.Trim();
        var lastSegmentStart = normalized.LastIndexOf('/') + 1;
        var imageName = normalized[lastSegmentStart..];

        if (imageName.StartsWith(ApiImageNamePrefix, StringComparison.Ordinal))
        {
            var version = imageName[ApiImageNamePrefix.Length..];
            var tagSeparatorIndex = version.IndexOf(':', StringComparison.Ordinal);

            return tagSeparatorIndex >= 0 ? version[..tagSeparatorIndex] : version;
        }

        const string legacyImagePrefix = "xrayne-api:";
        if (imageName.StartsWith(legacyImagePrefix, StringComparison.Ordinal))
        {
            return imageName[legacyImagePrefix.Length..];
        }

        var legacyTagSeparatorIndex = imageName.LastIndexOf(':');

        return legacyTagSeparatorIndex >= 0 && legacyTagSeparatorIndex < imageName.Length - 1
            ? imageName[(legacyTagSeparatorIndex + 1)..]
            : null;
    }
}
