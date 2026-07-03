using Microsoft.Extensions.Configuration;

namespace Contracts.Configurations;

/// <summary>
/// Contains panel bootstrap settings loaded from environment configuration.
/// </summary>
public sealed class PanelSettings
{
    /// <summary>
    /// Gets or sets the IP address used by the panel listener.
    /// </summary>
    public string? BindIp { get; set; }

    /// <summary>
    /// Gets or sets the public panel domain.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the panel listener port.
    /// </summary>
    public required int Port { get; set; }

    /// <summary>
    /// Gets or sets the public certificate path used by Kestrel.
    /// </summary>
    public string? CertPublicKeyPath { get; set; }

    /// <summary>
    /// Gets or sets the private certificate key path used by Kestrel.
    /// </summary>
    public string? CertPrivateKeyPath { get; set; }

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    public PanelSettings Clone() => new()
    {
        Port = Port,
        BindIp = BindIp,
        Domain = Domain,
        CertPublicKeyPath = CertPublicKeyPath,
        CertPrivateKeyPath = CertPrivateKeyPath
    };

    /// <summary>
    /// Parses panel bootstrap settings from configuration.
    /// </summary>
    public static PanelSettings Parse(IConfiguration configuration)
    {
        var port = configuration.GetValue<int>("PORT", 5097);
        var bindIp = configuration.GetValue<string>("IP");
        var domain = configuration.GetValue<string>("DOMAIN");
        var panelCertPublicKeyPath = configuration.GetValue<string>("CERT_PUBLIC_KEY_PATH");
        var panelCertPrivateKeyPath = configuration.GetValue<string>("CERT_PRIVATE_KEY_PATH");

        return new()
        {
            Port = port,
            BindIp = bindIp,
            Domain = domain,
            CertPrivateKeyPath = panelCertPrivateKeyPath,
            CertPublicKeyPath = panelCertPublicKeyPath,
        };
    }
}
