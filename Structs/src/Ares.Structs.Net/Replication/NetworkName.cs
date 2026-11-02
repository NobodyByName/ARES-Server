using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>
/// A name as sent on the wire: a built-in index for well-known names, otherwise the text and a number
/// (the number distinguishes <c>Name_1</c>-style instances; 0 means none).
/// </summary>
/// <param name="Text">Name text.</param>
/// <param name="Number">Name number.</param>
public sealed record NetworkName(string Text, uint Number)
{
    /// <summary>The empty name.</summary>
    public static NetworkName None { get; } = new("None", 0);

    private static readonly Dictionary<string, uint> s_builtIn = new(StringComparer.Ordinal)
    {
        ["None"] = 0,
        ["Actor"] = 102,
        ["Control"] = 255,
    };

    /// <summary>Writes the name.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (Text.Length == 0)
        {
            throw new InvalidPacketException("Empty name");
        }

        bool builtIn = Number == 0 && s_builtIn.ContainsKey(Text);
        writer.WriteBit(builtIn);
        if (builtIn)
        {
            writer.WritePacked(s_builtIn[Text]);
            return;
        }

        writer.WriteString(Text);
        writer.WriteBits(Number, 32);
    }

    /// <summary>Reads a name.</summary>
    /// <param name="reader">Source.</param>
    /// <param name="limit">Largest accepted text length, terminator included.</param>
    /// <returns>The name.</returns>
    public static NetworkName Read(BitReader reader, int limit = 4096)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (reader.ReadBit())
        {
            uint index = reader.ReadPacked();
            foreach ((string text, uint known) in s_builtIn)
            {
                if (known == index)
                {
                    return new NetworkName(text, 0);
                }
            }

            throw new InvalidPacketException("Unsupported built-in name");
        }

        string name = reader.ReadString(limit);
        return name.Length != 0 ? new NetworkName(name, (uint)reader.ReadBits(32)) : throw new InvalidPacketException("Empty name");
    }
}
