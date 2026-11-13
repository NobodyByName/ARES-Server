using System.Buffers.Binary;

namespace Ares.MapData.Packages;

/// <summary>A member listed by an unversioned property header: its chain index and whether a value is stored for it.</summary>
/// <param name="Index">Index in the class's (or struct's) property chain, derived class first.</param>
/// <param name="IsNonZero">
/// <see langword="true"/> when the member's value is stored after the header; <see langword="false"/> when the zero
/// mask marks it as zero (nothing stored).
/// </param>
internal readonly record struct UnversionedMember(int Index, bool IsNonZero);

/// <summary>
/// The header of an unversioned property block: fragments of two bytes (bits 0–6 members skipped, bit 7 the fragment
/// has zero members, bit 8 last fragment, bits 9–15 members listed), then a zero mask with one bit per member of the
/// fragments that have zero members (1, 2 or a multiple of 4 bytes). The listed members' values follow in chain order.
/// </summary>
internal sealed class UnversionedHeader
{
    /// <summary>The highest member count accepted when reading without a struct-specific limit.</summary>
    private const int DefaultMemberLimit = 4096;

    /// <summary>The most fragments a strict read accepts before its last one.</summary>
    private const int StrictMaxFragments = 16;

    private const int SkipMask = 0x7F;
    private const int HasZeroesFlag = 0x80;
    private const int IsLastFlag = 0x100;
    private const int ValueCountShift = 9;
    private const int MaxFragments = 1024;

    private readonly int[] _indices;
    private readonly UnversionedMember[] _members;

    private UnversionedHeader(UnversionedMember[] members, int valuesOffset)
    {
        _members = members;
        _indices = [.. members.Select(member => member.Index)];
        ValuesOffset = valuesOffset;
    }

    /// <summary>Listed members in ascending index order.</summary>
    public IReadOnlyList<UnversionedMember> Members => _members;

    /// <summary>Offset of the first stored value, right after the header and zero mask.</summary>
    public int ValuesOffset { get; }

    /// <summary>Number of members whose value is stored.</summary>
    public int NonZeroCount => _members.Count(member => member.IsNonZero);

    /// <summary>Whether the block lists member <paramref name="index"/> (stored or zero).</summary>
    public bool Contains(int index) => Array.BinarySearch(_indices, index) >= 0;

    /// <summary>Whether the block stores a value for member <paramref name="index"/>.</summary>
    public bool IsNonZero(int index)
    {
        int at = Array.BinarySearch(_indices, index);
        return at >= 0 && _members[at].IsNonZero;
    }

    /// <summary>Reads the header at <paramref name="offset"/>.</summary>
    /// <exception cref="InvalidDataException">The header runs past the data, has too many fragments or lists a member at or beyond <paramref name="memberLimit"/>.</exception>
    public static UnversionedHeader Read(ReadOnlySpan<byte> data, int offset, int memberLimit = DefaultMemberLimit)
    {
        return Parse(data, offset, memberLimit, strict: false)
            ?? throw new InvalidDataException($"Unversioned header at offset {offset} is malformed");
    }

    /// <summary>
    /// Reads the header at <paramref name="offset"/> with the checks used when searching for a block's start: no
    /// fragment before the last lists zero members, at most <see cref="StrictMaxFragments"/> fragments precede the last,
    /// the zero mask has no bits beyond its members, and every member is below <paramref name="memberLimit"/>.
    /// </summary>
    /// <returns>The header, or <see langword="null"/> when the bytes are not a valid header.</returns>
    public static UnversionedHeader? TryReadStrict(ReadOnlySpan<byte> data, int offset, int memberLimit)
    {
        return Parse(data, offset, memberLimit, strict: true);
    }

    private static UnversionedHeader? Parse(ReadOnlySpan<byte> data, int offset, int memberLimit, bool strict)
    {
        List<int> fragments = [];
        int zeroes = 0;
        int at = offset;
        while (true)
        {
            if (at < 0 || at + sizeof(ushort) > data.Length)
            {
                return null;
            }

            int fragment = BinaryPrimitives.ReadUInt16LittleEndian(data[at..]);
            at += sizeof(ushort);
            int count = fragment >> ValueCountShift;
            bool isLast = (fragment & IsLastFlag) != 0;
            if (strict && count == 0 && !isLast)
            {
                return null;
            }

            fragments.Add(fragment);
            if ((fragment & HasZeroesFlag) != 0)
            {
                zeroes += count;
            }

            if (isLast)
            {
                break;
            }

            if (fragments.Count > (strict ? StrictMaxFragments : MaxFragments))
            {
                return null;
            }
        }

        int maskSize = zeroes == 0 ? 0 : zeroes <= 8 ? 1 : zeroes <= 16 ? 2 : (zeroes + 31) / 32 * 4;
        if (at + maskSize > data.Length)
        {
            return null;
        }

        ReadOnlySpan<byte> mask = data.Slice(at, maskSize);
        if (strict && zeroes > 0)
        {
            for (int bit = zeroes; bit < maskSize * 8; bit++)
            {
                if (MaskBit(mask, bit))
                {
                    return null;
                }
            }
        }

        List<UnversionedMember> members = [];
        int index = 0;
        int maskBit = 0;
        foreach (int fragment in fragments)
        {
            index += fragment & SkipMask;
            bool hasZeroes = (fragment & HasZeroesFlag) != 0;
            for (int i = 0; i < fragment >> ValueCountShift; i++)
            {
                if (index >= memberLimit)
                {
                    return null;
                }

                members.Add(new UnversionedMember(index, !hasZeroes || !MaskBit(mask, maskBit)));
                maskBit += hasZeroes ? 1 : 0;
                index++;
            }
        }

        return new UnversionedHeader([.. members], at + maskSize);
    }

    private static bool MaskBit(ReadOnlySpan<byte> mask, int bit) => (mask[bit / 8] & (1 << (bit % 8))) != 0;
}
