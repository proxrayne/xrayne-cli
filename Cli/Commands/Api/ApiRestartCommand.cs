using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cli.Output;
using Cli.Services.Contracts;

namespace Cli.Commands.Api;

public sealed class ApiRestartCommand : Command
{
    public ApiRestartCommand(IServiceProvider serviceProvider)
        : base("restart", "Restart XRayne API service")
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
        var console = serviceProvider.GetRequiredService<ICliConsole>();
        var logger = serviceProvider.GetRequiredService<ILogger<ApiRestartCommand>>();
        var apiInstallationService = serviceProvider.GetRequiredService<IApiInstallationService>();

        try
        {
            await apiInstallationService.RunDockerComposeAsync("restart api", cancellationToken);
            console.Success("API service restarted.");

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "API restart failed.");
            console.Error(exception.Message);

            return 1;
        }
    }
}
