namespace Infrastructure.Services;

public interface ICoreService
{
    bool GetIsRunning();
    bool GetIsInstalled();
    string GetVersion();
    string? TryGetVersion();

    Task StopAsync(CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task RestartAsync(CancellationToken cancellationToken = default);
    Task SetupAsync(string corePath, CancellationToken cancellationToken = default);
}
