namespace Cli.Services.Contracts;

public interface IAcmeCertificateService
{
    Task<AcmeCertificateIssueResult> IssueCertificateAsync(
        AcmeCertificateRequest request,
        CancellationToken cancellationToken);

    Task<AcmeCertificateIssueResult> RenewCertificateAsync(
        AcmeCertificateRequest request,
        CancellationToken cancellationToken);

    Task EnableAutoRenewAsync(CancellationToken cancellationToken);
}

public sealed record AcmeCertificateRequest(
    string Mode,
    string Identifier,
    string Email,
    string CertName,
    bool Staging,
    bool Force);

public sealed record AcmeCertificateIssueResult(
    string FullChainPath,
    string PrivateKeyPath,
    string Output);
