using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cli.Output;
using Infrastructure.Services;

namespace Cli.Commands.Xray;

public sealed class XrayStartCommand : Command
{
    public XrayStartCommand(IServiceProvider serviceProvider)
        : base("start", "Start xray-core process")
    {
        SetAction(async (_, cancellationToken) =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            return await ExecuteAsync(scope.ServiceProvider, cancellationToken);
        });
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var coreService = serviceProvider.GetRequiredService<ICoreService>();
        var console = serviceProvider.GetRequiredService<ICliConsole>();
        var logger = serviceProvider.GetRequiredService<ILogger<XrayStartCommand>>();

        try
        {
            await coreService.StartAsync(cancellationToken);
            console.Success("xray start completed.");

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "xray start failed.");
            console.Error(exception.Message);

            return 1;
        }
    }
}
