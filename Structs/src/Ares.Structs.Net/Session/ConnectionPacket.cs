using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Session;

/// <summary>
/// The connection part of a packet: the notification header, optional packet info (jitter clock and
/// frame time), then bunches until the end of the data.
/// </summary>
/// <param name="Notification">Notification header.</param>
/// <param name="Bunches">Bunches, at most <see cref="MaxBunches"/>.</param>
public sealed record ConnectionPacket(NotificationHeader Notification, IReadOnlyList<ChannelBunch> Bunches)
{
    /// <summary>Largest number of bunches in a packet.</summary>
    public const int MaxBunches = 64;

    /// <summary>Jitter clock (10 bits); null when the packet carries no packet info.</summary>
    public int? JitterClock { get; init; }

    /// <summary>Client packets: whether the client asks for the server frame time.</summary>
    public bool FrameTimeRequested { get; init; }

    /// <summary>Server packets: server frame time (8 bits).</summary>
    public int? ServerFrameTime { get; init; }

    /// <summary>Throws unless <paramref name="maxPacketBytes"/> is a usable packet size.</summary>
    /// <param name="maxPacketBytes">Largest packet in bytes (64 to 65507).</param>
    /// <returns>The size.</returns>
    public static int CheckMaxPacketBytes(int maxPacketBytes) =>
        maxPacketBytes is >= 64 and <= 65507 ? maxPacketBytes : throw new ArgumentOutOfRangeException(nameof(maxPacketBytes));

    /// <summary>Writes the packet.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="fromServer">Whether the server sends it.</param>
    /// <param name="maxPacketBytes">Largest packet in bytes.</param>
    public void Write(BitWriter writer, bool fromServer, int maxPacketBytes)
    {
        ArgumentNullException.ThrowIfNull(writer);
        CheckMaxPacketBytes(maxPacketBytes);
        if ((fromServer && FrameTimeRequested) || (!fromServer && ServerFrameTime is not null))
        {
            throw new InvalidPacketException("Frame time field in the wrong direction");
        }

        if (Bunches.Count > MaxBunches)
        {
            throw new InvalidPacketException("Too many bunches");
        }

        long start = writer.Length;
        Notification.Write(writer);
        writer.WriteBit(JitterClock is not null);
        if (JitterClock is { } jitter)
        {
            writer.WriteBits((uint)jitter, 10);
            writer.WriteBit(fromServer ? ServerFrameTime is not null : FrameTimeRequested);
            if (ServerFrameTime is { } frameTime)
            {
                writer.WriteBits((uint)frameTime, 8);
            }
        }
        else if (FrameTimeRequested || ServerFrameTime is not null)
        {
            throw new InvalidPacketException("Frame time requires packet info");
        }

        foreach (ChannelBunch bunch in Bunches)
        {
            bunch.Write(writer, maxPacketBytes);
        }

        if (writer.Length - start > maxPacketBytes * 8L)
        {
            throw new InvalidPacketException("Packet too long");
        }
    }

    /// <summary>Reads a packet; bunches are read until the end of the data.</summary>
    /// <param name="reader">Source, limited to the connection payload.</param>
    /// <param name="fromServer">Whether the server sent it.</param>
    /// <param name="maxPacketBytes">Largest packet in bytes.</param>
    /// <returns>The packet.</returns>
    public static ConnectionPacket Read(BitReader reader, bool fromServer, int maxPacketBytes)
    {
        ArgumentNullException.ThrowIfNull(reader);
        CheckMaxPacketBytes(maxPacketBytes);
        if (reader.Remaining > maxPacketBytes * 8L)
        {
            throw new InvalidPacketException("Packet too long");
        }

        NotificationHeader header = NotificationHeader.Read(reader);
        int? jitter = reader.ReadBit() ? (int)reader.ReadBits(10) : null;
        bool requested = false;
        int? frameTime = null;
        if (jitter is not null)
        {
            bool frameBit = reader.ReadBit();
            switch (fromServer)
            {
                case true when frameBit:
                    frameTime = (int)reader.ReadBits(8);
                    break;
                case false:
                    requested = frameBit;
                    break;
            }
        }

        List<ChannelBunch> bunches = [];
        while (reader.Remaining > 0)
        {
            if (bunches.Count >= MaxBunches)
            {
                throw new InvalidPacketException("Too many bunches");
            }

            bunches.Add(ChannelBunch.Read(reader, maxPacketBytes));
        }

        return new ConnectionPacket(header, bunches) { JitterClock = jitter, FrameTimeRequested = requested, ServerFrameTime = frameTime };
    }
}
