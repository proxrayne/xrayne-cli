namespace SystemInfo;

/// <summary>
/// Reads host CPU, memory, storage, uptime, thread, and network information.
/// </summary>
public interface ISystemInfoService
{
    /// <summary>
    /// Gets a current full system information snapshot.
    /// </summary>
    Task<SystemInfoSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets CPU information.
    /// </summary>
    Task<CpuInfo> GetCpuInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets memory information.
    /// </summary>
    Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets swap or pagefile information.
    /// </summary>
    Task<SwapInfo> GetSwapInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configured directory storage information.
    /// </summary>
    StorageInfo GetStorageInfo();

    /// <summary>
    /// Gets host uptime.
    /// </summary>
    TimeSpan GetUptime();

    /// <summary>
    /// Gets the current process thread count.
    /// </summary>
    int GetCurrentProcessThreadCount();

    /// <summary>
    /// Gets the system-wide thread count when supported by the host platform.
    /// </summary>
    Task<long?> GetSystemThreadCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets local server network addresses.
    /// </summary>
    NetworkInfo GetNetworkInfo();
}
