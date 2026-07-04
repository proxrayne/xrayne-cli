using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Infrastructure.Utilities;

public static class NetworkAddress
{
    private static readonly Uri PublicIpAddressEndpoint = new("https://api.ipify.org");

    public static ServerIpAddresses GetServerIpAddresses()
    {
        var addresses = GetUsableServerAddresses().ToArray();

        var ipv4 = addresses
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .Select(address => address.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToArray();

        var ipv6 = addresses
            .Where(address => address.AddressFamily == AddressFamily.InterNetworkV6)
            .Where(IsUsableIPv6Address)
            .Select(address => address.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToArray();

        return new ServerIpAddresses(ipv4, ipv6);
    }

    public static string GetLocalServerIpAddress()
    {
        var address = GetUsableServerAddresses()
            .FirstOrDefault(IsUsableIPv4Address);

        return address?.ToString() ?? IPAddress.Loopback.ToString();
    }

    public static async Task<string> GetPublicIpAddressAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var value = await httpClient.GetStringAsync(PublicIpAddressEndpoint, cancellationToken);

        return NormalizePublicIPv4Address(value);
    }

    public static async Task<string> ResolveServerIpAddressAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetPublicIpAddressAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return GetLocalServerIpAddress();
        }
    }

    public static bool IsUsableIPv4Address(IPAddress address)
    {
        return address.AddressFamily == AddressFamily.InterNetwork
            && !IPAddress.IsLoopback(address);
    }

    public static bool IsUsableIPv6Address(IPAddress address)
    {
        return address.AddressFamily == AddressFamily.InterNetworkV6
            && !IPAddress.IsLoopback(address)
            && !address.IsIPv6LinkLocal
            && !address.IsIPv6Multicast;
    }

    public static bool IsPublicIPv4Address(IPAddress address)
    {
        if (!IsUsableIPv4Address(address))
        {
            return false;
        }

        var bytes = address.GetAddressBytes();

        return bytes[0] switch
        {
            0 or 10 or 127 or >= 224 => false,
            100 when bytes[1] is >= 64 and <= 127 => false,
            169 when bytes[1] == 254 => false,
            172 when bytes[1] is >= 16 and <= 31 => false,
            192 when bytes[1] == 168 => false,
            192 when bytes[1] == 0 && bytes[2] == 0 => false,
            192 when bytes[1] == 0 && bytes[2] == 2 => false,
            198 when bytes[1] is 18 or 19 => false,
            198 when bytes[1] == 51 && bytes[2] == 100 => false,
            203 when bytes[1] == 0 && bytes[2] == 113 => false,
            _ => true
        };
    }

    public static string NormalizeUsableIPv4Address(string value)
    {
        var normalized = value.Trim();
        if (!IPAddress.TryParse(normalized, out var address) || !IsUsableIPv4Address(address))
        {
            throw new InvalidOperationException($"'{value}' is not a usable IPv4 address.");
        }

        return address.ToString();
    }

    public static string NormalizePublicIPv4Address(string value)
    {
        var normalized = value.Trim();
        if (!IPAddress.TryParse(normalized, out var address) || !IsPublicIPv4Address(address))
        {
            throw new InvalidOperationException($"'{value}' is not a public IPv4 address.");
        }

        return address.ToString();
    }

    private static IEnumerable<IPAddress> GetUsableServerAddresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(item => item.OperationalStatus == OperationalStatus.Up)
            .Where(item => item.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(item => item.GetIPProperties().UnicastAddresses)
            .Select(item => item.Address)
            .Where(address => !IPAddress.IsLoopback(address));
    }
}
