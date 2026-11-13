using System.Buffers.Binary;
using System.Numerics;

namespace Ares.MapData.Archives;

/// <summary>
/// Key stream of the game's data-file wrapper. The compression function is BLAKE3's; each 64 KiB page has its
/// own key and its bytes are stored as plain byte plus key-stream byte (mod 256).
/// </summary>
internal static class TheiaCipher
{
    /// <summary>Words in a key, chaining value or seed half.</summary>
    public const int KeyWords = 8;

    private const int BlockWords = 16;
    private const int BlockBytes = 64;
    private const uint HashFlags = 11;
    private const uint PageKeyFlags = 17;
    private const uint StreamFlags = 26;

    private static readonly uint[] s_iv =
    [
        0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19,
    ];

    private static readonly byte[] s_permutation = [2, 6, 3, 10, 7, 0, 4, 13, 1, 11, 12, 5, 9, 14, 15, 8];

    /// <summary>Hash of exactly one 64-byte block: the first eight output words.</summary>
    public static uint[] Hash64(ReadOnlySpan<byte> input)
    {
        Span<uint> words = stackalloc uint[BlockWords];
        for (int i = 0; i < BlockWords; i++)
        {
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(input[(i * sizeof(uint))..]);
        }

        Span<uint> output = stackalloc uint[BlockWords];
        Compress(s_iv, words, 0, BlockBytes, HashFlags, output);
        return [.. output[..KeyWords]];
    }

    /// <summary>
    /// Decrypts <paramref name="data"/> in place. The bytes start <paramref name="pageOffset"/> bytes into a page whose
    /// key is <paramref name="pageKey"/>; <paramref name="containerKey"/> and <paramref name="master"/> are the same for every page of a file.
    /// </summary>
    public static void DecryptPage(Span<byte> data, uint pageOffset, ReadOnlySpan<uint> containerKey, ReadOnlySpan<uint> master, ReadOnlySpan<uint> pageKey)
    {
        Span<uint> stream = stackalloc uint[BlockWords];
        Span<byte> streamBytes = stackalloc byte[BlockBytes];
        Span<uint> state = stackalloc uint[BlockWords];
        PageState(containerKey, master, pageKey, state);
        int at = 0;
        while (at < data.Length)
        {
            uint position = pageOffset + (uint)at;
            int skip = (int)(position % BlockBytes);
            KeyStream(state, position / BlockBytes, stream, streamBytes);
            int count = Math.Min(BlockBytes - skip, data.Length - at);
            for (int i = 0; i < count; i++)
            {
                data[at + i] = (byte)(data[at + i] - streamBytes[skip + i]);
            }

            at += count;
        }
    }

    /// <summary>BLAKE3 compression of one block with chaining value <paramref name="chaining"/>.</summary>
    internal static void Compress(ReadOnlySpan<uint> chaining, ReadOnlySpan<uint> block, ulong counter, uint length, uint flags, Span<uint> output)
    {
        Span<uint> v = stackalloc uint[BlockWords];
        Span<uint> m = stackalloc uint[BlockWords];
        Span<uint> next = stackalloc uint[BlockWords];
        chaining[..KeyWords].CopyTo(v);
        s_iv.AsSpan(0, 4).CopyTo(v[8..]);
        block[..BlockWords].CopyTo(m);
        v[12] = (uint)counter;
        v[13] = (uint)(counter >> 32);
        v[14] = length;
        v[15] = flags;
        for (int round = 0; round < 7; round++)
        {
            Mix(v, 0, 4, 8, 12, m[0], m[1]);
            Mix(v, 1, 5, 9, 13, m[2], m[3]);
            Mix(v, 2, 6, 10, 14, m[4], m[5]);
            Mix(v, 3, 7, 11, 15, m[6], m[7]);
            Mix(v, 0, 5, 10, 15, m[8], m[9]);
            Mix(v, 1, 6, 11, 12, m[10], m[11]);
            Mix(v, 2, 7, 8, 13, m[12], m[13]);
            Mix(v, 3, 4, 9, 14, m[14], m[15]);
            for (int i = 0; i < BlockWords; i++)
            {
                next[i] = m[s_permutation[i]];
            }

            next.CopyTo(m);
        }

        for (int i = 0; i < KeyWords; i++)
        {
            output[i] = v[i] ^ v[i + 8];
            output[i + 8] = v[i + 8] ^ chaining[i];
        }
    }

    private static void PageState(ReadOnlySpan<uint> containerKey, ReadOnlySpan<uint> master, ReadOnlySpan<uint> pageKey, Span<uint> state)
    {
        Span<uint> combined = stackalloc uint[BlockWords];
        master[..KeyWords].CopyTo(combined);
        pageKey[..KeyWords].CopyTo(combined[KeyWords..]);
        Compress(containerKey, combined, 0, BlockBytes, PageKeyFlags, state);
    }

    private static void KeyStream(ReadOnlySpan<uint> state, uint block, Span<uint> stream, Span<byte> streamBytes)
    {
        Span<uint> zeros = stackalloc uint[BlockWords];
        zeros.Clear();
        Compress(state, zeros, block, 1, StreamFlags, stream);
        for (int i = 0; i < BlockWords; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(streamBytes[(i * sizeof(uint))..], stream[i]);
        }
    }

    private static void Mix(Span<uint> v, int a, int b, int c, int d, uint x, uint y)
    {
        v[a] += v[b] + x;
        v[d] = BitOperations.RotateRight(v[d] ^ v[a], 16);
        v[c] += v[d];
        v[b] = BitOperations.RotateRight(v[b] ^ v[c], 12);
        v[a] += v[b] + y;
        v[d] = BitOperations.RotateRight(v[d] ^ v[a], 8);
        v[c] += v[d];
        v[b] = BitOperations.RotateRight(v[b] ^ v[c], 7);
    }
}
