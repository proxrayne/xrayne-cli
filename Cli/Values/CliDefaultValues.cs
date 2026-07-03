namespace Cli.Values;

public static class CliDefaults
{
    public const string LatestVersion = "latest";
    public const string ApiImageNamePrefix = "xrayne-api-image-";
    public const string UiImageNamePrefix = "xrayne-ui-image-";
    public const string CliRepositoryUrl = "https://github.com/proxrayne/xrayne-cli";
    public const string ApiRepositoryUrl = "https://github.com/proxrayne/xrayne-api";
    public const string UiRepositoryUrl = "https://github.com/proxrayne/xrayne-ui";
    public const string NodeRepositoryUrl = "https://github.com/proxrayne/xrayne-node";
    public const int DefaultApiPort = 5000;
    public const int DefaultUiPort = 8080;
    public const string PostgresUser = "postgres";
    public const string PostgresDatabase = "xrayne";
    public const string ApiImageVariable = "API_IMAGE";
    public const string UiImageVariable = "UI_IMAGE";

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

    public static string GetUiImageName(string version)
    {
        return $"{UiImageNamePrefix}{version}";
    }

    public static string GetUiImageArchiveName(string version)
    {
        return $"{GetUiImageName(version)}.tar.gz";
    }

    public static string GetUiImageTarName(string version)
    {
        return $"{GetUiImageName(version)}.tar";
    }

    public static string? ExtractApiImageVersion(string image)
    {
        return ExtractImageVersion(image, ApiImageNamePrefix, allowLegacyApiPrefix: true);
    }

    public static string? ExtractUiImageVersion(string image)
    {
        return ExtractImageVersion(image, UiImageNamePrefix, allowLegacyApiPrefix: false);
    }

    private static string? ExtractImageVersion(
        string image,
        string imageNamePrefix,
        bool allowLegacyApiPrefix)
    {
        if (string.IsNullOrWhiteSpace(image))
        {
            return null;
        }

        var normalized = image.Trim();
        var lastSegmentStart = normalized.LastIndexOf('/') + 1;
        var imageName = normalized[lastSegmentStart..];

        if (imageName.StartsWith(imageNamePrefix, StringComparison.Ordinal))
        {
            var version = imageName[imageNamePrefix.Length..];
            var tagSeparatorIndex = version.IndexOf(':', StringComparison.Ordinal);

            return tagSeparatorIndex >= 0 ? version[..tagSeparatorIndex] : version;
        }

        const string legacyImagePrefix = "xrayne-api:";
        if (allowLegacyApiPrefix && imageName.StartsWith(legacyImagePrefix, StringComparison.Ordinal))
        {
            return imageName[legacyImagePrefix.Length..];
        }

        var legacyTagSeparatorIndex = imageName.LastIndexOf(':');

        return legacyTagSeparatorIndex >= 0 && legacyTagSeparatorIndex < imageName.Length - 1
            ? imageName[(legacyTagSeparatorIndex + 1)..]
            : null;
    }
}
