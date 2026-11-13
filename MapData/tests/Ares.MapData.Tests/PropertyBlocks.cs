namespace Ares.MapData.Tests;

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
