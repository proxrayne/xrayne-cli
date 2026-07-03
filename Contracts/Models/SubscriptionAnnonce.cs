namespace Contracts.Models;

/// <summary>
/// Stores subscription announcement settings.
/// </summary>
public sealed class SubscriptionAnnounce
{
    /// <summary>
    /// Gets or sets the announcement message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the announcement URL.
    /// </summary>
    public string? Url { get; set; }
}