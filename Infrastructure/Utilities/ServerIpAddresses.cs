namespace Infrastructure.Utilities;

public sealed record ServerIpAddresses(
    IReadOnlyCollection<string> IPv4Addresses,
    IReadOnlyCollection<string> IPv6Addresses);
