namespace Contracts.Enums;

/// <summary>
/// Defines the certificate target type used for remote node HTTPS setup.
/// </summary>
public enum CertificateMode
{
    /// <summary>
    /// Certificate is issued for a DNS domain name.
    /// </summary>
    Domain,

    /// <summary>
    /// Certificate is issued for a public IP address.
    /// </summary>
    Ip
}
