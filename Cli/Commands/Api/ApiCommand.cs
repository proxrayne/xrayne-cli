using System.CommandLine;

namespace Cli.Commands.Api;

public sealed class ApiCommand : Command
{
    public ApiCommand(
        ApiInstallCommand installCommand,
        ApiVersionCommand versionCommand,
        ApiStatusCommand statusCommand,
        ApiStopCommand stopCommand,
        ApiStartCommand startCommand,
        ApiRestartCommand restartCommand)
        : base("api", "Manage XRayne API installation")
    {
        Add(installCommand);
        Add(versionCommand);
        Add(statusCommand);
        Add(stopCommand);
        Add(startCommand);
        Add(restartCommand);
    }
}
