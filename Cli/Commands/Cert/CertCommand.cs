using System.CommandLine;

namespace Cli.Commands.Cert;

public sealed class CertCommand : Command
{
    public CertCommand(
        CertInstallCommand installCommand,
        CertRenewCommand renewCommand,
        CertStatusCommand statusCommand)
        : base("cert", "Manage HTTPS certificates for the XRayne API")
    {
        Add(installCommand);
        Add(renewCommand);
        Add(statusCommand);
    }
}
