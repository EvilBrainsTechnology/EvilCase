using System.Net;
using EvilBrains.EvilCase.Host;

namespace EvilBrains.EvilCase.Tests.Hosting;

public class RateLimitPartitionKeyTests
{
    [Test]
    public void TwoAddressesInOneIpv6PrefixShareAPartition()
    {
        var first = RateLimitPartitionKey.ForAddress(IPAddress.Parse("2001:db8:1234:5678::1"));
        var second = RateLimitPartitionKey.ForAddress(IPAddress.Parse("2001:db8:1234:5678:ffff:ffff:ffff:ffff"));

        Assert.That(first, Is.EqualTo(second), "an IPv6 caller is keyed on its /64 prefix");
    }

    [Test]
    public void TwoAddressesInDifferentIpv6PrefixesDoNotShareAPartition()
    {
        var first = RateLimitPartitionKey.ForAddress(IPAddress.Parse("2001:db8:1234:5678::1"));
        var second = RateLimitPartitionKey.ForAddress(IPAddress.Parse("2001:db8:1234:5679::1"));

        Assert.That(first, Is.Not.EqualTo(second), "an IPv6 caller is keyed on its /64 prefix, not a wider block");
    }

    [Test]
    public void Ipv4AddressesKeepTheirOwnPartitions()
    {
        var first = RateLimitPartitionKey.ForAddress(IPAddress.Parse("192.0.2.1"));
        var second = RateLimitPartitionKey.ForAddress(IPAddress.Parse("192.0.2.2"));

        Assert.That(first, Is.Not.EqualTo(second), "an IPv4 caller is keyed on its full address");
    }

    [Test]
    public void Ipv4MappedAddressBehavesLikeItsIpv4Form()
    {
        var mapped = RateLimitPartitionKey.ForAddress(IPAddress.Parse("::ffff:192.0.2.1"));
        var plain = RateLimitPartitionKey.ForAddress(IPAddress.Parse("192.0.2.1"));

        Assert.That(mapped, Is.EqualTo(plain), "an IPv4-mapped IPv6 address is an IPv4 caller wearing an IPv6 shape");
    }

    [Test]
    public void Ipv4AddressNeverCollidesWithAnIpv6Prefix()
    {
        var ipv4 = RateLimitPartitionKey.ForAddress(IPAddress.Parse("192.0.2.1"));
        var ipv6 = RateLimitPartitionKey.ForAddress(IPAddress.Parse("c000:201::1"));

        Assert.That(ipv4, Is.Not.EqualTo(ipv6), "the key must stay distinguishable between address families");
    }
}
