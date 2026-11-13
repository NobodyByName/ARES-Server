using System.Buffers.Binary;
using System.Numerics;

namespace Ares.MapData.Archives;

/// <summary>
/// The pak index cipher: a 128-bit block cipher keyed by the pak key, applied on top of AES-256 ECB. An encrypted index
/// is AES-encrypted first, then enciphered block by block.
/// </summary>
internal static class PakIndexCipher
{
    /// <summary>Bytes in a block.</summary>
    private const int BlockSize = 16;

    private const int Rounds = 34;
    private const int KeyWords = 4;

    /// <summary>Deciphers and AES-decrypts an encrypted index.</summary>
    /// <exception cref="InvalidDataException">The data is not a whole number of blocks.</exception>
    public static byte[] Decrypt(ReadOnlySpan<byte> data, byte[] key)
    {
        byte[] buffer = Blocks(data);
        ulong[] roundKeys = ExpandKey(key);
        for (int at = 0; at < buffer.Length; at += BlockSize)
        {
            ulong right = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(at));
            ulong left = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(at + sizeof(ulong)));
            for (int round = Rounds - 1; round >= 0; round--)
            {
                right = BitOperations.RotateRight(right ^ left, 3);
                left = BitOperations.RotateLeft((left ^ roundKeys[round]) - right, 8);
            }

            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(at), right);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(at + sizeof(ulong)), left);
        }

        return AesEcb.Decrypt(buffer, key);
    }

    /// <summary>AES-encrypts and enciphers an index; the inverse of <see cref="Decrypt"/>.</summary>
    /// <exception cref="InvalidDataException">The data is not a whole number of blocks.</exception>
    public static byte[] Encrypt(ReadOnlySpan<byte> data, byte[] key)
    {
        byte[] buffer = AesEcb.Encrypt(Blocks(data), key);
        ulong[] roundKeys = ExpandKey(key);
        for (int at = 0; at < buffer.Length; at += BlockSize)
        {
            ulong right = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(at));
            ulong left = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(at + sizeof(ulong)));
            for (int round = 0; round < Rounds; round++)
            {
                left = (BitOperations.RotateRight(left, 8) + right) ^ roundKeys[round];
                right = BitOperations.RotateLeft(right, 3) ^ left;
            }

            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(at), right);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(at + sizeof(ulong)), left);
        }

        return buffer;
    }

    private static byte[] Blocks(ReadOnlySpan<byte> data)
    {
        return data.Length % BlockSize == 0
            ? [.. data]
            : throw new InvalidDataException($"Encrypted pak index of {data.Length} bytes is not a whole number of blocks");
    }

    private static ulong[] ExpandKey(byte[] key)
    {
        ulong[] roundKeys = new ulong[Rounds];
        ulong[] schedule = new ulong[Rounds + KeyWords - 2];
        roundKeys[0] = BinaryPrimitives.ReadUInt64LittleEndian(key);
        for (int i = 1; i < KeyWords; i++)
        {
            schedule[i - 1] = BinaryPrimitives.ReadUInt64LittleEndian(key.AsSpan(i * sizeof(ulong)));
        }

        for (int i = 0; i < Rounds - 1; i++)
        {
            schedule[i + KeyWords - 1] = (roundKeys[i] + BitOperations.RotateRight(schedule[i], 8)) ^ (ulong)i;
            roundKeys[i + 1] = BitOperations.RotateLeft(roundKeys[i], 3) ^ schedule[i + KeyWords - 1];
        }

        return roundKeys;
    }
}
