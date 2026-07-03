using System.Runtime.InteropServices;
using Cli.Helpers;
using Cli.Services.Contracts;
using Contracts.Values;

namespace Cli.Services;

public sealed class AcmeCertificateService : IAcmeCertificateService
{
    private const string AcmeScriptDownloadUrl = "https://raw.githubusercontent.com/acmesh-official/acme.sh/master/acme.sh";

    private readonly IShellService _shellService;

    public AcmeCertificateService(IShellService shellService)
    {
        _shellService = shellService;
    }

    public async Task<AcmeCertificateIssueResult> IssueCertificateAsync(
        AcmeCertificateRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAcmeScriptAsync(cancellationToken);
        Directory.CreateDirectory(Path.GetDirectoryName(CertificateCommandHelper.GetHostFullChainPath(request.CertName))!);

        var arguments = BuildBaseArguments(request)
            .Append("--issue")
            .Append("-d")
            .Append(request.Identifier)
            .Append("--standalone")
            .Append("--keylength")
            .Append("ec-256")
            .Append("--accountemail")
            .Append(request.Email)
            .ToList();

        AddModeSpecificArguments(arguments, request);

        if (request.Force)
        {
            arguments.Add("--force");
        }

        var output = await RunAcmeAsync(arguments, cancellationToken);
        await InstallCertificateAsync(request, cancellationToken);

        return new AcmeCertificateIssueResult(
            CertificateCommandHelper.GetHostFullChainPath(request.CertName),
            CertificateCommandHelper.GetHostPrivateKeyPath(request.CertName),
            output);
    }

    public async Task<AcmeCertificateIssueResult> RenewCertificateAsync(
        AcmeCertificateRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAcmeScriptAsync(cancellationToken);

        var arguments = BuildBaseArguments(request)
            .Append("--renew")
            .Append("-d")
            .Append(request.Identifier)
            .ToList();

        AddModeSpecificArguments(arguments, request);

        if (request.Force)
        {
            arguments.Add("--force");
        }

        var output = await RunAcmeAsync(arguments, cancellationToken);
        await InstallCertificateAsync(request, cancellationToken);

        return new AcmeCertificateIssueResult(
            CertificateCommandHelper.GetHostFullChainPath(request.CertName),
            CertificateCommandHelper.GetHostPrivateKeyPath(request.CertName),
            output);
    }

    public async Task EnableAutoRenewAsync(CancellationToken cancellationToken)
    {
        await EnsureAcmeScriptAsync(cancellationToken);

        await RunAcmeAsync(
            [
                "--install-cronjob",
                "--home",
                CertificateCommandHelper.AcmeHome,
                "--config-home",
                CertificateCommandHelper.AcmeConfigHome,
                "--cert-home",
                CertificateCommandHelper.AcmeCertHome
            ],
            cancellationToken);
    }

    private async Task InstallCertificateAsync(
        AcmeCertificateRequest request,
        CancellationToken cancellationToken)
    {
        await RunAcmeAsync(
            BuildBaseArguments(request)
                .Append("--install-cert")
                .Append("-d")
                .Append(request.Identifier)
                .Append("--key-file")
                .Append(CertificateCommandHelper.GetHostPrivateKeyPath(request.CertName))
                .Append("--fullchain-file")
                .Append(CertificateCommandHelper.GetHostFullChainPath(request.CertName))
                .Append("--reloadcmd")
                .Append(CertificateCommandHelper.BuildReloadCommand())
                .ToList(),
            cancellationToken);
    }

    private static List<string> BuildBaseArguments(AcmeCertificateRequest request)
    {
        return
        [
            "--home",
            CertificateCommandHelper.AcmeHome,
            "--config-home",
            CertificateCommandHelper.AcmeConfigHome,
            "--cert-home",
            CertificateCommandHelper.AcmeCertHome,
            "--server",
            request.Staging ? "letsencrypt_test" : "letsencrypt"
        ];
    }

    private static void AddModeSpecificArguments(
        List<string> arguments,
        AcmeCertificateRequest request)
    {
        if (!string.Equals(request.Mode, "ip", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        arguments.Add("--cert-profile");
        arguments.Add("shortlived");
        arguments.Add("--days");
        arguments.Add("6");
    }

    private async Task EnsureAcmeScriptAsync(CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            throw new PlatformNotSupportedException("acme.sh certificate issuing is supported only on Linux and macOS hosts.");
        }

        Directory.CreateDirectory(CertificateCommandHelper.AcmeHome);
        Directory.CreateDirectory(CertificateCommandHelper.AcmeConfigHome);
        Directory.CreateDirectory(CertificateCommandHelper.AcmeCertHome);
        Directory.CreateDirectory(PathProvider.Paths.LetsEncryptDirectory);

        if (!File.Exists(CertificateCommandHelper.AcmeScriptPath))
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            var script = await httpClient.GetStringAsync(AcmeScriptDownloadUrl, cancellationToken);
            await File.WriteAllTextAsync(CertificateCommandHelper.AcmeScriptPath, script, cancellationToken);
        }

        await _shellService.RunAsync(
            "chmod",
            ["+x", CertificateCommandHelper.AcmeScriptPath],
            PathProvider.Paths.Root,
            cancellationToken);
    }

    private Task<string> RunAcmeAsync(
        IReadOnlyCollection<string> arguments,
        CancellationToken cancellationToken)
    {
        return _shellService.RunAsync(
            "sh",
            [CertificateCommandHelper.AcmeScriptPath, .. arguments],
            PathProvider.Paths.Root,
            cancellationToken);
    }
}
