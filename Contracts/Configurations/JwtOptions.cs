namespace Contracts.Configurations;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "xrayne";

    public string Audience { get; set; } = "xrayne";

    public string Secret { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
