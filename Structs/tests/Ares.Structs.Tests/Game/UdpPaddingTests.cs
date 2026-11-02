using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Network;

namespace Ares.Structs.Tests.Game;

public sealed class UdpPaddingTests
{
    [Theory]
    [MemberData(nameof(UdpPaddingVectors.Padded), MemberType = typeof(UdpPaddingVectors))]
    public void Padding_matches_the_reference(string datagram, string padded, string removed)
    {
        Assert.Equal(padded, Convert.ToHexStringLower(UdpPadding.Add(Convert.FromHexString(datagram))));
        Assert.Equal(removed, Convert.ToHexStringLower(UdpPadding.Remove(Convert.FromHexString(padded))));
    }

    [Theory]
    [MemberData(nameof(UdpPaddingVectors.Kept), MemberType = typeof(UdpPaddingVectors))]
    public void Unrecognized_trailers_are_kept(string datagram) =>
        Assert.Equal(datagram, Convert.ToHexStringLower(UdpPadding.Remove(Convert.FromHexString(datagram))));

    [Fact]
    public void Sizes_outside_the_range_are_rejected()
    {
        Assert.Throws<InvalidPacketException>(() => UdpPadding.Add(new byte[UdpPadding.MaximumLength + 1]));
        Assert.Throws<InvalidPacketException>(() => UdpPadding.Remove(new byte[UdpPadding.MinimumLength - 1]));
        Assert.Throws<InvalidPacketException>(() => UdpPadding.Remove(new byte[UdpPadding.MaximumLength + 1]));
    }
}
