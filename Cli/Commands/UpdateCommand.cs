using Github;
using System.CommandLine;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cli.Output;
using Cli.Services;
using Cli.Services.Contracts;
using Cli.Values;
using Contracts.Values;
using Data.Utilities;

namespace Cli.Commands;

public sealed class UpdateCommand : Command
{
    public UpdateCommand(IServiceProvider serviceProvider)
        : base("update", "Update XRayne CLI, API, and UI to a selected release")
    {
        var versionOption = new Option<string>("--version")
        {
            Description = "GitHub release tag to install, or 'latest'.",
            DefaultValueFactory = _ => CliDefaults.LatestVersion
        };

        var componentOption = new Option<string>("--component")
        {
            Description = "Component to update: all, api, ui, or cli.",
            DefaultValueFactory = _ => UpdateComponent.All.Value
        };

        var forceOption = new Option<bool>("--force")
        {
            Description = "Install the selected release even when it matches the installed version."
        };

        Add(versionOption);
        Add(componentOption);
        Add(forceOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            return await ExecuteAsync(
                scope.ServiceProvider,
                parseResult.GetValue(versionOption) ?? CliDefaults.LatestVersion,
                UpdateComponent.Parse(parseResult.GetValue(componentOption)),
                parseResult.GetValue(forceOption),
                cancellationToken);
        });
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider serviceProvider,
        string version,
        UpdateComponent component,
        bool force,
        CancellationToken cancellationToken)
    {
        var console = serviceProvider.GetRequiredService<ICliConsole>();
        var logger = serviceProvider.GetRequiredService<ILogger<UpdateCommand>>();
        var shellService = serviceProvider.GetRequiredService<IShellService>();
        var apiInstallationService = serviceProvider.GetRequiredService<IApiInstallationService>();
        var runtimeMigrationService = serviceProvider.GetRequiredService<IRuntimeMigrationService>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var repository = new GitHubRepository(CliDefaults.XRayneRepositoryUrl);

        try
        {
            var release = await repository.GetReleaseAsync(version, cancellationToken);
            if (release.PreRelease)
            {
                throw new InvalidOperationException("Pre-release versions are not supported. Use a stable release tag.");
            }

            var targetVersion = SanitizeDockerTag(release.TagName);
            var targetSchemaVersion = RuntimeSchemaCatalog.ResolveForRelease(release.TagName);

            console.Header("XRayne update");
            console.Value("Repository", repository.FullName);
            console.Value("Target release", release.TagName);
            console.Value("Target runtime schema", targetSchemaVersion.ToString());
            console.Value("Component", component.Value);

            var migrationResult = await runtimeMigrationService.MigrateToAsync(
                targetSchemaVersion,
                cancellationToken);
            PrintMigrationResult(console, migrationResult);

            var apiRestarted = false;
            var uiRestarted = false;

            if (component.UpdateApi)
            {
                apiRestarted = await UpdateDockerImageAsync(
                    console,
                    repository,
                    shellService,
                    apiInstallationService,
                    configuration,
                    release,
                    targetVersion,
                    force,
                    "API",
                    CliDefaults.ApiImageVariable,
                    CliDefaults.GetApiImageArchiveName,
                    CliDefaults.GetApiImageTarName,
                    CliDefaults.GetApiImageName,
                    CliDefaults.ExtractApiImageVersion,
                    "api",
                    cancellationToken);
            }

            if (component.UpdateUi)
            {
                uiRestarted = await UpdateDockerImageAsync(
                    console,
                    repository,
                    shellService,
                    apiInstallationService,
                    configuration,
                    release,
                    targetVersion,
                    force,
                    "UI",
                    CliDefaults.UiImageVariable,
                    CliDefaults.GetUiImageArchiveName,
                    CliDefaults.GetUiImageTarName,
                    CliDefaults.GetUiImageName,
                    CliDefaults.ExtractUiImageVersion,
                    "ui",
                    cancellationToken);
            }

            if (migrationResult.Changed && !apiRestarted && !uiRestarted && (component.UpdateApi || component.UpdateUi))
            {
                console.Success("Restarting Docker Compose after runtime migration.");
                await apiInstallationService.RunDockerComposeAsync("up -d --force-recreate", cancellationToken);
            }

            if (component.UpdateCli)
            {
                await UpdateCliAsync(
                    console,
                    repository,
                    release,
                    force,
                    cancellationToken);
            }

            console.Header("Update completed");
            console.Value("Release", release.TagName);
            console.Value("Project path", PathProvider.Paths.Root);
            console.Command("xrayne version");
            console.Command("xrayne info");

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "XRayne update failed.");
            console.Error(exception.Message);

            return 1;
        }
    }

    private static async Task<bool> UpdateDockerImageAsync(
        ICliConsole console,
        GitHubRepository gitHubRepository,
        IShellService shellService,
        IApiInstallationService apiInstallationService,
        IConfiguration configuration,
        GitHubRelease release,
        string targetVersion,
        bool force,
        string componentName,
        string imageVariable,
        Func<string, string> getArchiveName,
        Func<string, string> getTarName,
        Func<string, string> getImageName,
        Func<string, string?> extractImageVersion,
        string composeServiceName,
        CancellationToken cancellationToken)
    {
        console.Section(componentName);
        apiInstallationService.EnsureInstalled();

        var installedVersion = extractImageVersion(configuration[imageVariable] ?? string.Empty);

        console.Value("Installed version", installedVersion ?? "not installed");
        console.Value("Target version", targetVersion);

        if (!force && string.Equals(installedVersion, targetVersion, StringComparison.Ordinal))
        {
            console.Success($"{componentName} is already on the selected release.");
            return false;
        }

        var assetName = getArchiveName(targetVersion);
        var asset = release.Assets.SingleOrDefault(item => string.Equals(item.Name, assetName, StringComparison.Ordinal));
        if (asset is null)
        {
            throw new InvalidOperationException($"Release asset '{assetName}' was not found in release '{release.TagName}'.");
        }

        Directory.CreateDirectory(PathProvider.Paths.DownloadsDirectory);

        console.Success($"Downloading {asset.Name} from {gitHubRepository.FullName} {release.TagName}.");
        var imageArchivePath = await gitHubRepository.DownloadAssetAsync(
            asset,
            PathProvider.Paths.DownloadsDirectory,
            cancellationToken);

        var imageTarPath = Path.Combine(PathProvider.Paths.Root, getTarName(targetVersion));
        await DecompressGzipAsync(imageArchivePath, imageTarPath, cancellationToken);

        console.Success("Loading Docker image.");
        await shellService.RunAsync("docker", $"load -i \"{imageTarPath}\"", PathProvider.Paths.Root, cancellationToken);

        await UpdateEnvImageAsync(imageVariable, getImageName(targetVersion), cancellationToken);

        console.Success($"Restarting Docker Compose service '{composeServiceName}'.");
        await apiInstallationService.RunDockerComposeAsync($"up -d {composeServiceName}", cancellationToken);

        console.Value($"Previous {componentName} version", installedVersion ?? "not installed");
        console.Value($"Current {componentName} version", targetVersion);
        console.Value("Docker image", getImageName(targetVersion));

        return true;
    }

    private static void PrintMigrationResult(
        ICliConsole console,
        RuntimeMigrationResult result)
    {
        console.Section("Runtime migrations");
        console.Value("Current schema", result.CurrentSchemaVersion.ToString());
        console.Value("Target schema", result.TargetSchemaVersion.ToString());

        if (!result.Changed)
        {
            if (result.CurrentSchemaVersion == result.TargetSchemaVersion)
            {
                console.Success("Runtime schema is already compatible.");
            }
            else
            {
                console.Success("Runtime files are not installed; migration skipped.");
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(result.BackupDirectory))
        {
            console.Value("Backup", result.BackupDirectory);
        }

        foreach (var migration in result.AppliedMigrations)
        {
            console.Value("Applied", migration);
        }
    }

    private static async Task UpdateCliAsync(
        ICliConsole console,
        GitHubRepository gitHubRepository,
        GitHubRelease release,
        bool force,
        CancellationToken cancellationToken)
    {
        console.Section("CLI");

        var installedVersion = GetCliVersion();
        console.Value("Installed version", installedVersion);
        console.Value("Target version", release.TagName);

        if (!force && string.Equals(installedVersion, release.TagName, StringComparison.Ordinal))
        {
            console.Success("CLI is already on the selected release.");
            return;
        }

        var assetInfo = GetCliAssetInfo();
        var asset = release.Assets.SingleOrDefault(item => string.Equals(item.Name, assetInfo.AssetName, StringComparison.Ordinal));
        if (asset is null)
        {
            throw new InvalidOperationException($"Release asset '{assetInfo.AssetName}' was not found in release '{release.TagName}'.");
        }

        Directory.CreateDirectory(PathProvider.Paths.DownloadsDirectory);

        var updateDirectory = Path.Combine(PathProvider.Paths.DownloadsDirectory, $"cli-update-{Guid.NewGuid():N}");
        var extractDirectory = Path.Combine(updateDirectory, "extract");

        Directory.CreateDirectory(updateDirectory);
        Directory.CreateDirectory(extractDirectory);

        console.Success($"Downloading {asset.Name} from {gitHubRepository.FullName} {release.TagName}.");
        var archivePath = await gitHubRepository.DownloadAssetAsync(asset, updateDirectory, cancellationToken);

        ExtractCliArchive(archivePath, extractDirectory, assetInfo);

        var executablePath = Directory
            .GetFiles(extractDirectory, assetInfo.ExecutableName, SearchOption.AllDirectories)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException($"Executable '{assetInfo.ExecutableName}' was not found inside '{asset.Name}'.");
        }

        var installDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ScheduleWindowsCliReplacement(console, extractDirectory, installDirectory, updateDirectory);
            return;
        }

        ReplaceCliFiles(extractDirectory, installDirectory, assetInfo.ExecutableName);
        console.Success($"CLI files were updated in '{installDirectory}'.");
    }

    private static void ExtractCliArchive(
        string archivePath,
        string extractDirectory,
        CliAssetInfo assetInfo)
    {
        if (assetInfo.IsZip)
        {
            ZipFile.ExtractToDirectory(archivePath, extractDirectory, overwriteFiles: true);
            return;
        }

        using var archive = File.OpenRead(archivePath);
        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        TarFile.ExtractToDirectory(gzip, extractDirectory, overwriteFiles: true);
    }

    private static void ReplaceCliFiles(
        string sourceDirectory,
        string targetDirectory,
        string executableName)
    {
        foreach (var sourcePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            var targetPath = Path.Combine(targetDirectory, relativePath);
            var targetParent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetParent))
            {
                Directory.CreateDirectory(targetParent);
            }

            var temporaryTargetPath = $"{targetPath}.update-{Guid.NewGuid():N}";
            File.Copy(sourcePath, temporaryTargetPath, overwrite: true);
            File.Move(temporaryTargetPath, targetPath, overwrite: true);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                && string.Equals(Path.GetFileName(targetPath), executableName, StringComparison.Ordinal))
            {
                File.SetUnixFileMode(
                    targetPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }
    }

    private static void ScheduleWindowsCliReplacement(
        ICliConsole console,
        string sourceDirectory,
        string targetDirectory,
        string updateDirectory)
    {
        var currentPid = Environment.ProcessId;
        var scriptPath = Path.Combine(updateDirectory, "finish-cli-update.cmd");
        var script = $"""
                      @echo off
                      setlocal
                      set "PID={currentPid}"
                      set "SOURCE={sourceDirectory}"
                      set "TARGET={targetDirectory}"

                      :wait
                      tasklist /FI "PID eq %PID%" 2>NUL | find "%PID%" >NUL
                      if not errorlevel 1 (
                        timeout /T 1 /NOBREAK >NUL
                        goto wait
                      )

                      xcopy "%SOURCE%\*" "%TARGET%\" /E /I /Y >NUL
                      rmdir /S /Q "{updateDirectory}"
                      echo XRayne CLI update completed.
                      """;

        File.WriteAllText(scriptPath, script, Encoding.Default);

        Process.Start(new ProcessStartInfo("cmd.exe", $"/c start \"\" \"{scriptPath}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        });

        console.Warning("Windows keeps the running executable locked. CLI replacement was scheduled and will finish after this command exits.");
    }

    private static CliAssetInfo GetCliAssetInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            return new CliAssetInfo("xrayne-cli-win-x64.zip", "xrayne.exe", IsZip: true);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            return new CliAssetInfo("xrayne-cli-linux-x64.tar.gz", "xrayne", IsZip: false);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            return new CliAssetInfo("xrayne-cli-osx-arm64.tar.gz", "xrayne", IsZip: false);
        }

        throw new InvalidOperationException(
            $"Unsupported OS/architecture: {RuntimeInformation.OSDescription} {RuntimeInformation.ProcessArchitecture}. Published CLI builds: win-x64, linux-x64, osx-arm64.");
    }

    private static string GetCliVersion()
    {
        var assembly = typeof(UpdateCommand).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+', 2)[0];
        }

        return assembly.GetName().Version?.ToString() ?? "unknown";
    }

    private static async Task UpdateEnvImageAsync(
        string imageVariable,
        string imageName,
        CancellationToken cancellationToken)
    {
        await EnvConfig.SetAsync(
            imageVariable,
            imageName,
            PathProvider.Paths.EnvConfig,
            cancellationToken);
    }

    private static async Task DecompressGzipAsync(
        string archivePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var input = File.OpenRead(archivePath);
        await using var gzip = new GZipStream(input, CompressionMode.Decompress);
        await using var output = File.Create(destinationPath);
        await gzip.CopyToAsync(output, cancellationToken);
    }

    private static string SanitizeDockerTag(string value)
    {
        var chars = value.Select(character =>
            char.IsAsciiLetterOrDigit(character) || character is '_' or '.' or '-'
                ? character
                : '-').ToArray();
        var tag = new string(chars).Trim('-');

        return string.IsNullOrWhiteSpace(tag) ? "latest" : tag;
    }

    private sealed record UpdateComponent(
        string Value,
        bool UpdateApi,
        bool UpdateUi,
        bool UpdateCli)
    {
        public static readonly UpdateComponent All = new("all", UpdateApi: true, UpdateUi: true, UpdateCli: true);
        public static readonly UpdateComponent Api = new("api", UpdateApi: true, UpdateUi: false, UpdateCli: false);
        public static readonly UpdateComponent Ui = new("ui", UpdateApi: false, UpdateUi: true, UpdateCli: false);
        public static readonly UpdateComponent Cli = new("cli", UpdateApi: false, UpdateUi: false, UpdateCli: true);

        public static UpdateComponent Parse(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                null or "" or "all" => All,
                "api" => Api,
                "ui" => Ui,
                "cli" => Cli,
                _ => throw new InvalidOperationException("Component must be one of: all, api, ui, cli.")
            };
        }
    }

    private sealed record CliAssetInfo(
        string AssetName,
        string ExecutableName,
        bool IsZip);
}
