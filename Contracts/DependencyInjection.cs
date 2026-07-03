using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Contracts.Configurations;

namespace Contracts;

public static class DependencyInjection
{
    public static IServiceCollection AddContracts(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<XrayOptions>(configuration.GetSection("Xray"));

        return services;
    }
}
