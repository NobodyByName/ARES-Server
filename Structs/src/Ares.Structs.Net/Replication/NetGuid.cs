using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>
/// Network object ids. Odd ids are static (loadable by path), even ids are dynamic (spawned), 0 is none.
/// On the wire: 7 value bits per byte, lowest first, low bit set when more follow (at most 10 bytes).
/// </summary>
public static class NetGuid
{
    /// <summary>Whether <paramref name="guid"/> is a static (path) id.</summary>
    /// <param name="guid">The id.</param>
    /// <returns>True for odd ids.</returns>
    public static bool IsStatic(ulong guid) => (guid & 1) != 0;

    /// <summary>Writes an id.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="guid">The id.</param>
    public static void Write(BitWriter writer, ulong guid)
    {
        ArgumentNullException.ThrowIfNull(writer);
        while (true)
        {
            ulong rest = guid >> 7;
            writer.WriteByte((byte)(((guid & 0x7F) << 1) | (rest != 0 ? 1UL : 0UL)));
            if (rest == 0)
            {
                return;
            }

            guid = rest;
        }
    }

    /// <summary>Reads an id.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The id.</returns>
    public static ulong Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ulong result = 0;
        for (int index = 0; index < 10; index++)
        {
            byte unit = reader.ReadByte();
            if (index == 9 && unit > 2)
            {
                throw new InvalidPacketException("Network id above 64 bits");
            }

            result |= (ulong)(unit >> 1) << (7 * index);
            if ((unit & 1) == 0)
            {
                return result;
            }
        }

        throw new InvalidPacketException("Unterminated network id");
    }
}
