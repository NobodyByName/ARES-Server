using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Session;

/// <summary>
/// A connection (non-handshake) packet before encryption: packet ids, the plaintext marker of the
/// encryption component, an optional direct-replication sideband payload, the server time (server
/// packets only), the connection payload, and the termination bits.
/// </summary>
/// <param name="SessionId">Session id (2 bits).</param>
/// <param name="ClientId">Client id (3 bits).</param>
/// <param name="Payload">Connection payload bits.</param>
/// <param name="PayloadBits">Number of payload bits.</param>
public sealed record ConnectionEnvelope(int SessionId, int ClientId, byte[] Payload, long PayloadBits)
{
    /// <summary>Largest connection payload in bits.</summary>
    public const int MaxPayloadBits = 8192;

    /// <summary>Largest packet in bytes.</summary>
    public const int MaxPacketBytes = 1050;

    /// <summary>Largest direct-replication payload in bytes.</summary>
    public const int MaxDirectBytes = 768;

    /// <summary>Number of direct-replication stream kinds.</summary>
    public const int DirectKinds = 5;

    /// <summary>Server time in milliseconds (server packets only).</summary>
    public uint? ServerTimeMs { get; init; }

    /// <summary>Direct-replication stream kind, if the packet carries a sideband payload.</summary>
    public int? DirectKind { get; init; }

    /// <summary>Direct-replication payload (1 to 768 bytes when <see cref="DirectKind"/> is set).</summary>
    public byte[] DirectPayload { get; init; } = [];

    /// <summary>Encodes the packet.</summary>
    /// <param name="fromServer">Whether the server sends it.</param>
    /// <returns>The packet bytes.</returns>
    public byte[] Encode(bool fromServer)
    {
        if (PayloadBits is < 0 or > MaxPayloadBits)
        {
            throw new InvalidPacketException("Connection payload too long");
        }

        BitWriter writer = new();
        writer.WriteBits((uint)SessionId, 2);
        writer.WriteBits((uint)ClientId, 3);
        writer.WriteBit(false);
        writer.WriteByte(0);
        long directStart = writer.Length;
        if (DirectKind is not { } kind)
        {
            if (DirectPayload.Length != 0)
            {
                throw new InvalidPacketException("Direct payload requires a stream kind");
            }

            writer.WriteBit(false);
        }
        else
        {
            if (DirectPayload.Length is < 1 or > MaxDirectBytes || kind is < 0 or >= DirectKinds)
            {
                throw new InvalidPacketException("Invalid direct-replication payload");
            }

            writer.WriteBit(true);
            writer.WriteBounded((uint)kind, DirectKinds);
            writer.WriteBounded((uint)DirectPayload.Length, MaxDirectBytes + 1);
        }

        writer.WriteBits(0, (int)(-(writer.Length - directStart) & 7));
        writer.WriteBytes(DirectPayload);
        bool directOnly = DirectKind is not null && PayloadBits == 0;
        if (fromServer && !directOnly)
        {
            writer.WriteBit(false);
            writer.WriteBit(ServerTimeMs is not null);
            if (ServerTimeMs is { } time)
            {
                writer.WriteBounded(time, uint.MaxValue);
            }
        }
        else if (ServerTimeMs is not null)
        {
            throw new InvalidPacketException("Only server connection packets carry server time");
        }

        writer.WriteBits(Payload, PayloadBits);
        if (!directOnly)
        {
            writer.WriteBit(true);
        }

        writer.WriteBit(true);
        byte[] packet = writer.ToArray();
        return packet.Length <= MaxPacketBytes ? packet : throw new InvalidPacketException("Packet too long");
    }

    /// <summary>Decodes a packet.</summary>
    /// <param name="packet">The packet bytes.</param>
    /// <param name="fromServer">Whether the server sent it.</param>
    /// <returns>The envelope.</returns>
    public static ConnectionEnvelope Decode(byte[] packet, bool fromServer)
    {
        ArgumentNullException.ThrowIfNull(packet);
        if (packet.Length is < 4 or > MaxPacketBytes)
        {
            throw new InvalidPacketException("Invalid connection packet size");
        }

        BitReader reader = new(packet, PacketTermination.DataBits(packet));
        int session = (int)reader.ReadBits(2);
        int client = (int)reader.ReadBits(3);
        if (reader.ReadBit())
        {
            throw new InvalidPacketException("Expected a connection packet");
        }

        if (reader.ReadByte() != 0)
        {
            throw new InvalidPacketException("Encrypted packets need negotiated decryption");
        }

        long directStart = reader.Position;
        int? kind = null;
        int directSize = 0;
        if (reader.ReadBit())
        {
            kind = (int)reader.ReadBounded(DirectKinds);
            directSize = (int)reader.ReadBounded(MaxDirectBytes + 1);
            if (directSize == 0)
            {
                throw new InvalidPacketException("Empty direct-replication payload");
            }
        }

        if (reader.ReadBits((int)(-(reader.Position - directStart) & 7)) != 0)
        {
            throw new InvalidPacketException("Invalid direct-replication header padding");
        }

        byte[] direct = reader.ReadBytes(directSize);
        if (reader.Remaining == 0 && kind is not null)
        {
            // Sideband-only packets have no connection part or connection termination.
            return new ConnectionEnvelope(session, client, [], 0) { DirectKind = kind, DirectPayload = direct };
        }

        uint? serverTime = null;
        if (fromServer)
        {
            if (reader.ReadBit())
            {
                throw new InvalidPacketException("Optional server-time metadata is not supported");
            }

            if (reader.ReadBit())
            {
                serverTime = reader.ReadBounded(uint.MaxValue);
            }
        }

        long payloadBits = reader.Remaining - 1;
        if (payloadBits is < 0 or > MaxPayloadBits)
        {
            throw new InvalidPacketException("Missing termination or payload too long");
        }

        byte[] payload = reader.ReadBitArray(payloadBits);
        if (!reader.ReadBit())
        {
            throw new InvalidPacketException("Missing connection termination");
        }

        return new ConnectionEnvelope(session, client, payload, payloadBits)
        {
            ServerTimeMs = serverTime,
            DirectKind = kind,
            DirectPayload = direct,
        };
    }
}
