using System.CommandLine;

namespace Cli.Commands.Admin;

public sealed class AdminCommand : Command
{
    public AdminCommand(AdminCreateCommand createCommand)
        : base("admin", "Manage administrator accounts")
    {
        Add(createCommand);
    }
}
