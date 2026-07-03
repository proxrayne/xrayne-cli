using System.Globalization;
using System.Text.RegularExpressions;

namespace SystemInfo;

/// <summary>
/// Reads system information from macOS command-line system tools.
/// </summary>
public sealed partial class MacOsSystemInfoService : SystemInfoService
{
    /// <summary>
    /// Initializes a macOS system information service.
    /// </summary>
    public MacOsSystemInfoService(SystemInfoOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override async Task<CpuInfo> GetCpuInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunProcessAsync(
                "sh",
                "-c \"top -l 2 -n 0 -s 1 | grep 'CPU usage' | tail -1\"",
                cancellationToken);
            var idleMatch = CpuIdleRegex().Match(output);

            if (idleMatch.Success
                && double.TryParse(idleMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var idle))
            {
                var totalUsage = ClampPercent(100 - idle);
                var cores = Enumerable
                    .Range(0, Environment.ProcessorCount)
                    .Select(_ => (double?)totalUsage)
                    .ToArray();

                return CreateCpuInfo(cores);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
        }

        return CreateCpuInfoWithoutUsage();
    }

    /// <inheritdoc />
    public override async Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalOutput = await RunProcessAsync("sysctl", "-n hw.memsize", cancellationToken);
            var vmOutput = await RunProcessAsync("vm_stat", string.Empty, cancellationToken);

            if (!TryParseLong(totalOutput.Trim(), out var total))
            {
                return new MemoryInfo(0, 0, 0);
            }

            var pageSize = GetPageSize(vmOutput);
            var freePages = GetVmStatValue(vmOutput, "Pages free")
                + GetVmStatValue(vmOutput, "Pages speculative");
            var available = Math.Min(total, freePages * pageSize);

            return new MemoryInfo(total, Math.Max(0, total - available), available);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new MemoryInfo(0, 0, 0);
        }
    }

    /// <inheritdoc />
    public override async Task<SwapInfo> GetSwapInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunProcessAsync("sysctl", "-n vm.swapusage", cancellationToken);
            var match = SwapRegex().Match(output);

            if (!match.Success)
            {
                return new SwapInfo(0, 0, 0);
            }

            var total = ToBytes(match.Groups["total"].Value, match.Groups["totalUnit"].Value);
            var used = ToBytes(match.Groups["used"].Value, match.Groups["usedUnit"].Value);

            return new SwapInfo(total, used, Math.Max(0, total - used));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new SwapInfo(0, 0, 0);
        }
    }

    /// <inheritdoc />
    public override async Task<long?> GetSystemThreadCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunProcessAsync("sysctl", "-n kern.num_threads", cancellationToken);

            return TryParseLong(output.Trim(), out var count) ? count : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private static long GetPageSize(string vmStatOutput)
    {
        var match = PageSizeRegex().Match(vmStatOutput);

        return match.Success && TryParseLong(match.Groups[1].Value, out var pageSize)
            ? pageSize
            : 4096;
    }

    private static long GetVmStatValue(string vmStatOutput, string key)
    {
        foreach (var line in vmStatOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith(key, StringComparison.Ordinal))
            {
                continue;
            }

            var value = line.Split(':', 2)[1].Trim().TrimEnd('.');

            return TryParseLong(value, out var parsed) ? parsed : 0;
        }

        return 0;
    }

    private static long ToBytes(string value, string unit)
    {
        if (!double.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
        {
            return 0;
        }

        var multiplier = unit.ToUpperInvariant() switch
        {
            "K" => 1024d,
            "M" => 1024d * 1024,
            "G" => 1024d * 1024 * 1024,
            "T" => 1024d * 1024 * 1024 * 1024,
            _ => 1d
        };

        return (long)(parsed * multiplier);
    }

    [GeneratedRegex(@"([\d.]+)% idle", RegexOptions.IgnoreCase)]
    private static partial Regex CpuIdleRegex();

    [GeneratedRegex(@"page size of (\d+) bytes")]
    private static partial Regex PageSizeRegex();

    [GeneratedRegex(@"total = (?<total>[\d.]+)(?<totalUnit>[KMGT])\s+used = (?<used>[\d.]+)(?<usedUnit>[KMGT])", RegexOptions.IgnoreCase)]
    private static partial Regex SwapRegex();
}
