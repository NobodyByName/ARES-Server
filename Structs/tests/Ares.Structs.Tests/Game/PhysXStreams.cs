using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Ares.Structs.Tests.Game;

/// <summary>Builds small PhysX cooked streams in the layouts the collision decoders read.</summary>
internal sealed class PhysXStreams
{
    private readonly List<byte> _bytes = [];

    public byte[] ToArray() => [.. _bytes];

    public PhysXStreams Byte(byte value)
    {
        _bytes.Add(value);
        return this;
    }

    public PhysXStreams Bytes(params byte[] values)
    {
        _bytes.AddRange(values);
        return this;
    }

    public PhysXStreams UInt16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        _bytes.AddRange(buffer);
        return this;
    }

    public PhysXStreams UInt32(uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        _bytes.AddRange(buffer);
        return this;
    }

    public PhysXStreams Int32(int value) => UInt32((uint)value);

    public PhysXStreams Single(params float[] values)
    {
        foreach (float value in values)
        {
            UInt32(BitConverter.SingleToUInt32Bits(value));
        }

        return this;
    }

    public PhysXStreams Vector(Vector3 value) => Single(value.X, value.Y, value.Z);

    public PhysXStreams Header(string stream, string chunk, uint version)
    {
        _bytes.AddRange(Encoding.ASCII.GetBytes(stream));
        _bytes.Add(1);
        _bytes.AddRange(Encoding.ASCII.GetBytes(chunk));
        return UInt32(version);
    }

    /// <summary>The eight corners of a box from (0, 0, 0) to (1, 2, 3).</summary>
    public static Vector3[] BoxCorners =>
    [
        new(0, 0, 0), new(1, 0, 0), new(0, 2, 0), new(1, 2, 0),
        new(0, 0, 3), new(1, 0, 3), new(0, 2, 3), new(1, 2, 3),
    ];

    /// <summary>Faces of <see cref="BoxCorners"/>: outward normal, distance, four corners.</summary>
    public static (Vector3 Normal, float Distance, byte[] Corners)[] BoxFaces =>
    [
        (new Vector3(-1, 0, 0), 0, [0, 4, 6, 2]),
        (new Vector3(1, 0, 0), -1, [1, 3, 7, 5]),
        (new Vector3(0, -1, 0), 0, [0, 1, 5, 4]),
        (new Vector3(0, 1, 0), -2, [2, 6, 7, 3]),
        (new Vector3(0, 0, -1), 0, [0, 2, 3, 1]),
        (new Vector3(0, 0, 1), -3, [4, 5, 7, 6]),
    ];

    /// <summary>A convex mesh stream of the box (its first <paramref name="faceCount"/> faces), optionally with GPU edge data and a large-convex block.</summary>
    public PhysXStreams ConvexBox(bool gpuEdges = false, bool largeConvex = false, int faceCount = 6)
    {
        const uint EdgeCount = 12;
        (Vector3 Normal, float Distance, byte[] Corners)[] faces = BoxFaces[..faceCount];
        Header("NXS", "CVXM", 13).UInt32(0);
        Header("ICE", "CLHL", 9).UInt32(8).UInt32(EdgeCount | (gpuEdges ? 0x8000u : 0)).UInt32((uint)faceCount).UInt32((uint)(faceCount * 4));
        foreach (Vector3 corner in BoxCorners)
        {
            Vector(corner);
        }

        ushort first = 0;
        foreach ((Vector3 normal, float distance, byte[] corners) in faces)
        {
            Vector(normal).Single(distance).UInt16(first).Byte((byte)corners.Length).Byte(corners.Min());
            first += (ushort)corners.Length;
        }

        foreach ((Vector3 _, float _, byte[] corners) in faces)
        {
            Bytes(corners);
        }

        Bytes(new byte[EdgeCount * 2]).Bytes(new byte[8 * 3]);
        if (gpuEdges)
        {
            Bytes(new byte[EdgeCount * 4]);
        }

        Single(0).Single(0, 0, 0, 1, 2, 3).Single(new float[13]);
        if (largeConvex)
        {
            Single(1);
            Header("ICE", "SUPM", 0).Header("ICE", "GAUS", 0).UInt32(2).UInt32(3).Bytes(new byte[6]);
            Header("ICE", "VALE", 2).UInt32(8).UInt32(5).UInt32(300);
            for (int i = 0; i < 8; i++)
            {
                UInt16(3);
            }

            Bytes(new byte[5]);
        }
        else
        {
            Single(-1);
        }

        return Single(0.5f, 0.5f, 1, 1.5f);
    }

    /// <summary>A two-triangle quad mesh stream with the given index width (1, 2 or 4 bytes).</summary>
    public PhysXStreams TriangleQuad(int indexWidth, bool materials = true, bool remap = true, bool adjacencies = false, bool edgeFlags = true, bool quantized = true)
    {
        uint flags = (materials ? 1u : 0) | (remap ? 2u : 0) | (adjacencies ? 16u : 0) | indexWidth switch
        {
            1 => 4u,
            2 => 8u,
            _ => 0u,
        };
        Header("NXS", "MESH", 15).UInt32(1).UInt32(flags).UInt32(4).UInt32(2);
        Vector(new Vector3(0, 0, 0)).Vector(new Vector3(100, 0, 0)).Vector(new Vector3(0, 100, 0)).Vector(new Vector3(100, 100, 0));
        foreach (uint index in new uint[] { 0, 1, 2, 2, 1, 3 })
        {
            _ = indexWidth switch
            {
                1 => Byte((byte)index),
                2 => UInt16((ushort)index),
                _ => UInt32(index),
            };
        }

        if (materials)
        {
            UInt16(0).UInt16(5);
        }

        if (remap)
        {
            UInt32(1).Byte(1).Byte(0);
        }

        if (adjacencies)
        {
            Bytes(new byte[2 * 12]);
        }

        Bytes([.. "BV4 "u8]).UInt32(3).Single(50, 50, 0, 70).UInt32(2).Single(new float[6]).UInt32(quantized ? 1u : 0).UInt32(1);
        Bytes(new byte[quantized ? 16 : 28]);
        Single(0).Single(0, 0, 0, 100, 100, 0);
        return edgeFlags ? UInt32(2).Bytes(7, 7) : UInt32(0);
    }

    /// <summary>A height-field stream of 3 rows × 2 columns; sample i has height i − 2 and materials (i, 127 − i).</summary>
    public PhysXStreams HeightField3By2()
    {
        Header("NXS", "HFHF", 1).UInt32(3).UInt32(2).Single(1, 0, 2, -1, 0).UInt16(1).UInt32(1);
        Single(0, -2, 0, 2, 3, 1).UInt32(4).UInt32(6).Single(-2, 3);
        for (int i = 0; i < 6; i++)
        {
            UInt16((ushort)(short)(i - 2)).Byte((byte)(i == 1 ? 0x80 | 127 : i)).Byte((byte)(127 - i));
        }

        return this;
    }
}
