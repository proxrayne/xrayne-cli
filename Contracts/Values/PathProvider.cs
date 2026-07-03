using System.Runtime.InteropServices;

namespace Contracts.Values;

public static class PathProvider
{
    public static string SystemProjectDirectory { get; }

    public static ProjectPaths Paths { get; }

    static PathProvider()
    {
        SystemProjectDirectory = GetProjectSystemDirectory();

        var projectPath = Environment.GetEnvironmentVariable("PROJECT_PATH") ?? SystemProjectDirectory;

        Paths = new ProjectPaths(projectPath);
    }

    public static string GetProjectDirectory()
    {
        var cliDirectory = GetCliDirectory();
        if (cliDirectory != null)
        {
            return cliDirectory.Parent!.FullName;
        }

        return GetProjectSystemDirectory();
    }

    public static DirectoryInfo? GetCliDirectory()
    {
        var baseDirectory = AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return null;
        }

        var cliDirectory = new DirectoryInfo(baseDirectory);
        if (!string.Equals(cliDirectory.Name, "cli", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return cliDirectory;
    }

    private static string GetProjectSystemDirectory()
    {
        const string projectName = "xrayne";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.Combine(programFiles, projectName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Path.Combine("/opt", projectName);
        }

        return Path.Combine(Path.GetTempPath(), projectName);
    }
}

public sealed class ProjectPaths
{
    public string Root { get; set; }
    public string XrayDirectory { get; }
    public string LogsDirectory { get; }
    public string PostgresDirectory { get; }
    public string DownloadsDirectory { get; }
    public string CertificatesDirectory { get; }
    public string LetsEncryptDirectory { get; }
    public string GeoResourcesDirectory { get; }
    public string JsonConfig { get; }
    public string EnvConfig { get; }
    public string DockerCompose { get; }

    public ProjectPaths(string rootPath)
    {
        Root = rootPath;

        XrayDirectory = Path.Combine(rootPath, "xray");
        LogsDirectory = Path.Combine(rootPath, "logs");
        PostgresDirectory = Path.Combine(rootPath, "postgres");
        DownloadsDirectory = Path.Combine(rootPath, "downloads");
        CertificatesDirectory = Path.Combine(rootPath, "certificates");
        LetsEncryptDirectory = Path.Combine(CertificatesDirectory, "letsencrypt");
        GeoResourcesDirectory = Path.Combine(XrayDirectory, "geo");

        JsonConfig = Path.Combine(rootPath, "config.json");
        EnvConfig = Path.Combine(rootPath, ".env");
        DockerCompose = Path.Combine(rootPath, "docker-compose.yml");
    }
}
