using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Game.Network;

/// <summary>
/// Padding the client adds to short datagrams it sends, outside all packet handlers: fill bytes, a
/// marker and the padding length as the last byte. Server datagrams are never padded.
/// </summary>
public static class UdpPadding
{
    /// <summary>Datagrams shorter than this are padded to it.</summary>
    public const int MinimumLength = 22;

    /// <summary>Largest datagram.</summary>
    public const int MaximumLength = 1172;

    /// <summary>Fill byte before the marker.</summary>
    public const byte Fill = 0xCD;

    /// <summary>Marker before the length byte.</summary>
    public static ReadOnlySpan<byte> Marker => [0x6A, 0xB3, 0x2D, 0xF1, 0x97];

    private static int TrailerLength => Marker.Length + 1;

    /// <summary>Pads a datagram the way the client does.</summary>
    /// <param name="datagram">The datagram.</param>
    /// <returns>The padded datagram.</returns>
    public static byte[] Add(ReadOnlySpan<byte> datagram)
    {
        switch (datagram.Length)
        {
            case > MaximumLength:
                throw new InvalidPacketException("Datagram too long");
            case >= MinimumLength:
                return [.. datagram];
        }

        int count = Math.Max(TrailerLength, MinimumLength - datagram.Length);
        byte[] result = new byte[datagram.Length + count];
        datagram.CopyTo(result);
        result.AsSpan(datagram.Length, count - TrailerLength).Fill(Fill);
        Marker.CopyTo(result.AsSpan(result.Length - TrailerLength));
        result[^1] = (byte)count;
        return result;
    }

    /// <summary>
    /// Removes a well-formed trailer from a received datagram of 22 to 27 bytes. Anything else is
    /// returned unchanged; the packet decoders then validate it.
    /// </summary>
    /// <param name="datagram">The datagram.</param>
    /// <returns>The datagram without padding.</returns>
    public static byte[] Remove(ReadOnlySpan<byte> datagram)
    {
        if (datagram.Length is < MinimumLength or > MaximumLength)
        {
            throw new InvalidPacketException("Datagram size outside the accepted range");
        }

        if (datagram.Length > MinimumLength + Marker.Length || !datagram[^TrailerLength..^1].SequenceEqual(Marker))
        {
            return [.. datagram];
        }

        int count = datagram[^1];
        if (count < TrailerLength || count > Math.Min(datagram.Length - 1, MinimumLength)
            || datagram[^count..^TrailerLength].ContainsAnyExcept(Fill))
        {
            return [.. datagram];
        }

        return [.. datagram[..^count]];
    }
}
