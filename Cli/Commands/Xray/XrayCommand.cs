using System.CommandLine;

namespace Cli.Commands.Xray;

public sealed class XrayCommand : Command
{
    public XrayCommand(XrayStartCommand startCommand)
        : base("xray", "Process xray-core commands")
    {
        Add(startCommand);
    }
}
