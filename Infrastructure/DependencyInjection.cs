using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Contracts.Configurations;
using Infrastructure.Services;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCliInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<XrayOptions>(configuration.GetSection("Xray"));
        services.AddSingleton<ICoreService, CoreService>();

        return services;
    }
}
