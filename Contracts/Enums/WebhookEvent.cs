namespace Contracts.Enums;

/// <summary>
/// Defines webhook notification events.
/// </summary>
[Flags]
public enum WebhookEvent : ulong
{
    /// <summary>
    /// No webhook events are enabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// A user was created.
    /// </summary>
    UserCreated = 1UL << 0,

    /// <summary>
    /// A user was updated.
    /// </summary>
    UserUpdated = 1UL << 1,

    /// <summary>
    /// A user was deleted.
    /// </summary>
    UserDeleted = 1UL << 2,

    /// <summary>
    /// A device connected.
    /// </summary>
    DeviceConnected = 1UL << 3,

    /// <summary>
    /// A device was revoked.
    /// </summary>
    DeviceRevoked = 1UL << 4,

    /// <summary>
    /// A user status changed.
    /// </summary>
    UserStatusChanged = 1UL << 5,

    /// <summary>
    /// User traffic was reset.
    /// </summary>
    TrafficReset = 1UL << 6,

    /// <summary>
    /// A traffic usage percentage threshold was reached.
    /// </summary>
    TrafficPercentThresholdReached = 1UL << 7,

    /// <summary>
    /// A subscription expiration hours threshold was reached.
    /// </summary>
    SubscriptionHoursThresholdReached = 1UL << 8
}
