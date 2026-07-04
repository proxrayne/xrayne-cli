namespace Contracts.Values;

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
