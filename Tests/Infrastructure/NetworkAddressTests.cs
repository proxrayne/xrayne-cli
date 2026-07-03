using System.Net;
using System.Net.Sockets;
using Infrastructure.Utilities;

namespace Test.Infrastructure;

public sealed class NetworkAddressTests
{
    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("::1", false)]
    public void IsUsableIPv4Address_ReturnsExpectedResult(
        string value,
        bool expected)
    {
        var address = IPAddress.Parse(value);

        Assert.Equal(expected, NetworkAddress.IsUsableIPv4Address(address));
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("1.1.1.1", true)]
    [InlineData("10.0.0.1", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("169.254.1.1", false)]
    [InlineData("192.0.2.1", false)]
    [InlineData("198.51.100.1", false)]
    [InlineData("203.0.113.1", false)]
    [InlineData("224.0.0.1", false)]
    public void IsPublicIPv4Address_ReturnsExpectedResult(
        string value,
        bool expected)
    {
        var address = IPAddress.Parse(value);

        Assert.Equal(expected, NetworkAddress.IsPublicIPv4Address(address));
    }

    [Fact]
    public void IsPublicIPv4Address_ReturnsFalseForIPv6Address()
    {
        var address = IPAddress.Parse("2001:4860:4860::8888");

        Assert.False(NetworkAddress.IsPublicIPv4Address(address));
    }

    [Fact]
    public void NormalizeUsableIPv4Address_TrimsAndNormalizesValidIPv4()
    {
        var result = NetworkAddress.NormalizeUsableIPv4Address(" 8.8.8.8 ");

        Assert.Equal("8.8.8.8", result);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("not-an-ip")]
    public void NormalizeUsableIPv4Address_RejectsInvalidOrLoopbackAddress(string value)
    {
        Assert.Throws<InvalidOperationException>(() => NetworkAddress.NormalizeUsableIPv4Address(value));
    }

    [Fact]
    public void NormalizePublicIPv4Address_TrimsAndNormalizesPublicIPv4()
    {
        var result = NetworkAddress.NormalizePublicIPv4Address(" 1.1.1.1 ");

        Assert.Equal("1.1.1.1", result);
    }

    [Theory]
    [InlineData("10.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("203.0.113.1")]
    [InlineData("2001:4860:4860::8888")]
    [InlineData("not-an-ip")]
    public void NormalizePublicIPv4Address_RejectsNonPublicIPv4Address(string value)
    {
        Assert.Throws<InvalidOperationException>(() => NetworkAddress.NormalizePublicIPv4Address(value));
    }

    [Fact]
    public void IsUsableIPv4Address_ReturnsFalseForIPv6Address()
    {
        var address = IPAddress.Parse("2001:4860:4860::8888");

        Assert.Equal(AddressFamily.InterNetworkV6, address.AddressFamily);
        Assert.False(NetworkAddress.IsUsableIPv4Address(address));
    }

    [Theory]
    [InlineData("2001:4860:4860::8888", true)]
    [InlineData("fe80::1", false)]
    [InlineData("ff02::1", false)]
    [InlineData("::1", false)]
    [InlineData("8.8.8.8", false)]
    public void IsUsableIPv6Address_ReturnsExpectedResult(
        string value,
        bool expected)
    {
        var address = IPAddress.Parse(value);

        Assert.Equal(expected, NetworkAddress.IsUsableIPv6Address(address));
    }

    [Fact]
    public void GetServerIpAddresses_ReturnsAddressCollections()
    {
        var result = NetworkAddress.GetServerIpAddresses();

        Assert.NotNull(result.IPv4Addresses);
        Assert.NotNull(result.IPv6Addresses);
    }
}
