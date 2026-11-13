using System.Text;
using Ares.MapData.Archives;

namespace Ares.MapData.Packages;

/// <summary>
/// A stored list of names: count, string byte count, hash version, one 64-bit hash per name, a two-byte header per
/// name (top bit set for UTF-16, then a 15-bit length) and the strings back to back.
/// </summary>
internal static class NameBatch
{
    /// <summary>Bit of a name header's first byte that marks a UTF-16 name.</summary>
    private const byte WideFlag = 0x80;

    private const int MaxNames = 4_000_000;

    /// <summary>Reads a name batch at the reader's position.</summary>
    public static string[] Read(ref ByteReader reader)
    {
        int count = reader.ReadCount(MaxNames);
        if (count == 0)
        {
            return [];
        }

        uint stringBytes = reader.ReadUInt32();
        reader.Skip(sizeof(ulong) + (count * sizeof(ulong)));
        ReadOnlySpan<byte> headers = reader.ReadBytes(count * 2);
        int start = reader.Position;
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
        {
            byte high = headers[i * 2];
            int length = ((high & ~WideFlag) << 8) | headers[(i * 2) + 1];
            names[i] = (high & WideFlag) != 0
                ? Encoding.Unicode.GetString(reader.ReadBytes(length * sizeof(char)))
                : Encoding.UTF8.GetString(reader.ReadBytes(length));
        }

        return reader.Position - start == stringBytes
            ? names
            : throw new InvalidDataException("Name batch string bytes differ from its header");
    }

    /// <summary>The display form of a stored name: the base name, plus <c>_</c> and <paramref name="number"/> − 1 when the number is not zero.</summary>
    public static string Display(string name, uint number)
    {
        return number == 0 ? name : $"{name}_{number - 1}";
    }
}
