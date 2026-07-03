using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SystemInfo;

/// <summary>
/// Base implementation for platform-specific system information services.
/// </summary>
public abstract class SystemInfoService : ISystemInfoService
{
    /// <summary>
    /// Initializes a system information service.
    /// </summary>
    protected SystemInfoService(SystemInfoOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApplicationDirectory))
        {
            throw new ArgumentException("Application directory cannot be empty.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.DownloadsDirectory))
        {
            throw new ArgumentException("Downloads directory cannot be empty.", nameof(options));
        }

        Options = options;
    }

    /// <summary>
    /// Gets configured system information options.
    /// </summary>
    protected SystemInfoOptions Options { get; }

    /// <summary>
    /// Creates a platform-specific system information service.
    /// </summary>
    public static ISystemInfoService Create(SystemInfoOptions options)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsSystemInfoService(options);
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxSystemInfoService(options);
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsSystemInfoService(options);
        }

        throw new PlatformNotSupportedException(
            $"System information service is not supported on {RuntimeInformation.OSDescription}.");
    }

    /// <inheritdoc />
    public async Task<SystemInfoSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var cpuTask = GetCpuInfoAsync(cancellationToken);
        var memoryTask = GetMemoryInfoAsync(cancellationToken);
        var swapTask = GetSwapInfoAsync(cancellationToken);
        var systemThreadCountTask = GetSystemThreadCountAsync(cancellationToken);

        await Task.WhenAll(cpuTask, memoryTask, swapTask, systemThreadCountTask);

        return new SystemInfoSnapshot(
            cpuTask.Result,
            memoryTask.Result,
            swapTask.Result,
            GetStorageInfo(),
            GetUptime(),
            GetCurrentProcessThreadCount(),
            systemThreadCountTask.Result,
            GetNetworkInfo());
    }

    /// <inheritdoc />
    public abstract Task<CpuInfo> GetCpuInfoAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<SwapInfo> GetSwapInfoAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<long?> GetSystemThreadCountAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public TimeSpan GetUptime() => TimeSpan.FromMilliseconds(Environment.TickCount64);

    /// <inheritdoc />
    public int GetCurrentProcessThreadCount() => Process.GetCurrentProcess().Threads.Count;

    /// <inheritdoc />
    public StorageInfo GetStorageInfo()
    {
        var applicationDirectorySize = GetDirectorySize(Options.ApplicationDirectory);
        var downloadsDirectorySize = GetDirectorySize(Options.DownloadsDirectory);
        var applicationDirectoryUsedDiskPercent = GetDirectoryUsedDiskPercent(
            Options.ApplicationDirectory,
            applicationDirectorySize);

        return new StorageInfo(
            new DirectorySizeInfo(Options.ApplicationDirectory, applicationDirectorySize),
            new DirectorySizeInfo(Options.DownloadsDirectory, downloadsDirectorySize),
            applicationDirectoryUsedDiskPercent);
    }

    /// <inheritdoc />
    public NetworkInfo GetNetworkInfo()
    {
        var addresses = GetUsableServerAddresses().ToArray();

        var ipv4 = addresses
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .Select(address => address.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToArray();

        var ipv6 = addresses
            .Where(address => address.AddressFamily == AddressFamily.InterNetworkV6)
            .Where(IsUsableIPv6Address)
            .Select(address => address.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToArray();

        return new NetworkInfo(ipv4, ipv6);
    }

    /// <summary>
    /// Creates CPU information from per-core usage values.
    /// </summary>
    protected static CpuInfo CreateCpuInfo(IReadOnlyCollection<double?> usageByCore)
    {
        var cores = usageByCore
            .Select((usage, index) => new CpuCoreUsage(index, usage))
            .ToArray();
        var values = cores
            .Where(core => core.UsagePercent.HasValue)
            .Select(core => core.UsagePercent!.Value)
            .ToArray();
        var average = values.Length == 0
            ? null
            : (double?)ClampPercent(values.Average());

        return new CpuInfo(Environment.ProcessorCount, average, cores);
    }

    /// <summary>
    /// Creates CPU information without usage values.
    /// </summary>
    protected static CpuInfo CreateCpuInfoWithoutUsage()
    {
        var cores = Enumerable
            .Range(0, Environment.ProcessorCount)
            .Select(index => new CpuCoreUsage(index, null))
            .ToArray();

        return new CpuInfo(Environment.ProcessorCount, null, cores);
    }

    /// <summary>
    /// Converts kilobytes to bytes.
    /// </summary>
    protected static long KilobytesToBytes(long value) => value * 1024;

    /// <summary>
    /// Converts megabytes to bytes.
    /// </summary>
    protected static long MegabytesToBytes(long value) => value * 1024 * 1024;

    /// <summary>
    /// Clamps a value to the 0-100 percentage range.
    /// </summary>
    protected static double ClampPercent(double value) => Math.Clamp(value, 0, 100);

    /// <summary>
    /// Parses an invariant integer value.
    /// </summary>
    protected static bool TryParseLong(string? value, out long result)
    {
        return long.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out result);
    }

    /// <summary>
    /// Runs a process and returns its standard output.
    /// </summary>
    protected static async Task<string> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken,
        bool createNoWindow = true)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = createNoWindow
        };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return await outputTask;
    }

    private static long GetDirectorySize(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return 0;
        }

        long size = 0;
        var pending = new Stack<string>();
        pending.Push(directoryPath);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }

                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    pending.Push(directory);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return size;
    }

    private static double GetDirectoryUsedDiskPercent(
        string directoryPath,
        long directorySize)
    {
        try
        {
            var root = Path.GetPathRoot(directoryPath) ?? directoryPath;
            var drive = new DriveInfo(root);

            return drive.TotalSize <= 0
                ? 0
                : ClampPercent(directorySize / (double)drive.TotalSize * 100);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return 0;
        }
    }

    private static IEnumerable<IPAddress> GetUsableServerAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(item => item.OperationalStatus == OperationalStatus.Up)
            .Where(item => item.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(item => item.GetIPProperties().UnicastAddresses)
            .Select(item => item.Address)
            .Where(address => !IPAddress.IsLoopback(address));
    }

    private static bool IsUsableIPv6Address(IPAddress address)
    {
        return address.AddressFamily == AddressFamily.InterNetworkV6
            && !IPAddress.IsLoopback(address)
            && !address.IsIPv6LinkLocal
            && !address.IsIPv6Multicast;
    }
}
