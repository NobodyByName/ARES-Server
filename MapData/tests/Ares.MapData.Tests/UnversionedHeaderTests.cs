using Ares.MapData.Packages;

namespace Ares.MapData.Tests;

public sealed class UnversionedHeaderTests
{
    [Fact]
    public void A_single_fragment_lists_consecutive_stored_members()
    {
        byte[] data = [.. Fragment(skip: 2, count: 3, last: true), 0xAA];

        UnversionedHeader header = UnversionedHeader.Read(data, 0);

        Assert.Equal([new UnversionedMember(2, true), new UnversionedMember(3, true), new UnversionedMember(4, true)], header.Members);
        Assert.Equal(2, header.ValuesOffset);
        Assert.True(header.Contains(3));
        Assert.False(header.Contains(5));
        Assert.True(header.IsNonZero(4));
    }

    [Fact]
    public void The_zero_mask_marks_members_stored_as_zero()
    {
        byte[] data =
        [
            0xFF,
            .. Fragment(skip: 0, count: 2, zeroes: true),
            .. Fragment(skip: 3, count: 1, last: true),
            0b01,
        ];

        UnversionedHeader header = UnversionedHeader.Read(data, 1);

        Assert.Equal([new UnversionedMember(0, false), new UnversionedMember(1, true), new UnversionedMember(5, true)], header.Members);
        Assert.Equal(6, header.ValuesOffset);
        Assert.True(header.Contains(0));
        Assert.False(header.IsNonZero(0));
        Assert.Equal(2, header.NonZeroCount);
    }

    [Theory]
    [InlineData(8, 1)]
    [InlineData(9, 2)]
    [InlineData(16, 2)]
    [InlineData(17, 4)]
    [InlineData(33, 8)]
    public void The_zero_mask_size_follows_the_number_of_maskable_members(int count, int maskSize)
    {
        byte[] data = [.. Fragment(skip: 0, count: count, zeroes: true, last: true), .. new byte[maskSize]];

        UnversionedHeader header = UnversionedHeader.Read(data, 0);

        Assert.Equal(2 + maskSize, header.ValuesOffset);
        Assert.Equal(count, header.NonZeroCount);
    }

    [Fact]
    public void A_truncated_header_or_a_member_beyond_the_limit_is_rejected()
    {
        Assert.Throws<InvalidDataException>(() => UnversionedHeader.Read(Fragment(skip: 0, count: 1), 0));
        Assert.Throws<InvalidDataException>(() => UnversionedHeader.Read(Fragment(skip: 0, count: 9, zeroes: true, last: true), 0));
        Assert.Throws<InvalidDataException>(() => UnversionedHeader.Read(Fragment(skip: 5, count: 1, last: true), 0, memberLimit: 5));
    }

    [Fact]
    public void Strict_reading_rejects_what_a_real_header_never_holds()
    {
        byte[] emptyFragment = [.. Fragment(skip: 3, count: 0), .. Fragment(skip: 0, count: 1, last: true)];
        byte[] strayMaskBit = [.. Fragment(skip: 0, count: 2, zeroes: true, last: true), 0b100];
        byte[] tooManyFragments = [.. Enumerable.Range(0, 17).SelectMany(_ => Fragment(skip: 0, count: 1)), .. Fragment(skip: 0, count: 1, last: true)];

        Assert.NotNull(UnversionedHeader.Read(emptyFragment, 0));
        Assert.Null(UnversionedHeader.TryReadStrict(emptyFragment, 0, 80));
        Assert.NotNull(UnversionedHeader.Read(strayMaskBit, 0));
        Assert.Null(UnversionedHeader.TryReadStrict(strayMaskBit, 0, 80));
        Assert.Null(UnversionedHeader.TryReadStrict(tooManyFragments, 0, 80));
        Assert.Null(UnversionedHeader.TryReadStrict(Fragment(skip: 4, count: 1, last: true), 0, 4));
        Assert.Null(UnversionedHeader.TryReadStrict([0x00], 0, 80));
        Assert.NotNull(UnversionedHeader.TryReadStrict(Fragment(skip: 3, count: 1, last: true), 0, 4));
    }

    private static byte[] Fragment(int skip, int count, bool zeroes = false, bool last = false)
    {
        int value = skip | (zeroes ? 0x80 : 0) | (last ? 0x100 : 0) | (count << 9);
        return [(byte)value, (byte)(value >> 8)];
    }
}
