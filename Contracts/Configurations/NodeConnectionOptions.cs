namespace Contracts.Configurations;

/// <summary>
/// Configures remote node connection and reconnect behavior.
/// </summary>
public sealed class NodeConnectionOptions
{
    /// <summary>
    /// Optional fixed node API key used only by the panel in Development when SSH provisioning is bypassed.
    /// </summary>
    public string? DevelopmentApiKey { get; set; }

    public int ReconnectAttempts { get; set; } = 3;

    public int ReconnectDelaySeconds { get; set; } = 30;

    public int PingTimeoutSeconds { get; set; } = 10;

    public int StreamHeartbeatSeconds { get; set; } = 15;
}
