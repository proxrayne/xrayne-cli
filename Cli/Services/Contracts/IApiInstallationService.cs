namespace Cli.Services.Contracts;

public interface IApiInstallationService
{
    string InstallDirectory { get; }

    void EnsureInstalled();

    Task<string> RunDockerComposeAsync(
        string arguments,
        CancellationToken cancellationToken);

    Task<bool> IsApiRunningAsync(CancellationToken cancellationToken);
}
