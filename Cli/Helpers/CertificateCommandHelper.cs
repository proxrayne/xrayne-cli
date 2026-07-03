using Contracts.Values;

namespace Cli.Helpers;

internal static class CertificateCommandHelper
{
    public const string ContainerLetsEncryptDirectory = "/app/shared/certificates/letsencrypt";

    public static string AcmeHome => Path.Combine(PathProvider.Paths.CertificatesDirectory, "acme-sh");

    public static string AcmeConfigHome => Path.Combine(AcmeHome, "config");

    public static string AcmeCertHome => Path.Combine(AcmeHome, "certs");

    public static string AcmeScriptPath => Path.Combine(AcmeHome, "acme.sh");

    public static string BuildCertName(string mode, string identifier)
    {
        var normalized = new string(identifier
            .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-')
            .ToLowerInvariant();

        return $"xrayne-{mode}-{normalized}";
    }

    public static string GetHostFullChainPath(string certName)
    {
        return Path.Combine(GetHostLiveDirectory(certName), "fullchain.pem");
    }

    public static string GetHostPrivateKeyPath(string certName)
    {
        return Path.Combine(GetHostLiveDirectory(certName), "privkey.pem");
    }

    public static string GetContainerFullChainPath(string certName)
    {
        return $"{ContainerLetsEncryptDirectory}/live/{certName}/fullchain.pem";
    }

    public static string GetContainerPrivateKeyPath(string certName)
    {
        return $"{ContainerLetsEncryptDirectory}/live/{certName}/privkey.pem";
    }

    public static string BuildReloadCommand()
    {
        var projectPath = PathProvider.Paths.Root.Replace("\"", "\\\"");

        return $"cd \"{projectPath}\" && docker compose up -d --force-recreate api";
    }

    private static string GetHostLiveDirectory(string certName)
    {
        return Path.Combine(PathProvider.Paths.LetsEncryptDirectory, "live", certName);
    }
}
