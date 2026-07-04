namespace Cli.Services.Contracts;

public sealed record AcmeCertificateIssueResult(
    string FullChainPath,
    string PrivateKeyPath,
    string Output);
