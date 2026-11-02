using System.Buffers.Binary;
using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Session;

/// <summary>Stateless handshake packet types.</summary>
public enum HandshakeType : byte
{
    /// <summary>Client hello.</summary>
    Initial = 0,

    /// <summary>Server challenge with a cookie.</summary>
    Challenge = 1,

    /// <summary>Client response echoing the cookie.</summary>
    Response = 2,

    /// <summary>Server acknowledgement.</summary>
    Ack = 3,
}

/// <summary>
/// The fixed start of a stateless handshake packet (71 bits). Only handshake version 4 is supported
/// (minimum version 3 or 4).
/// </summary>
/// <param name="SessionId">Session id (2 bits).</param>
/// <param name="ClientId">Client id (3 bits).</param>
/// <param name="Restart">Restart handshake flag.</param>
/// <param name="MinimumVersion">Minimum supported handshake version.</param>
/// <param name="CurrentVersion">Handshake version in use.</param>
/// <param name="PacketType">Handshake packet type.</param>
/// <param name="SentCount">Number of times the sender sent this packet type.</param>
/// <param name="NetworkVersion">Network version checksum.</param>
public sealed record StatelessPrefix(
    int SessionId,
    int ClientId,
    bool Restart,
    byte MinimumVersion,
    byte CurrentVersion,
    byte PacketType,
    byte SentCount,
    uint NetworkVersion)
{
    /// <summary>Length of the prefix in bits.</summary>
    public const int BitLength = 71;

    /// <summary>Writes the prefix.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        CheckVersions(MinimumVersion, CurrentVersion);
        writer.WriteBits((uint)SessionId, 2);
        writer.WriteBits((uint)ClientId, 3);
        writer.WriteBit(true);
        writer.WriteBit(Restart);
        writer.WriteByte(MinimumVersion);
        writer.WriteByte(CurrentVersion);
        writer.WriteByte(PacketType);
        writer.WriteByte(SentCount);
        writer.WriteBits(NetworkVersion, 32);
    }

    /// <summary>Reads a prefix; the rest of the packet is not interpreted.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The prefix.</returns>
    public static StatelessPrefix Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        int session = (int)reader.ReadBits(2);
        int client = (int)reader.ReadBits(3);
        if (!reader.ReadBit())
        {
            throw new InvalidPacketException("Not a stateless handshake packet");
        }

        bool restart = reader.ReadBit();
        byte minimum = reader.ReadByte();
        byte current = reader.ReadByte();
        CheckVersions(minimum, current);
        return new StatelessPrefix(session, client, restart, minimum, current, reader.ReadByte(), reader.ReadByte(), (uint)reader.ReadBits(32));
    }

    private static void CheckVersions(byte minimum, byte current)
    {
        if (minimum is not (3 or 4) || current != 4)
        {
            throw new InvalidPacketException("Unsupported handshake version");
        }
    }
}

/// <summary>A normal (non-restart) version-4 stateless handshake packet.</summary>
/// <param name="Prefix">Fixed start.</param>
/// <param name="NetworkFeatures">Network feature flags.</param>
/// <param name="SecretId">Server secret slot (0 or 1).</param>
/// <param name="Timestamp">Server timestamp in seconds.</param>
/// <param name="Cookie">20-byte cookie.</param>
/// <param name="Padding">8 to 16 padding bytes.</param>
public sealed record HandshakePacket(
    StatelessPrefix Prefix,
    ushort NetworkFeatures,
    int SecretId,
    double Timestamp,
    byte[] Cookie,
    byte[] Padding)
{
    /// <summary>Cookie size in bytes.</summary>
    public const int CookieLength = 20;

    private const int FixedBits = StatelessPrefix.BitLength + 16 + 1 + 64 + (CookieLength * 8);

    /// <summary>Encodes the packet, termination bit included.</summary>
    /// <returns>The packet bytes.</returns>
    public byte[] Encode()
    {
        ArgumentNullException.ThrowIfNull(Prefix);
        if (Prefix.Restart || Prefix.PacketType > (byte)HandshakeType.Ack)
        {
            throw new InvalidPacketException("Unsupported handshake variant");
        }

        if (Cookie.Length != CookieLength || Padding.Length is < 8 or > 16 || SecretId is not (0 or 1))
        {
            throw new InvalidPacketException("Invalid cookie, padding or secret");
        }

        if (!double.IsFinite(Timestamp))
        {
            throw new InvalidPacketException("Non-finite handshake timestamp");
        }

        BitWriter writer = new();
        Prefix.Write(writer);
        writer.WriteBits(NetworkFeatures, 16);
        writer.WriteBits((uint)SecretId, 1);
        writer.WriteBits(BitConverter.DoubleToUInt64Bits(Timestamp), 64);
        writer.WriteBytes(Cookie);
        writer.WriteBytes(Padding);
        writer.WriteBit(true);
        return writer.ToArray();
    }

    /// <summary>Decodes a packet.</summary>
    /// <param name="packet">The packet bytes.</param>
    /// <returns>The handshake.</returns>
    public static HandshakePacket Decode(byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        if (packet.Length is < 48 or > 56)
        {
            throw new InvalidPacketException("Invalid handshake size");
        }

        long size = PacketTermination.DataBits(packet);
        if (size is < FixedBits + 64 or > FixedBits + 128 || (size - FixedBits) % 8 != 0)
        {
            throw new InvalidPacketException("Invalid handshake padding");
        }

        BitReader reader = new(packet, size);
        StatelessPrefix prefix = StatelessPrefix.Read(reader);
        if (prefix.Restart || prefix.PacketType > (byte)HandshakeType.Ack)
        {
            throw new InvalidPacketException("Unsupported handshake variant");
        }

        ushort features = (ushort)reader.ReadBits(16);
        int secret = (int)reader.ReadBits(1);
        double timestamp = BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReadUInt64LittleEndian(reader.ReadBytes(8)));
        if (!double.IsFinite(timestamp))
        {
            throw new InvalidPacketException("Non-finite handshake timestamp");
        }

        byte[] cookie = reader.ReadBytes(CookieLength);
        byte[] padding = reader.ReadBytes((int)(reader.Remaining / 8));
        return new HandshakePacket(prefix, features, secret, timestamp, cookie, padding);
    }
}
