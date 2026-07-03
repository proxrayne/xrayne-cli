using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cli.Output;
using Cli.Services.Contracts;

namespace Cli.Commands.Api;

public sealed class ApiStartCommand : Command
{
    public ApiStartCommand(IServiceProvider serviceProvider)
        : base("start", "Start XRayne API service")
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
        var logger = serviceProvider.GetRequiredService<ILogger<ApiStartCommand>>();
        var apiInstallationService = serviceProvider.GetRequiredService<IApiInstallationService>();

        try
        {
            if (await apiInstallationService.IsApiRunningAsync(cancellationToken))
            {
                Console.Write("API service is already running. Restart it? [y/N]: ");
                var answer = Console.ReadLine();
                if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    console.Success("API service is already running.");

                    return 0;
                }

                await apiInstallationService.RunDockerComposeAsync("restart api", cancellationToken);
                console.Success("API service restarted.");

                return 0;
            }

            await apiInstallationService.RunDockerComposeAsync("up -d api", cancellationToken);
            console.Success("API service started.");

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "API start failed.");
            console.Error(exception.Message);

            return 1;
        }
    }
}
