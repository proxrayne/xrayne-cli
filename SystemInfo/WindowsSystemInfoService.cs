using System.Runtime.InteropServices;

namespace SystemInfo;

/// <summary>
/// Reads system information from Windows system APIs and PowerShell.
/// </summary>
public sealed class WindowsSystemInfoService : SystemInfoService
{
    /// <summary>
    /// Initializes a Windows system information service.
    /// </summary>
    public WindowsSystemInfoService(SystemInfoOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override async Task<CpuInfo> GetCpuInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var first = ReadProcessorPerformance();
            await Task.Delay(250, cancellationToken);
            var second = ReadProcessorPerformance();

            var usages = second
                .Select((current, index) =>
                {
                    if (index >= first.Length)
                    {
                        return null;
                    }

                    var previous = first[index];
                    var idleDelta = current.IdleTime - previous.IdleTime;
                    var kernelDelta = current.KernelTime - previous.KernelTime;
                    var userDelta = current.UserTime - previous.UserTime;
                    var totalDelta = kernelDelta + userDelta;

                    return totalDelta <= 0
                        ? null
                        : (double?)ClampPercent((1 - (idleDelta / (double)totalDelta)) * 100);
                })
                .ToArray();

            return usages.Length == 0
                ? CreateCpuInfoWithoutUsage()
                : CreateCpuInfo(usages);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return CreateCpuInfoWithoutUsage();
        }
    }

    /// <inheritdoc />
    public override async Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunPowerShellAsync(
                "$os = Get-CimInstance Win32_OperatingSystem; [string]::Join('|', @($os.TotalVisibleMemorySize, $os.FreePhysicalMemory))",
                cancellationToken);
            var values = output.Trim().Split('|');

            if (values.Length >= 2
                && TryParseLong(values[0], out var totalKb)
                && TryParseLong(values[1], out var freeKb))
            {
                var total = KilobytesToBytes(totalKb);
                var free = KilobytesToBytes(freeKb);

                return new MemoryInfo(total, Math.Max(0, total - free), free);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
        }

        return new MemoryInfo(0, 0, 0);
    }

    /// <inheritdoc />
    public override async Task<SwapInfo> GetSwapInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunPowerShellAsync(
                "$items = Get-CimInstance Win32_PageFileUsage; [string]::Join('|', @(($items | Measure-Object AllocatedBaseSize -Sum).Sum, ($items | Measure-Object CurrentUsage -Sum).Sum))",
                cancellationToken);
            var values = output.Trim().Split('|');

            if (values.Length >= 2
                && TryParseLong(values[0], out var totalMb)
                && TryParseLong(values[1], out var usedMb))
            {
                var total = MegabytesToBytes(totalMb);
                var used = MegabytesToBytes(usedMb);

                return new SwapInfo(total, used, Math.Max(0, total - used));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
        }

        return new SwapInfo(0, 0, 0);
    }

    /// <inheritdoc />
    public override async Task<long?> GetSystemThreadCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunPowerShellAsync(
                "(Get-CimInstance Win32_PerfRawData_PerfOS_System).Threads",
                cancellationToken);

            return TryParseLong(output.Trim(), out var count) ? count : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private static Task<string> RunPowerShellAsync(string command, CancellationToken cancellationToken)
    {
        var escapedCommand = command.Replace("\"", "\\\"", StringComparison.Ordinal);

        return RunProcessAsync(
            "powershell",
            $"-NoProfile -ExecutionPolicy Bypass -Command \"{escapedCommand}\"",
            cancellationToken);
    }

    private static ProcessorPerformance[] ReadProcessorPerformance()
    {
        var processorCount = Environment.ProcessorCount;
        var entrySize = Marshal.SizeOf<ProcessorPerformance>();
        var buffer = new byte[entrySize * processorCount];
        var status = NtQuerySystemInformation(
            SystemProcessorPerformanceInformation,
            buffer,
            buffer.Length,
            out _);
        if (status != 0)
        {
            return [];
        }

        var result = new ProcessorPerformance[processorCount];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var baseAddress = handle.AddrOfPinnedObject();
            for (var index = 0; index < result.Length; index++)
            {
                var entryAddress = IntPtr.Add(baseAddress, index * entrySize);
                result[index] = Marshal.PtrToStructure<ProcessorPerformance>(entryAddress);
            }
        }
        finally
        {
            handle.Free();
        }

        return result;
    }

    private const int SystemProcessorPerformanceInformation = 8;

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(
        int systemInformationClass,
        byte[] systemInformation,
        int systemInformationLength,
        out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct ProcessorPerformance
    {
        public readonly long IdleTime;
        public readonly long KernelTime;
        public readonly long UserTime;
        public readonly long DpcTime;
        public readonly long InterruptTime;
        public readonly uint InterruptCount;
    }
}
