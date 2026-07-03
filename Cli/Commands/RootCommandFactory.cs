using System.CommandLine;
using Cli.Commands.Api;
using Cli.Commands.Admin;
using Cli.Commands.Cert;
using Cli.Commands.Xray;

namespace Cli.Commands;

public sealed class RootCommandFactory(
    XrayCommand xrayCommand,
    AdminCommand adminCommand,
    ApiCommand apiCommand,
    CertCommand certCommand,
    VersionCommand versionCommand,
    UpdateCommand updateCommand,
    InfoCommand infoCommand)
{
    public RootCommand Create()
    {
        return new RootCommand("XRayne CLI")
        {
            xrayCommand,
            adminCommand,
            apiCommand,
            certCommand,
            versionCommand,
            updateCommand,
            infoCommand
        };
    }
}
