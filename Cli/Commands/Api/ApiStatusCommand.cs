using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cli.Output;
using Cli.Services.Contracts;

namespace Cli.Commands.Api;

public sealed class ApiStatusCommand : Command
{
    public ApiStatusCommand(IServiceProvider serviceProvider)
        : base("status", "Print XRayne API service status")
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
        var logger = serviceProvider.GetRequiredService<ILogger<ApiStatusCommand>>();
        var apiInstallationService = serviceProvider.GetRequiredService<IApiInstallationService>();

        try
        {
            var output = await apiInstallationService.RunDockerComposeAsync("ps api", cancellationToken);
            Console.WriteLine(string.IsNullOrWhiteSpace(output)
                ? "API service status is unavailable."
                : output);

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "API status lookup failed.");
            console.Error(exception.Message);

            return 1;
        }
    }
}
