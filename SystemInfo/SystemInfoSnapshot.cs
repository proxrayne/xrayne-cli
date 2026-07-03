namespace SystemInfo;

/// <summary>
/// Contains a point-in-time host system information snapshot.
/// </summary>
public sealed record SystemInfoSnapshot(
    CpuInfo Cpu,
    MemoryInfo Memory,
    SwapInfo Swap,
    StorageInfo Storage,
    TimeSpan Uptime,
    int CurrentProcessThreadCount,
    long? SystemThreadCount,
    NetworkInfo Network);

/// <summary>
/// Contains CPU topology and usage information.
/// </summary>
public sealed record CpuInfo(
    int LogicalCoreCount,
    double? AverageUsagePercent,
    IReadOnlyCollection<CpuCoreUsage> Cores);

/// <summary>
/// Contains usage information for one logical CPU core.
/// </summary>
public sealed record CpuCoreUsage(
    int Index,
    double? UsagePercent);

/// <summary>
/// Contains physical memory information.
/// </summary>
public sealed record MemoryInfo(
    long TotalBytes,
    long UsedBytes,
    long AvailableBytes);

/// <summary>
/// Contains swap or pagefile information.
/// </summary>
public sealed record SwapInfo(
    long TotalBytes,
    long UsedBytes,
    long AvailableBytes);

/// <summary>
/// Contains storage usage information for configured directories.
/// </summary>
public sealed record StorageInfo(
    DirectorySizeInfo ApplicationDirectory,
    DirectorySizeInfo DownloadsDirectory,
    double ApplicationDirectoryUsedDiskPercent);

/// <summary>
/// Contains a directory path and its calculated size.
/// </summary>
public sealed record DirectorySizeInfo(
    string Path,
    long SizeBytes);

/// <summary>
/// Contains local server network addresses.
/// </summary>
public sealed record NetworkInfo(
    IReadOnlyCollection<string> IPv4Addresses,
    IReadOnlyCollection<string> IPv6Addresses);
