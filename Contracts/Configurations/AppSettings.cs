using Contracts.Enums;
using Contracts.Models;

namespace Contracts.Configurations;

/// <summary>
/// Contains mutable application settings stored in the database.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the subscription profile title.
    /// </summary>
    public string SubscriptionProfileTitle { get; set; } = "XRayne";

    /// <summary>
    /// Gets or sets the optional subscription support URL.
    /// </summary>
    public string? SubscriptionSupportUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional subscription website URL.
    /// </summary>
    public string? SubscriptionWebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the subscription update interval in hours.
    /// </summary>
    public int SubscriptionUpdateIntervalHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the optional subscription announcement.
    /// </summary>
    public SubscriptionAnnounce? Announce { get; set; }

    /// <summary>
    /// Gets or sets configured webhooks.
    /// </summary>
    public List<AppWebhook> Webhooks { get; set; } = [];
}
