using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>
/// A reference to a component of an actor or to a style (<c>FComponentOrStyle</c>). Only the component form is
/// supported: two flag bits (set, then clear) followed by the component's id.
/// </summary>
/// <param name="Component">Object id of the scene component.</param>
public sealed record ComponentOrStyle(ulong Component)
{
    /// <summary>Writes the reference.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (Component < 2)
        {
            throw new InvalidPacketException("Component reference requires a component id");
        }

        writer.WriteBit(true);
        writer.WriteBit(false);
        NetGuid.Write(writer, Component);
    }

    /// <summary>Reads a reference.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The reference.</returns>
    public static ComponentOrStyle Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (!reader.ReadBit() || reader.ReadBit())
        {
            throw new InvalidPacketException("Only component references are supported");
        }

        ComponentOrStyle result = new(NetGuid.Read(reader));
        return result.Component >= 2 ? result : throw new InvalidPacketException("Component reference requires a component id");
    }
}
