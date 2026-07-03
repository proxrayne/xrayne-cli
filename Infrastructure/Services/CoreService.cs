using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xray.Config.Enums;
using Xray.Config.Models;
using Xray.Core;
using Xray.Core.Models;
using Contracts.Configurations;
using Contracts.Values;
using Data.Utilities;

namespace Infrastructure.Services;

public sealed class CoreService(ILogger<CoreService> logger, IOptionsMonitor<XrayOptions> options) : ICoreService
{
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private IXrayProcessCore? _core = TryInitializeCore(options.CurrentValue, logger);

    private IXrayProcessCore _safeCore => _core == null ? throw new InvalidOperationException("Core not installed.") : _core;

    public bool GetIsRunning() => _core != null && _core.IsStarted();

    public bool GetIsInstalled() => _core != null;

    public string GetVersion() => _safeCore.Version();

    public string? TryGetVersion() => _core == null ? null : _core.Version();

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return CallLockedAsync(() => StopUnsafeAsync(), cancellationToken);
    }

    /// <summary>
    /// Setup new core. If core is already running, it will be stopped before setup.
    /// </summary>
    /// <param name="directory">Path to download core folder. Example: /xray-v26_5_3.</param>
    /// <returns></returns>
    public async Task SetupAsync(string directory, CancellationToken cancellationToken = default)
    {
        await CallLockedAsync(async () =>
        {
            var newCore = CreateCoreFromDirectory(directory);

            logger.LogInformation("New core version: {Version}.", newCore.Version());

            var isStarted = GetIsRunning();

            if (isStarted) await StopUnsafeAsync();

            _core = newCore;

            await JsonConfig.SetAsync(
                "Xray:Directory",
                directory,
                cancellationToken: cancellationToken);

            if (isStarted) await StartUnsafeAsync(cancellationToken);

        }, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return CallLockedAsync(() => StartUnsafeAsync(cancellationToken), cancellationToken);
    }

    public Task RestartAsync(CancellationToken cancellationToken = default)
    {
        return CallLockedAsync(async () =>
        {
            await StopUnsafeAsync(cancellationToken);
            await StartUnsafeAsync(cancellationToken);
        }, cancellationToken);
    }

    private XrayConfig GetConfig(CancellationToken cancellationToken = default)
    {
        return new XrayConfig()
        {
            Log = new LogConfig()
            {
                LogLevel = Xray.Config.Enums.LogLevel.Warning,
            },
            Inbounds = new List<Inbound>()
            {
                new SocksInbound()
                {
                    Tag = "socks-in",
                    Listen = "0.0.0.0",
                    Port = new Port(10808),
                    Settings = new Inbound.SocksSettings()
                    {
                        Auth = SocksAuth.NoAuth,
                        Udp = true
                    }
                }
            },
            Outbounds = new List<Outbound>()
            {
                new FreedomOutbound() {
                    Tag = "direct"
                },
            }
        };
    }

    private static IXrayProcessCore? TryInitializeCore(XrayOptions options, ILogger logger)
    {
        if (string.IsNullOrEmpty(options.Directory))
        {
            logger.LogInformation("Xray directory is not set. Core will not be initialized.");

            return null;
        }

        return CreateCoreFromDirectory(options.Directory);
    }

    private static IXrayProcessCore CreateCoreFromDirectory(string directory) => new XrayProcessCore(new XrayProcessOptions()
    {
        WorkingDirectory = Path.Combine(PathProvider.Paths.XrayDirectory, directory),
        ProcessName = "xray",
    });

    private Task StartUnsafeAsync(CancellationToken cancellationToken)
    {
        if (_core is null)
        {
            throw new InvalidOperationException("Core not installed.");
        }

        if (_core.IsStarted())
        {
            return Task.CompletedTask;
        }

        return _core.StartAsync(GetConfig(cancellationToken), cancellationToken);
    }

    private async Task StopUnsafeAsync(CancellationToken cancellationToken = default)
    {
        if (_core is null || !_core.IsStarted())
        {
            return;
        }

        await _core.StopAsync(cancellationToken);
    }

    private async Task CallLockedAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            await action();
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }
}
