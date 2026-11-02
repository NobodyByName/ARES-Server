using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>Opening an actor channel for an actor that already exists in the map.</summary>
/// <param name="Guid">The actor's static id.</param>
public sealed record StaticActorReference(ulong Guid)
{
    /// <summary>Writes the reference.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        if (Guid < 3 || !NetGuid.IsStatic(Guid))
        {
            throw new InvalidPacketException("Static actor requires an exported static id");
        }

        NetGuid.Write(writer, Guid);
    }

    /// <summary>Reads a reference that fills the whole reader.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The reference.</returns>
    public static StaticActorReference Read(BitReader reader)
    {
        StaticActorReference result = new(NetGuid.Read(reader));
        result.Write(new BitWriter());
        return reader.Remaining == 0 ? result : throw new InvalidPacketException("Trailing static actor data");
    }
}

/// <summary>
/// Opening an actor channel for a newly spawned actor: its id, archetype, level, and location.
/// Rotation, scale and velocity are sent as defaults (zero rotation, unit scale, no velocity).
/// </summary>
/// <param name="Guid">The actor's dynamic id.</param>
/// <param name="Archetype">Static id of the archetype (class default object).</param>
/// <param name="Level">Static id of the level, or 0 for the persistent level.</param>
/// <param name="Location">Spawn location; zero is sent as "no location".</param>
public sealed record ActorSpawn(ulong Guid, ulong Archetype, ulong Level, Vector3D Location)
{
    /// <summary>Writes the spawn.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (Guid < 2 || NetGuid.IsStatic(Guid) || Archetype < 3 || !NetGuid.IsStatic(Archetype) || Level == 1
            || !double.IsFinite(Location.X) || !double.IsFinite(Location.Y) || !double.IsFinite(Location.Z))
        {
            throw new InvalidPacketException("Invalid actor spawn");
        }

        NetGuid.Write(writer, Guid);
        NetGuid.Write(writer, Archetype);
        NetGuid.Write(writer, Level);
        bool hasLocation = Location != default;
        writer.WriteBit(hasLocation);
        if (hasLocation)
        {
            writer.WriteBit(false);
            foreach (double value in (ReadOnlySpan<double>)[Location.X, Location.Y, Location.Z])
            {
                writer.WriteBits(BitConverter.DoubleToUInt64Bits(value), 64);
            }
        }

        writer.WriteBits(0, 3);
    }

    /// <summary>Reads a spawn that fills the whole reader.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The spawn.</returns>
    public static ActorSpawn Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ulong actor = NetGuid.Read(reader);
        ulong archetype = NetGuid.Read(reader);
        ulong level = NetGuid.Read(reader);
        Vector3D location = default;
        if (reader.ReadBit())
        {
            if (reader.ReadBit())
            {
                throw new InvalidPacketException("Quantized spawn locations are not supported");
            }

            location = new Vector3D(ReadDouble(reader), ReadDouble(reader), ReadDouble(reader));
        }

        if (reader.ReadBits(3) != 0 || reader.Remaining != 0)
        {
            throw new InvalidPacketException("Unsupported initial transform or trailing spawn data");
        }

        ActorSpawn result = new(actor, archetype, level, location);
        result.Write(new BitWriter());
        return result;
    }

    private static double ReadDouble(BitReader reader) => BitConverter.UInt64BitsToDouble(reader.ReadBits(64));
}

/// <summary>
/// Fields a player controller's channel carries after its spawn: they bind the controller to the
/// client's local player.
/// </summary>
/// <param name="PlayerIndex">Local player index.</param>
/// <param name="HandshakeId">Handshake id.</param>
/// <param name="ConnectionIdentifier">Connection identifier.</param>
public sealed record PlayerControllerOpen(byte PlayerIndex, uint HandshakeId, int ConnectionIdentifier)
{
    /// <summary>Writes the fields.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(PlayerIndex);
        writer.WriteBits(HandshakeId, 32);
        writer.WriteBits((uint)ConnectionIdentifier, 32);
    }

    /// <summary>Reads the fields.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The fields.</returns>
    public static PlayerControllerOpen Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return new PlayerControllerOpen(reader.ReadByte(), (uint)reader.ReadBits(32), (int)(uint)reader.ReadBits(32));
    }
}
