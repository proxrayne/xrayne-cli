using Github;
using System.CommandLine;
using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cli.Output;
using Cli.Services.Contracts;
using Cli.Values;
using Contracts.Values;
using Infrastructure.Utilities;
using Data.Utilities;

namespace Cli.Commands.Api;

public sealed class ApiInstallCommand : Command
{
    public ApiInstallCommand(IServiceProvider serviceProvider)
        : base("install", "Download and install XRayne API and UI Docker images")
    {
        var versionOption = new Option<string>("--version")
        {
            Description = "GitHub release tag to install, or 'latest'.",
            DefaultValueFactory = _ => CliDefaults.LatestVersion
        };

        Add(versionOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            return await ExecuteAsync(
                scope.ServiceProvider,
                parseResult.GetValue(versionOption) ?? CliDefaults.LatestVersion,
                cancellationToken);
        });
    }

    private static async Task<int> ExecuteAsync(
        IServiceProvider serviceProvider,
        string version,
        CancellationToken cancellationToken)
    {
        var console = serviceProvider.GetRequiredService<ICliConsole>();
        var logger = serviceProvider.GetRequiredService<ILogger<ApiInstallCommand>>();
        var shellService = serviceProvider.GetRequiredService<IShellService>();
        var apiInstallationService = serviceProvider.GetRequiredService<IApiInstallationService>();
        var dockerComposeFileService = serviceProvider.GetRequiredService<IDockerComposeFileService>();
        var repository = new GitHubRepository(CliDefaults.XRayneRepositoryUrl);

        try
        {
            var options = ReadInstallOptions();

            var release = await repository.GetReleaseAsync(version, cancellationToken);
            if (release.PreRelease)
            {
                throw new InvalidOperationException("Pre-release versions are not supported. Use a stable release tag.");
            }

            var imageTag = SanitizeDockerTag(release.TagName);
            Directory.CreateDirectory(options.Paths.LogsDirectory);
            Directory.CreateDirectory(options.Paths.PostgresDirectory);
            Directory.CreateDirectory(options.Paths.XrayDirectory);
            Directory.CreateDirectory(options.Paths.DownloadsDirectory);

            await DownloadAndLoadImageAsync(
                console,
                repository,
                shellService,
                release,
                CliDefaults.GetApiImageArchiveName(imageTag),
                CliDefaults.GetApiImageTarName(imageTag),
                options,
                cancellationToken);
            await DownloadAndLoadImageAsync(
                console,
                repository,
                shellService,
                release,
                CliDefaults.GetUiImageArchiveName(imageTag),
                CliDefaults.GetUiImageTarName(imageTag),
                options,
                cancellationToken);

            await WriteEnvFileAsync(imageTag, options, cancellationToken);
            await dockerComposeFileService.WriteApiComposeAsync(options.Paths, imageTag, cancellationToken);

            console.Success($"API installation files are ready in '{options.Paths.Root}'.");
            console.Success("Starting Docker Compose.");

            await apiInstallationService.RunDockerComposeAsync("up -d", cancellationToken);

            PrintInstallSummary(console, release.TagName, imageTag, options);

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "API installation failed.");
            console.Error(exception.Message);

            return 1;
        }
    }

    private static InstallOptions ReadInstallOptions()
    {
        var apiPort = ReadInt(
            $"API port [{CliDefaults.DefaultApiPort}]: ",
            CliDefaults.DefaultApiPort,
            value => value is >= 1 and <= 65535,
            "Port must be between 1 and 65535.");
        var uiPort = ReadInt(
            $"UI port [{CliDefaults.DefaultUiPort}]: ",
            CliDefaults.DefaultUiPort,
            value => value is >= 1 and <= 65535,
            "Port must be between 1 and 65535.");

        Console.Write("Enter PostgreSQL password or leave empty to generate one: ");
        var postgresPassword = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(postgresPassword))
        {
            postgresPassword = PasswordGenerator.Generate(length: 16);
        }

        return new InstallOptions(PathProvider.GetProjectDirectory())
        {
            ApiPort = apiPort,
            UiPort = uiPort,
            PostgresPassword = postgresPassword
        };
    }

    private static async Task DownloadAndLoadImageAsync(
        ICliConsole console,
        GitHubRepository repository,
        IShellService shellService,
        GitHubRelease release,
        string assetName,
        string imageTarName,
        InstallOptions options,
        CancellationToken cancellationToken)
    {
        var asset = release.Assets.SingleOrDefault(item => string.Equals(item.Name, assetName, StringComparison.Ordinal));
        if (asset is null)
        {
            throw new InvalidOperationException($"Release asset '{assetName}' was not found in release '{release.TagName}'.");
        }

        console.Success($"Downloading {asset.Name} from {repository.FullName} {release.TagName}.");
        var imageArchivePath = await repository.DownloadAssetAsync(
            asset,
            options.Paths.DownloadsDirectory,
            cancellationToken);

        var imageTarPath = Path.Combine(options.Paths.Root, imageTarName);
        await DecompressGzipAsync(imageArchivePath, imageTarPath, cancellationToken);

        console.Success($"Loading Docker image from {imageTarName}.");
        await shellService.RunAsync("docker", $"load -i \"{imageTarPath}\"", options.Paths.Root, cancellationToken);
    }

    private static int ReadInt(
        string prompt,
        int defaultValue,
        Func<int, bool> validate,
        string errorMessage)
    {
        while (true)
        {
            Console.Write(prompt);
            var raw = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            if (int.TryParse(raw, out var value) && validate(value))
            {
                return value;
            }

            Console.WriteLine(errorMessage);
        }
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

    private static async Task WriteEnvFileAsync(
        string imageTag,
        InstallOptions options,
        CancellationToken cancellationToken)
    {
        var values = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PORT"] = options.ApiPort.ToString(),
            ["UI_PORT"] = options.UiPort.ToString(),
            ["PROJECT_PATH"] = options.Paths.Root,
            ["API_IMAGE"] = CliDefaults.GetApiImageName(imageTag),
            ["UI_IMAGE"] = CliDefaults.GetUiImageName(imageTag),
            ["POSTGRES_DB"] = CliDefaults.PostgresDatabase,
            ["POSTGRES_HOST_API"] = "localhost",
            ["POSTGRES_USER"] = CliDefaults.PostgresUser,
            ["POSTGRES_PASSWORD"] = options.PostgresPassword,
            ["POSTGRES_CONTAINER_PORT"] = "5432",
            ["POSTGRES_PORT"] = "5432"
        };

        await EnvConfig.UpdateAsync(
            options.Paths.EnvConfig,
            env =>
            {
                foreach (var (key, value) in values)
                {
                    EnvConfig.Set(env, key, value);
                }
            },
            cancellationToken);
    }

    private static void PrintInstallSummary(
        ICliConsole console,
        string releaseTag,
        string imageTag,
        InstallOptions options)
    {
        var panelUrl = $"http://0.0.0.0:{options.UiPort}/";
        var apiUrl = $"http://0.0.0.0:{options.ApiPort}/api";

        console.Header("XRayne API installation completed");
        console.Value("Release", releaseTag);
        console.Value("Docker image", CliDefaults.GetApiImageName(imageTag));
        console.Value("Project path", options.Paths.Root);
        console.Value("Environment file", options.Paths.EnvConfig);
        console.Value("Compose file", options.Paths.DockerCompose);

        console.Section("Panel");
        console.Value("URL", panelUrl);
        console.Value("API URL", apiUrl);
        console.Value("Docker image", CliDefaults.GetUiImageName(imageTag));

        console.Section("PostgreSQL");
        console.Value("API host", "localhost:5432");
        console.Value("CLI host", "localhost:5432");
        console.Value("Database", CliDefaults.PostgresDatabase);
        console.Value("Username", CliDefaults.PostgresUser);
        console.Value("Password", options.PostgresPassword);

        console.Section("Project folders");
        console.Value("Logs", options.Paths.LogsDirectory);
        console.Value("Xray", options.Paths.XrayDirectory);
        console.Value("PostgreSQL data", options.Paths.PostgresDirectory);
        console.Value("Certificates", options.Paths.CertificatesDirectory);
        console.Value("Container project", "/app/shared");

        console.Section("Next useful commands");
        console.Command($"cd {options.Paths.Root}");
        console.Command("docker compose ps");
        console.Command("docker compose logs -f ui");
        console.Command("docker compose logs -f api");
        console.Command("docker compose logs -f postgres");
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

    private sealed class InstallOptions
    {
        public int ApiPort { get; set; }
        public int UiPort { get; set; }
        public string PostgresPassword { get; set; } = string.Empty;
        public ProjectPaths Paths { get; }

        public InstallOptions(string projectPath)
        {
            Paths = new ProjectPaths(projectPath);
        }
    }
}
