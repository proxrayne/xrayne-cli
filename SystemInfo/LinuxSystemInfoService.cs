using System.Globalization;

namespace SystemInfo;

/// <summary>
/// Reads system information from Linux procfs.
/// </summary>
public sealed class LinuxSystemInfoService : SystemInfoService
{
    private const string MemInfoPath = "/proc/meminfo";
    private const string StatPath = "/proc/stat";

    /// <summary>
    /// Initializes a Linux system information service.
    /// </summary>
    public LinuxSystemInfoService(SystemInfoOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override async Task<CpuInfo> GetCpuInfoAsync(CancellationToken cancellationToken = default)
    {
        var first = await ReadCpuStatsAsync(cancellationToken);
        await Task.Delay(250, cancellationToken);
        var second = await ReadCpuStatsAsync(cancellationToken);

        var usages = second
            .Where(item => item.Key != "cpu")
            .OrderBy(item => int.Parse(item.Key[3..], CultureInfo.InvariantCulture))
            .Select(item =>
            {
                if (!first.TryGetValue(item.Key, out var previous))
                {
                    return (double?)null;
                }

                var idleDelta = item.Value.Idle - previous.Idle;
                var totalDelta = item.Value.Total - previous.Total;
                if (totalDelta <= 0)
                {
                    return null;
                }

                return ClampPercent((1 - (idleDelta / (double)totalDelta)) * 100);
            })
            .ToArray();

        return usages.Length == 0
            ? CreateCpuInfoWithoutUsage()
            : CreateCpuInfo(usages);
    }

    /// <inheritdoc />
    public override async Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default)
    {
        var values = await ReadMemInfoAsync(cancellationToken);

        var total = KilobytesToBytes(values.GetValueOrDefault("MemTotal"));
        var available = KilobytesToBytes(values.GetValueOrDefault("MemAvailable"));

        return new MemoryInfo(total, Math.Max(0, total - available), available);
    }

    /// <inheritdoc />
    public override async Task<SwapInfo> GetSwapInfoAsync(CancellationToken cancellationToken = default)
    {
        var values = await ReadMemInfoAsync(cancellationToken);

        var total = KilobytesToBytes(values.GetValueOrDefault("SwapTotal"));
        var free = KilobytesToBytes(values.GetValueOrDefault("SwapFree"));

        return new SwapInfo(total, Math.Max(0, total - free), free);
    }

    /// <inheritdoc />
    public override Task<long?> GetSystemThreadCountAsync(CancellationToken cancellationToken = default)
    {
        long count = 0;

        foreach (var processDirectory in Directory.EnumerateDirectories("/proc"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directoryName = Path.GetFileName(processDirectory);
            if (!directoryName.All(char.IsDigit))
            {
                continue;
            }

            var statusFile = Path.Combine(processDirectory, "status");
            if (!TryReadThreadCount(statusFile, out var threadCount))
            {
                continue;
            }

            count += threadCount;
        }

        return Task.FromResult<long?>(count);
    }

    private static async Task<Dictionary<string, CpuStat>> ReadCpuStatsAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, CpuStat>(StringComparer.Ordinal);
        var lines = await File.ReadAllLinesAsync(StatPath, cancellationToken);

        foreach (var line in lines.Where(item => item.StartsWith("cpu", StringComparison.Ordinal)))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
            {
                continue;
            }

            var values = parts
                .Skip(1)
                .Select(value => long.Parse(value, CultureInfo.InvariantCulture))
                .ToArray();

            var idle = values[3] + (values.Length > 4 ? values[4] : 0);
            var total = values.Sum();

            result[parts[0]] = new CpuStat(idle, total);
        }

        return result;
    }

    private static async Task<Dictionary<string, long>> ReadMemInfoAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        var lines = await File.ReadAllLinesAsync(MemInfoPath, cancellationToken);

        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var value = parts[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (TryParseLong(value, out var parsed))
            {
                result[parts[0]] = parsed;
            }
        }

        return result;
    }

    private static bool TryReadThreadCount(string statusFile, out long count)
    {
        count = 0;

        try
        {
            foreach (var line in File.ReadLines(statusFile))
            {
                if (!line.StartsWith("Threads:", StringComparison.Ordinal))
                {
                    continue;
                }

                var value = line["Threads:".Length..].Trim();
                return TryParseLong(value, out count);
            }
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        return false;
    }

    private sealed record CpuStat(long Idle, long Total);
}
