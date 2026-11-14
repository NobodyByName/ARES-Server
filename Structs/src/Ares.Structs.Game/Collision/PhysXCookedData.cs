using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ares.Structs.Game.Collision;

/// <summary>
/// Decoders for the PhysX 4.1 cooked shapes the game stores: convex meshes (<c>CVXM</c> 13), triangle meshes
/// (<c>MESH</c> 15 with a BV4 midphase), height fields (<c>HFHF</c> 1) and the body payload that groups convex and
/// triangle meshes. Every decoder consumes its input exactly and throws <see cref="InvalidDataException"/> on
/// truncated, trailing or malformed data and on layouts it does not support.
/// </summary>
public static class PhysXCookedData
{
    /// <summary>Convex mesh stream version.</summary>
    public const uint ConvexMeshVersion = 13;

    /// <summary>Convex hull chunk version inside a convex mesh.</summary>
    public const uint ConvexHullVersion = 9;

    /// <summary>Triangle mesh stream version.</summary>
    public const uint TriangleMeshVersion = 15;

    /// <summary>Height-field stream version.</summary>
    public const uint HeightFieldVersion = 1;

    /// <summary>Height-field sample format: signed 16-bit height with two material bytes.</summary>
    public const uint HeightFieldSampleFormat = 1;

    private const uint LargeConvexVersion = 0;
    private const uint GaussMapVersion = 0;
    private const uint ValenciesVersion = 2;
    private const uint Bv4Version = 3;
    private const uint Bv4MidphaseId = 1;
    private const int MaximumHullVertices = 255;
    private const int MinimumHullFaces = 4;
    private const int HullPolygonSize = 20;
    private const int QuantizedBv4NodeSize = 16;
    private const int UnquantizedBv4NodeSize = 28;

    private const uint MeshMaterials = 1;
    private const uint MeshFaceRemap = 2;
    private const uint Mesh8BitIndices = 4;
    private const uint Mesh16BitIndices = 8;
    private const uint MeshAdjacencies = 16;
    private const uint MeshGpuData = 32;

    private static ReadOnlySpan<byte> Nxs => "NXS"u8;
    private static ReadOnlySpan<byte> Ice => "ICE"u8;

    /// <summary>
    /// Decodes a body payload: a little-endian flag byte (1), the counts of convex, mirrored convex and triangle
    /// meshes (int32 each), each convex entry as a validity byte followed by the convex mesh when valid, each triangle
    /// mesh, then three empty UV-info arrays (index buffer, vertex positions, vertex UVs).
    /// </summary>
    public static CookedBodyGeometry ReadBodyGeometry(ReadOnlySpan<byte> payload)
    {
        PhysXReader reader = new(payload);
        if (reader.ReadByte() != 1)
        {
            throw new InvalidDataException("Body payload is not little-endian.");
        }

        int convexCount = reader.ReadSignedCount(1);
        int mirroredCount = reader.ReadSignedCount(1);
        int triangleMeshCount = reader.ReadSignedCount(1);
        ImmutableArray<ConvexHull?> convex = ReadConvexEntries(ref reader, convexCount);
        ImmutableArray<ConvexHull?> mirrored = ReadConvexEntries(ref reader, mirroredCount);
        ImmutableArray<TriangleMesh>.Builder meshes = ImmutableArray.CreateBuilder<TriangleMesh>(triangleMeshCount);
        for (int i = 0; i < triangleMeshCount; i++)
        {
            meshes.Add(ReadTriangleMesh(ref reader));
        }

        for (int i = 0; i < 3; i++)
        {
            if (reader.ReadInt32() != 0)
            {
                throw new InvalidDataException("Body payloads with UV info are not supported.");
            }
        }

        reader.EnsureEnd();
        return new CookedBodyGeometry(convex, mirrored, meshes.MoveToImmutable());
    }

    /// <summary>Decodes one PhysX convex mesh stream.</summary>
    public static ConvexHull ReadConvexMesh(ReadOnlySpan<byte> data)
    {
        PhysXReader reader = new(data);
        ConvexHull hull = ReadConvexMesh(ref reader);
        reader.EnsureEnd();
        return hull;
    }

    /// <summary>Decodes one PhysX triangle mesh stream (BV4 midphase).</summary>
    public static TriangleMesh ReadTriangleMesh(ReadOnlySpan<byte> data)
    {
        PhysXReader reader = new(data);
        TriangleMesh mesh = ReadTriangleMesh(ref reader);
        reader.EnsureEnd();
        return mesh;
    }

    /// <summary>Decodes one PhysX height-field stream.</summary>
    public static HeightField ReadHeightField(ReadOnlySpan<byte> data)
    {
        PhysXReader reader = new(data);
        reader.ReadHeader(Nxs, "HFHF"u8, HeightFieldVersion);
        uint rows = reader.ReadUInt32();
        uint columns = reader.ReadUInt32();
        reader.ReadSingle(); // last row index as a float
        reader.ReadSingle(); // last column index as a float
        reader.ReadSingle(); // column count as a float
        float thickness = reader.ReadSingle();
        float convexEdgeThreshold = reader.ReadSingle();
        ushort flags = reader.ReadUInt16();
        uint format = reader.ReadUInt32();
        BoundingBox bounds = reader.ReadBounds();
        uint sampleStride = reader.ReadUInt32();
        uint sampleCount = reader.ReadUInt32();
        float minimumHeight = reader.ReadSingle();
        float maximumHeight = reader.ReadSingle();
        if (format != HeightFieldSampleFormat || sampleStride != 4)
        {
            throw new InvalidDataException($"Unsupported height-field sample format {format} (stride {sampleStride}).");
        }

        if (rows < 2 || columns < 2 || (ulong)rows * columns != sampleCount)
        {
            throw new InvalidDataException($"Height field of {rows} × {columns} does not hold {sampleCount} samples.");
        }

        int count = reader.CheckCount(sampleCount, 4);
        HeightFieldSample[] samples = new HeightFieldSample[count];
        for (int i = 0; i < count; i++)
        {
            short height = reader.ReadInt16();
            byte material0 = reader.ReadByte();
            byte material1 = reader.ReadByte();
            samples[i] = new HeightFieldSample(height, material0, material1);
        }

        reader.EnsureEnd();
        return new HeightField(
            (int)rows,
            (int)columns,
            thickness,
            convexEdgeThreshold,
            flags,
            bounds,
            minimumHeight,
            maximumHeight,
            ImmutableCollectionsMarshal.AsImmutableArray(samples));
    }

    private static ImmutableArray<ConvexHull?> ReadConvexEntries(ref PhysXReader reader, int count)
    {
        ImmutableArray<ConvexHull?>.Builder hulls = ImmutableArray.CreateBuilder<ConvexHull?>(count);
        for (int i = 0; i < count; i++)
        {
            byte valid = reader.ReadByte();
            hulls.Add(valid switch
            {
                0 => null,
                1 => ReadConvexMesh(ref reader),
                _ => throw new InvalidDataException($"Invalid convex validity flag {valid}."),
            });
        }

        return hulls.MoveToImmutable();
    }

    private static ConvexHull ReadConvexMesh(ref PhysXReader reader)
    {
        reader.ReadHeader(Nxs, "CVXM"u8, ConvexMeshVersion);
        reader.ReadUInt32(); // serialization flags, always zero
        reader.ReadHeader(Ice, "CLHL"u8, ConvexHullVersion);
        int vertexCount = reader.ReadCount(12);
        uint edgeData = reader.ReadUInt32();
        int polygonCount = reader.ReadCount(HullPolygonSize);
        int indexCount = reader.ReadCount(1);
        bool hasGpuEdges = (edgeData & 0x8000) != 0;
        uint edgeCount = edgeData & 0x7FFF;
        if (vertexCount > MaximumHullVertices || polygonCount < MinimumHullFaces || polygonCount > MaximumHullVertices || edgeData > 0xFFFF)
        {
            throw new InvalidDataException($"Convex hull with {vertexCount} vertices and {polygonCount} faces is out of range.");
        }

        Vector3[] vertices = ReadVertices(ref reader, vertexCount);
        HullPolygon[] polygons = new HullPolygon[polygonCount];
        bool[] used = new bool[indexCount];
        for (int i = 0; i < polygonCount; i++)
        {
            Vector3 normal = reader.ReadVector3();
            float distance = reader.ReadSingle();
            ushort first = reader.ReadUInt16();
            byte count = reader.ReadByte();
            reader.ReadByte(); // index of the face's lowest vertex
            if (count < 3 || first + count > indexCount)
            {
                throw new InvalidDataException("Convex hull face indices out of range.");
            }

            for (int index = first; index < first + count; index++)
            {
                if (used[index])
                {
                    throw new InvalidDataException("Convex hull faces share vertex indices.");
                }

                used[index] = true;
            }

            polygons[i] = new HullPolygon(new Plane(normal, distance), first, count);
        }

        byte[] indices = [.. reader.ReadBytes(indexCount)];
        if (indices.Any(index => index >= vertexCount))
        {
            throw new InvalidDataException("Convex hull vertex index out of range.");
        }

        reader.Skip(edgeCount * 2); // faces adjacent to each edge
        reader.Skip(vertexCount * 3L); // faces adjacent to each vertex
        if (hasGpuEdges)
        {
            reader.Skip(edgeCount * 4); // edge vertex pairs
        }

        reader.ReadSingle(); // geometric epsilon
        BoundingBox bounds = reader.ReadBounds();
        reader.Skip(13 * 4); // mass, inertia tensor, centre of mass
        float largeConvexFlag = reader.ReadSingle();
        switch (largeConvexFlag)
        {
            case 1.0f:
                SkipLargeConvexData(ref reader);
                break;
            case -1.0f:
                break;
            default:
                throw new InvalidDataException($"Invalid large-convex flag {largeConvexFlag}.");
        }

        reader.Skip(4 * 4); // internal sphere radius and box extents
        return new ConvexHull(
            ImmutableCollectionsMarshal.AsImmutableArray(vertices),
            ImmutableCollectionsMarshal.AsImmutableArray(polygons),
            ImmutableCollectionsMarshal.AsImmutableArray(indices),
            bounds);
    }

    /// <summary>Skips the support-vertex map PhysX stores for hulls with many vertices.</summary>
    private static void SkipLargeConvexData(ref PhysXReader reader)
    {
        reader.ReadHeader(Ice, "SUPM"u8, LargeConvexVersion);
        reader.ReadHeader(Ice, "GAUS"u8, GaussMapVersion);
        reader.ReadUInt32(); // subdivision
        int sampleCount = reader.ReadCount(2);
        reader.Skip(sampleCount * 2L);
        reader.ReadHeader(Ice, "VALE"u8, ValenciesVersion);
        uint valencyCount = reader.ReadUInt32();
        uint adjacentCount = reader.ReadUInt32();
        uint maximumValency = reader.ReadUInt32();
        if (maximumValency > ushort.MaxValue)
        {
            throw new InvalidDataException($"Invalid maximum valency {maximumValency}.");
        }

        reader.SkipIndices(maximumValency, reader.CheckCount(valencyCount, 1));
        reader.Skip(reader.CheckCount(adjacentCount, 1));
    }

    // Compiled fully optimized at once: in Release builds the .NET 10.0.3 runtime's on-stack replacement of this method
    // returns an invalid material index array.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static TriangleMesh ReadTriangleMesh(ref PhysXReader reader)
    {
        reader.ReadHeader(Nxs, "MESH"u8, TriangleMeshVersion);
        uint midphase = reader.ReadUInt32();
        uint flags = reader.ReadUInt32();
        if (midphase != Bv4MidphaseId)
        {
            throw new InvalidDataException($"Unsupported triangle mesh midphase {midphase}.");
        }

        if ((flags & ~(MeshGpuData * 2 - 1)) != 0 || (flags & MeshGpuData) != 0 ||
            (flags & (Mesh8BitIndices | Mesh16BitIndices)) == (Mesh8BitIndices | Mesh16BitIndices))
        {
            throw new InvalidDataException($"Unsupported triangle mesh flags 0x{flags:X}.");
        }

        int vertexCount = reader.ReadCount(12);
        int indexWidth = (flags & Mesh8BitIndices) != 0 ? 1 : (flags & Mesh16BitIndices) != 0 ? 2 : 4;
        int triangleCount = reader.ReadCount(3 * indexWidth);
        Vector3[] vertices = ReadVertices(ref reader, vertexCount);
        int[] indices = new int[triangleCount * 3];
        for (int i = 0; i < indices.Length; i++)
        {
            uint index = indexWidth switch
            {
                1 => reader.ReadByte(),
                2 => reader.ReadUInt16(),
                _ => reader.ReadUInt32(),
            };
            if (index >= (uint)vertexCount)
            {
                throw new InvalidDataException("Triangle vertex index out of range.");
            }

            indices[i] = (int)index;
        }

        ushort[] materials = [];
        if ((flags & MeshMaterials) != 0)
        {
            materials = new ushort[reader.CheckCount((uint)triangleCount, 2)];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = reader.ReadUInt16();
            }
        }

        if ((flags & MeshFaceRemap) != 0)
        {
            reader.SkipIndices(reader.ReadUInt32(), triangleCount); // original triangle order
        }

        if ((flags & MeshAdjacencies) != 0)
        {
            reader.Skip(triangleCount * 12L);
        }

        SkipBv4Tree(ref reader);
        reader.ReadSingle(); // geometric epsilon
        BoundingBox bounds = reader.ReadBounds();
        uint edgeFlagCount = reader.ReadUInt32();
        if (edgeFlagCount != 0 && edgeFlagCount != (uint)triangleCount)
        {
            throw new InvalidDataException($"Edge flags for {edgeFlagCount} of {triangleCount} triangles.");
        }

        reader.Skip(edgeFlagCount); // per-triangle active edge flags
        return new TriangleMesh(
            ImmutableCollectionsMarshal.AsImmutableArray(vertices),
            ImmutableCollectionsMarshal.AsImmutableArray(indices),
            ImmutableCollectionsMarshal.AsImmutableArray(materials),
            bounds);
    }

    /// <summary>Skips the BV4 bounding-volume tree a triangle mesh stores for its midphase.</summary>
    private static void SkipBv4Tree(ref PhysXReader reader)
    {
        if (!reader.ReadBytes(4).SequenceEqual("BV4 "u8))
        {
            throw new InvalidDataException("Expected a BV4 tree.");
        }

        uint version = reader.ReadUInt32();
        if (version != Bv4Version)
        {
            throw new InvalidDataException($"Unsupported BV4 version {version}.");
        }

        reader.Skip(4 * 4); // local bounds: centre and extents magnitude
        reader.ReadUInt32(); // tree initialisation data
        reader.Skip(6 * 4); // centre or minimum, extents or maximum coefficients
        uint quantized = reader.ReadUInt32();
        if (quantized > 1)
        {
            throw new InvalidDataException($"Invalid BV4 quantization flag {quantized}.");
        }

        int nodeSize = quantized == 1 ? QuantizedBv4NodeSize : UnquantizedBv4NodeSize;
        reader.Skip((long)reader.ReadCount(nodeSize) * nodeSize);
    }

    private static Vector3[] ReadVertices(ref PhysXReader reader, int count)
    {
        Vector3[] vertices = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            vertices[i] = reader.ReadVector3();
        }

        return vertices;
    }
}
