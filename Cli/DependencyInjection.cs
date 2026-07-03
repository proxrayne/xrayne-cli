using Microsoft.Extensions.DependencyInjection;
using Cli.Commands;
using Cli.Commands.Api;
using Cli.Commands.Admin;
using Cli.Commands.Cert;
using Cli.Commands.Xray;
using Cli.Output;
using Cli.Services;
using Cli.Services.Contracts;

namespace Cli;

public static class CliDependencyInjection
{
    public static IServiceCollection AddCliActions(this IServiceCollection services)
    {
        services.AddSingleton<ICliConsole, CliConsole>();
        services.AddScoped<IShellService, ShellService>();
        services.AddScoped<IApiInstallationService, ApiInstallationService>();
        services.AddScoped<IAcmeCertificateService, AcmeCertificateService>();
        services.AddScoped<IDockerComposeFileService, DockerComposeFileService>();
        services.AddScoped<IRuntimeMigrationService, RuntimeMigrationService>();

        services.AddSingleton<RootCommandFactory>();
        services.AddSingleton<VersionCommand>();
        services.AddSingleton<UpdateCommand>();
        services.AddSingleton<InfoCommand>();
        services.AddSingleton<ApiCommand>();
        services.AddSingleton<ApiInstallCommand>();
        services.AddSingleton<ApiVersionCommand>();
        services.AddSingleton<ApiStatusCommand>();
        services.AddSingleton<ApiStopCommand>();
        services.AddSingleton<ApiStartCommand>();
        services.AddSingleton<ApiRestartCommand>();
        services.AddSingleton<CertCommand>();
        services.AddSingleton<CertInstallCommand>();
        services.AddSingleton<CertRenewCommand>();
        services.AddSingleton<CertStatusCommand>();
        services.AddSingleton<XrayCommand>();
        services.AddSingleton<XrayStartCommand>();
        services.AddSingleton<AdminCommand>();
        services.AddSingleton<AdminCreateCommand>();

        return services;
    }
}
