namespace SystemInfo;

/// <summary>
/// Defines host directories used by system information storage metrics.
/// </summary>
public sealed record SystemInfoOptions(
    string ApplicationDirectory,
    string DownloadsDirectory);
