using System.Text;

namespace Ares.GameDataExtract.Tests;

/// <summary>Builds unversioned property blocks for synthetic exports.</summary>
internal static class PropertyBlocks
{
    private const int MaxSkip = 0x7F;
    private const int MaxCount = 0x7F;

    /// <summary>An unversioned header listing <paramref name="members"/> (ascending), each with a stored value.</summary>
    public static byte[] Header(params int[] members)
    {
        List<byte> bytes = [];
        if (members.Length == 0)
        {
            AddFragment(bytes, 0, 0, last: true);
            return [.. bytes];
        }

        int next = 0;
        int i = 0;
        while (i < members.Length)
        {
            int skip = members[i] - next;
            while (skip > MaxSkip)
            {
                AddFragment(bytes, MaxSkip, 0, last: false);
                skip -= MaxSkip;
            }

            int count = 1;
            while (i + count < members.Length && members[i + count] == members[i] + count && count < MaxCount)
            {
                count++;
            }

            AddFragment(bytes, skip, count, last: i + count == members.Length);
            next = members[i] + count;
            i += count;
        }

        return [.. bytes];
    }

    /// <summary>One header fragment: members skipped, members listed, whether they have zero-mask bits, whether it is the last.</summary>
    public static byte[] Fragment(int skip, int count, bool zeroes = false, bool last = false)
    {
        int value = skip | (zeroes ? 0x80 : 0) | (last ? 0x100 : 0) | (count << 9);
        return [(byte)value, (byte)(value >> 8)];
    }

    /// <summary>A length-prefixed UTF-8 string with its terminating zero.</summary>
    public static byte[] String(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return [.. BitConverter.GetBytes(bytes.Length + 1), .. bytes, 0];
    }

    /// <summary>Three doubles.</summary>
    public static byte[] Vector(double x, double y, double z) => [.. BitConverter.GetBytes(x), .. BitConverter.GetBytes(y), .. BitConverter.GetBytes(z)];

    /// <summary>A stored name: name-map index and number 0.</summary>
    public static byte[] Name(int index) => [.. BitConverter.GetBytes(index), .. BitConverter.GetBytes(0)];

    private static void AddFragment(List<byte> bytes, int skip, int count, bool last)
    {
        int value = skip | (last ? 0x100 : 0) | (count << 9);
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
    }
}
