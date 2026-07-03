using System.Runtime.InteropServices;

namespace Infrastructure.Utilities;

public static class CoreReleasesUtilities
{
    public static string GetCurrentPlatformAssetName()
    {
        var os = GetXrayOsName();
        var architecture = GetXrayArchitectureName(RuntimeInformation.ProcessArchitecture);

        return $"Xray-{os}-{architecture}.zip";
    }

    public static string GetXrayOsName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows";
        }

        if (OperatingSystem.IsLinux())
        {
            return "linux";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "macos";
        }

        if (OperatingSystem.IsFreeBSD())
        {
            return "freebsd";
        }

        throw new PlatformNotSupportedException(
            $"Xray-core release asset selection is not supported on {RuntimeInformation.OSDescription}.");
    }

    public static string GetXrayArchitectureName(Architecture architecture)
    {
        return architecture.ToString().ToLowerInvariant() switch
        {
            "x64" => "64",
            "x86" => "32",
            "arm64" => "arm64-v8a",
            "arm" => "arm32-v7a",
            "armv6" => "arm32-v6",
            "s390x" => "s390x",
            "ppc64le" => "ppc64le",
            _ => throw new PlatformNotSupportedException(
                $"Xray-core release asset selection is not supported for {architecture} architecture.")
        };
    }
}
