using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Ares.MapData.Packages;

/// <summary>
/// CityHash64 (version 1.1), which the engine uses for package ids and script object ids.
/// </summary>
internal static class CityHash
{
    private const ulong K0 = 0xc3a5c85c97cb3127;
    private const ulong K1 = 0xb492b66fbe98f273;
    private const ulong K2 = 0x9ae16a3b2f90404f;
    private const ulong KMul = 0x9ddfea08eb382d69;

    /// <summary>CityHash64 of the lower-cased UTF-16 code units of <paramref name="text"/>.</summary>
    public static ulong HashLowerUtf16(string text)
    {
        return Hash64(Encoding.Unicode.GetBytes(text.ToLowerInvariant()));
    }

    /// <summary>CityHash64 of <paramref name="data"/>.</summary>
    public static ulong Hash64(ReadOnlySpan<byte> data)
    {
        int length = data.Length;
        switch (length)
        {
            case <= 16:
                return HashLength0To16(data);
            case <= 32:
                return HashLength17To32(data);
            case <= 64:
                return HashLength33To64(data);
        }

        ulong x = Fetch64(data, length - 40);
        ulong y = Fetch64(data, length - 16) + Fetch64(data, length - 56);
        ulong z = Hash16(Fetch64(data, length - 48) + (ulong)length, Fetch64(data, length - 24));
        (ulong First, ulong Second) v = WeakHash32(data, length - 64, (ulong)length, z);
        (ulong First, ulong Second) w = WeakHash32(data, length - 32, y + K1, x);
        x = (x * K1) + Fetch64(data, 0);
        int remaining = (length - 1) & ~63;
        int at = 0;
        do
        {
            x = BitOperations.RotateRight(x + y + v.First + Fetch64(data, at + 8), 37) * K1;
            y = BitOperations.RotateRight(y + v.Second + Fetch64(data, at + 48), 42) * K1;
            x ^= w.Second;
            y += v.First + Fetch64(data, at + 40);
            z = BitOperations.RotateRight(z + w.First, 33) * K1;
            v = WeakHash32(data, at, v.Second * K1, x + w.First);
            w = WeakHash32(data, at + 32, z + w.Second, y + Fetch64(data, at + 16));
            (z, x) = (x, z);
            at += 64;
            remaining -= 64;
        }
        while (remaining != 0);

        return Hash16(Hash16(v.First, w.First) + (ShiftMix(y) * K1) + z, Hash16(v.Second, w.Second) + x);
    }

    private static ulong HashLength0To16(ReadOnlySpan<byte> data)
    {
        int length = data.Length;
        switch (length)
        {
            case >= 8:
            {
                ulong mul = K2 + ((ulong)length * 2);
                ulong a = Fetch64(data, 0) + K2;
                ulong b = Fetch64(data, length - 8);
                ulong c = (BitOperations.RotateRight(b, 37) * mul) + a;
                ulong d = (BitOperations.RotateRight(a, 25) + b) * mul;
                return Hash16(c, d, mul);
            }
            case >= 4:
            {
                ulong mul = K2 + ((ulong)length * 2);
                ulong a = Fetch32(data, 0);
                return Hash16((ulong)length + (a << 3), Fetch32(data, length - 4), mul);
            }
            case 0:
                return K2;
            default:
            {
                uint y = data[0] + ((uint)data[length >> 1] << 8);
                uint z = (uint)length + ((uint)data[length - 1] << 2);
                return ShiftMix((y * K2) ^ (z * K0)) * K2;
            }
        }
    }

    private static ulong HashLength17To32(ReadOnlySpan<byte> data)
    {
        int length = data.Length;
        ulong mul = K2 + ((ulong)length * 2);
        ulong a = Fetch64(data, 0) * K1;
        ulong b = Fetch64(data, 8);
        ulong c = Fetch64(data, length - 8) * mul;
        ulong d = Fetch64(data, length - 16) * K2;
        return Hash16(BitOperations.RotateRight(a + b, 43) + BitOperations.RotateRight(c, 30) + d,
            a + BitOperations.RotateRight(b + K2, 18) + c, mul);
    }

    private static ulong HashLength33To64(ReadOnlySpan<byte> data)
    {
        int length = data.Length;
        ulong mul = K2 + ((ulong)length * 2);
        ulong a = Fetch64(data, 0) * K2;
        ulong b = Fetch64(data, 8);
        ulong c = Fetch64(data, length - 24);
        ulong d = Fetch64(data, length - 32);
        ulong e = Fetch64(data, 16) * K2;
        ulong f = Fetch64(data, 24) * 9;
        ulong g = Fetch64(data, length - 8);
        ulong h = Fetch64(data, length - 16) * mul;
        ulong u = BitOperations.RotateRight(a + g, 43) + ((BitOperations.RotateRight(b, 30) + c) * 9);
        ulong v = ((a + g) ^ d) + f + 1;
        ulong w = BinaryPrimitives.ReverseEndianness((u + v) * mul) + h;
        ulong x = BitOperations.RotateRight(e + f, 42) + c;
        ulong y = (BinaryPrimitives.ReverseEndianness((v + w) * mul) + g) * mul;
        ulong z = e + f + c;
        a = BinaryPrimitives.ReverseEndianness(((x + z) * mul) + y) + b;
        b = ShiftMix(((z + a) * mul) + d + h) * mul;
        return b + x;
    }

    private static (ulong First, ulong Second) WeakHash32(ReadOnlySpan<byte> data, int at, ulong a, ulong b)
    {
        ulong w = Fetch64(data, at);
        ulong x = Fetch64(data, at + 8);
        ulong y = Fetch64(data, at + 16);
        ulong z = Fetch64(data, at + 24);
        a += w;
        b = BitOperations.RotateRight(b + a + z, 21);
        ulong c = a;
        a += x + y;
        b += BitOperations.RotateRight(a, 44);
        return (a + z, b + c);
    }

    private static ulong Hash16(ulong u, ulong v, ulong mul = KMul)
    {
        ulong a = (u ^ v) * mul;
        a ^= a >> 47;
        ulong b = (v ^ a) * mul;
        b ^= b >> 47;
        return b * mul;
    }

    private static ulong ShiftMix(ulong value) => value ^ (value >> 47);

    private static ulong Fetch64(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadUInt64LittleEndian(data[at..]);

    private static uint Fetch32(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadUInt32LittleEndian(data[at..]);
}
