using Cli.Services.Contracts;
using Contracts.Values;

namespace Cli.Services;

public sealed class ApiInstallationService : IApiInstallationService
{
    private readonly IShellService _shellService;

    public ApiInstallationService(IShellService shellService)
    {
        _shellService = shellService;
    }

    public string InstallDirectory => PathProvider.Paths.Root;

    public void EnsureInstalled()
    {
        if (!File.Exists(PathProvider.Paths.EnvConfig))
        {
            throw new InvalidOperationException($"Environment file '{PathProvider.Paths.EnvConfig}' was not found. Run 'xrayne api install' first.");
        }

        if (!File.Exists(PathProvider.Paths.DockerCompose))
        {
            throw new InvalidOperationException($"Compose file '{PathProvider.Paths.DockerCompose}' was not found. Run 'xrayne api install' first.");
        }
    }

    public async Task<string> RunDockerComposeAsync(
        string arguments,
        CancellationToken cancellationToken)
    {
        EnsureInstalled();

        return await _shellService.RunAsync("docker", $"compose {arguments}", InstallDirectory, cancellationToken);
    }

    public async Task<bool> IsApiRunningAsync(CancellationToken cancellationToken)
    {
        var output = await RunDockerComposeAsync("ps --status running --services api", cancellationToken);

        return output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(line => string.Equals(line, "api", StringComparison.OrdinalIgnoreCase));
    }
}
