namespace Cli.Services.Contracts;

public sealed record AcmeCertificateRequest(
    string Mode,
    string Identifier,
    string Email,
    string CertName,
    bool Staging,
    bool Force);
