using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cli.Helpers;
using Cli.Output;

namespace Cli.Commands.Cert;

public sealed class CertStatusCommand : Command
{
    public CertStatusCommand(IServiceProvider serviceProvider)
        : base("status", "Print installed HTTPS certificate information")
    {
        SetAction(async (_, cancellationToken) =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            return Execute(scope.ServiceProvider);
        });
    }

    private static int Execute(IServiceProvider serviceProvider)
    {
        var console = serviceProvider.GetRequiredService<ICliConsole>();
        var logger = serviceProvider.GetRequiredService<ILogger<CertStatusCommand>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        try
        {
            var certName = configuration["CERTIFICATE_CERT_NAME"];
            if (string.IsNullOrWhiteSpace(certName))
            {
                console.Header("XRayne certificate status");
                console.Value("Status", "not installed");

                return 0;
            }

            var fullChainPath = configuration["CERTIFICATE_HOST_FULL_CHAIN_PATH"]
                ?? CertificateCommandHelper.GetHostFullChainPath(certName);
            var privateKeyPath = configuration["CERTIFICATE_HOST_PRIVATE_KEY_PATH"]
                ?? CertificateCommandHelper.GetHostPrivateKeyPath(certName);

            console.Header("XRayne certificate status");
            console.Value("Status", File.Exists(fullChainPath) && File.Exists(privateKeyPath) ? "installed" : "missing files");
            console.Value("Mode", configuration["CERTIFICATE_MODE"] ?? "(unknown)");
            console.Value("Identifier", configuration["CERTIFICATE_IDENTIFIER"] ?? "(unknown)");
            console.Value("ACME client", configuration["CERTIFICATE_ACME_CLIENT"] ?? "(unknown)");
            console.Value("Issuer", configuration["CERTIFICATE_ISSUER"] ?? "(unknown)");
            console.Value("Cert profile", configuration["CERTIFICATE_CERT_PROFILE"] ?? "(default)");
            console.Value("Auto renew", configuration.GetValue("CERTIFICATE_AUTO_RENEW", false) ? "enabled" : "disabled");
            console.Value("Certificate name", certName);
            console.Value("Certificate", FormatPathState(fullChainPath));
            console.Value("Private key", FormatPathState(privateKeyPath));
            console.Value("HTTPS endpoint", !string.IsNullOrWhiteSpace(configuration["CERT_PUBLIC_KEY_PATH"])
                && !string.IsNullOrWhiteSpace(configuration["CERT_PRIVATE_KEY_PATH"])
                    ? $"https://+:{configuration["PORT"] ?? "(unknown)"}"
                    : "(not configured)");

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Certificate status lookup failed.");
            console.Error(exception.Message);

            return 1;
        }
    }

    private static string FormatPathState(string path)
    {
        return File.Exists(path) || Directory.Exists(path)
            ? path
            : $"{path} (missing)";
    }
}
