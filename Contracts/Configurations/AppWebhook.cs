using Contracts.Enums;

namespace Contracts.Configurations;

/// <summary>
/// Contains a single webhook configuration.
/// </summary>
public sealed class AppWebhook
{
    /// <summary>
    /// Gets or sets the webhook identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the webhook target URL.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Gets or sets enabled webhook events.
    /// </summary>
    public WebhookEvent Events { get; set; }

    /// <summary>
    /// Gets or sets the optional webhook secret.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets retry attempts count after a delivery error.
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets retry interval in seconds.
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets subscription expiration thresholds in hours.
    /// </summary>
    public List<int> SubscriptionExpirationThresholdHours { get; set; } = [];

    /// <summary>
    /// Gets or sets traffic usage thresholds in percents.
    /// </summary>
    public List<int> TrafficThresholdPercents { get; set; } = [];
}
