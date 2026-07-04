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
