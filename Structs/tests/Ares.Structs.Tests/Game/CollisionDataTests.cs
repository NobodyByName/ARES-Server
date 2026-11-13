using System.Numerics;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.Structs.Tests.Game;

public sealed class CollisionDataTests
{
    private static readonly int[] s_quadIndices = [0, 1, 2, 2, 1, 3];

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convex_mesh_keeps_vertices_faces_and_bounds(bool gpuEdges, bool largeConvex)
    {
        ConvexHull hull = PhysXCookedData.ReadConvexMesh(new PhysXStreams().ConvexBox(gpuEdges, largeConvex).ToArray());

        Assert.Equal(PhysXStreams.BoxCorners, hull.Vertices);
        Assert.Equal(6, hull.Polygons.Length);
        Assert.Equal(new BoundingBox(Vector3.Zero, new Vector3(1, 2, 3)), hull.Bounds);
        HullPolygon top = hull.Polygons[5];
        Assert.Equal(new Plane(new Vector3(0, 0, 1), -3), top.Plane);
        Assert.Equal(new byte[] { 4, 5, 7, 6 }, hull.PolygonVertices(top).ToArray());
        foreach (HullPolygon polygon in hull.Polygons)
        {
            foreach (byte index in hull.PolygonVertices(polygon))
            {
                Assert.Equal(0, Plane.DotCoordinate(polygon.Plane, hull.Vertices[index]), 5);
            }
        }
    }

    [Fact]
    public void Convex_mesh_with_shared_face_indices_or_fewer_than_four_faces_is_rejected()
    {
        // Each face record is 20 bytes after the 140-byte head and the vertices; its first index is at +16.
        byte[] shared = new PhysXStreams().ConvexBox().ToArray();
        shared[140 + 20 + 16] = 0;

        Assert.Throws<InvalidDataException>(() => PhysXCookedData.ReadConvexMesh(shared));
        Assert.Throws<InvalidDataException>(() => PhysXCookedData.ReadConvexMesh(new PhysXStreams().ConvexBox(faceCount: 3).ToArray()));
        Assert.Equal(4, PhysXCookedData.ReadConvexMesh(new PhysXStreams().ConvexBox(faceCount: 4).ToArray()).Polygons.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Triangle_mesh_reads_each_index_width(int indexWidth)
    {
        TriangleMesh mesh = PhysXCookedData.ReadTriangleMesh(new PhysXStreams().TriangleQuad(indexWidth).ToArray());

        Assert.Equal(4, mesh.Vertices.Length);
        Assert.Equal(2, mesh.TriangleCount);
        Assert.Equal(s_quadIndices, mesh.Indices);
        Assert.Equal(new ushort[] { 0, 5 }, mesh.MaterialIndices);
        Assert.Equal(new Vector3(100, 100, 0), mesh.Bounds.Maximum);
    }

    [Fact]
    public void Triangle_mesh_optional_blocks_are_consumed()
    {
        byte[] bare = new PhysXStreams().TriangleQuad(2, materials: false, remap: false, edgeFlags: false, quantized: false).ToArray();
        byte[] full = new PhysXStreams().TriangleQuad(2, adjacencies: true).ToArray();

        Assert.True(PhysXCookedData.ReadTriangleMesh(bare).MaterialIndices.IsEmpty);
        Assert.Equal(2, PhysXCookedData.ReadTriangleMesh(full).TriangleCount);
    }

    [Fact]
    public void Body_payload_groups_convex_mirrored_and_triangle_meshes()
    {
        byte[] payload = BodyPayload();

        CookedBodyGeometry body = PhysXCookedData.ReadBodyGeometry(payload);

        Assert.Equal(2, body.ConvexHulls.Length);
        Assert.NotNull(body.ConvexHulls[0]);
        Assert.Null(body.ConvexHulls[1]);
        Assert.Single(body.MirroredConvexHulls);
        Assert.Equal(2, body.TriangleMeshes.Length);
        Assert.Equal(s_quadIndices, body.TriangleMeshes[1].Indices);
    }

    [Fact]
    public void Empty_body_payload_has_no_shapes()
    {
        CookedBodyGeometry body = PhysXCookedData.ReadBodyGeometry(new PhysXStreams().Byte(1).Int32(0).Int32(0).Int32(0).Int32(0).Int32(0).Int32(0).ToArray());

        Assert.True(body.ConvexHulls.IsEmpty && body.MirroredConvexHulls.IsEmpty && body.TriangleMeshes.IsEmpty);
    }

    [Fact]
    public void BodySetup_native_data_points_at_the_inline_payload()
    {
        byte[] payload = BodyPayload();
        byte[] cooked = new PhysXStreams().UInt32(0).Bytes(new byte[16]).UInt32(1).UInt32(1).UInt32(1).UInt32(7).UInt32(0).UInt32(3).Bytes(payload).ToArray();
        byte[] withObjectGuid = new PhysXStreams().UInt32(1).Bytes(new byte[32]).UInt32(1).UInt32(1).UInt32(1).UInt32(7).UInt32(0).UInt32(3).Bytes(payload).ToArray();
        byte[] noFormats = new PhysXStreams().UInt32(0).Bytes(new byte[16]).UInt32(1).UInt32(0).UInt32(0).ToArray();

        BodySetupCookedFormat format = Assert.NotNull(BodySetupNativeData.Read(cooked));
        Assert.Equal(7, format.FormatNameIndex);
        Assert.Equal(3, format.BulkDataIndex);
        Assert.Equal(payload, format.Payload.ToArray());
        Assert.Equal(payload, Assert.NotNull(BodySetupNativeData.Read(withObjectGuid)).Payload.ToArray());
        Assert.Null(BodySetupNativeData.Read(noFormats));
        Assert.Throws<InvalidDataException>(() => BodySetupNativeData.Read(new PhysXStreams().UInt32(0).Bytes(new byte[16]).UInt32(1).UInt32(1).UInt32(2).ToArray()));
        Assert.Throws<InvalidDataException>(() => BodySetupNativeData.Read(noFormats.AsMemory(..^1)));
        Assert.Throws<InvalidDataException>(() => BodySetupNativeData.Read(new PhysXStreams().Bytes(noFormats).Byte(0).ToArray()));
    }

    [Fact]
    public void Height_field_keeps_samples_and_material_bits()
    {
        HeightField field = PhysXCookedData.ReadHeightField(new PhysXStreams().HeightField3By2().ToArray());

        Assert.Equal((3, 2, 6), (field.Rows, field.Columns, field.Samples.Length));
        Assert.Equal((-2f, 3f), (field.MinimumHeight, field.MaximumHeight));
        Assert.Equal(new Vector3(2, 3, 1), field.Bounds.Maximum);
        Assert.Equal(-1f, field.Thickness);
        HeightFieldSample hole = field.Sample(0, 1);
        Assert.Equal(-1, hole.Height);
        Assert.True(hole.SharesFirstCorner);
        Assert.True(hole.FirstTriangleIsHole);
        Assert.Equal(126, hole.SecondTriangleMaterial);
        HeightFieldSample last = field.Sample(2, 1);
        Assert.Equal((3, (byte)5, (byte)122), (last.Height, last.FirstTriangleMaterial, last.SecondTriangleMaterial));
        Assert.False(last.SharesFirstCorner);
        Assert.True(field.Sample(0, 0).SecondTriangleIsHole);
        Assert.Throws<ArgumentOutOfRangeException>(() => field.Sample(3, 0));
    }

    [Fact]
    public void Landscape_native_data_holds_the_height_field_stream()
    {
        byte[] stream = new PhysXStreams().HeightField3By2().ToArray();
        byte[] native = new PhysXStreams().UInt32(0).UInt32(0).UInt32(1).UInt32(1).UInt32((uint)stream.Length).Bytes(stream).ToArray();

        Assert.Equal(stream, LandscapeCollision.ReadCookedHeightField(native).ToArray());
        Assert.True(LandscapeCollision.ReadCookedHeightField(new PhysXStreams().UInt32(0).UInt32(0).UInt32(0).ToArray()).IsEmpty);
        Assert.Throws<InvalidDataException>(() => LandscapeCollision.ReadCookedHeightField(native.AsMemory(..^1)));
        Assert.Throws<InvalidDataException>(() => LandscapeCollision.ReadCookedHeightField(new PhysXStreams().Bytes(native).Byte(0).ToArray()));
    }

    [Fact]
    public void Landscape_samples_run_along_reversed_rows()
    {
        HeightField field = PhysXCookedData.ReadHeightField(new PhysXStreams().HeightField3By2().ToArray());

        Assert.Equal(4, LandscapeCollision.SampleIndex(field, 0, 0));
        Assert.Equal(1, LandscapeCollision.SampleIndex(field, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => LandscapeCollision.SampleIndex(field, 3, 0));
        Assert.Equal(new Vector3D(12, 5, 1), LandscapeCollision.SamplePosition(new Vector3D(10, 3, 0), 1, 2, 2, 128));
    }

    public static TheoryData<string> Streams => ["convex", "large convex", "mesh8", "mesh16", "mesh32", "body", "heightfield"];

    [Theory]
    [MemberData(nameof(Streams))]
    public void Every_truncation_and_trailing_byte_is_rejected(string name)
    {
        byte[] data = Stream(name);
        Decode(name, data);

        for (int length = 0; length < data.Length; length++)
        {
            byte[] truncated = data[..length];
            Assert.Throws<InvalidDataException>(() => Decode(name, truncated));
        }

        Assert.Throws<InvalidDataException>(() => Decode(name, [.. data, 0]));
    }

    [Theory]
    [MemberData(nameof(Streams))]
    public void Garbage_is_rejected(string name)
    {
        Random random = new(1234);
        for (int i = 0; i < 200; i++)
        {
            byte[] garbage = new byte[random.Next(0, 400)];
            random.NextBytes(garbage);
            Assert.Throws<InvalidDataException>(() => Decode(name, garbage));
        }
    }

    [Fact]
    public void Wrong_versions_and_unsupported_layouts_are_rejected()
    {
        byte[] convex = new PhysXStreams().ConvexBox().ToArray();
        convex[8] = 14;
        byte[] badIndex = new PhysXStreams().TriangleQuad(1).ToArray();
        badIndex[28 + (4 * 12)] = 4;
        byte[] bv33 = new PhysXStreams().TriangleQuad(2).ToArray();
        bv33[12] = 0;
        byte[] gpu = new PhysXStreams().TriangleQuad(2).ToArray();
        gpu[16] |= 32;
        byte[] bigEndian = new PhysXStreams().HeightField3By2().ToArray();
        bigEndian[3] = 0;
        byte[] uvInfo = BodyPayload();
        uvInfo[^1] = 1;

        Assert.Throws<InvalidDataException>(() => PhysXCookedData.ReadConvexMesh(convex));
        Assert.Throws<InvalidDataException>(() => PhysXCookedData.ReadTriangleMesh(badIndex));
        Assert.Throws<InvalidDataException>(() => PhysXCookedData.ReadTriangleMesh(bv33));
        Assert.Throws<InvalidDataException>(() => PhysXCookedData.ReadTriangleMesh(gpu));
        Assert.Throws<InvalidDataException>(() => PhysXCookedData.ReadHeightField(bigEndian));
        Assert.Throws<InvalidDataException>(() => PhysXCookedData.ReadBodyGeometry(uvInfo));
    }

    private static byte[] BodyPayload() =>
        new PhysXStreams()
            .Byte(1).Int32(2).Int32(1).Int32(2)
            .Byte(1).ConvexBox(largeConvex: true)
            .Byte(0)
            .Byte(1).ConvexBox(gpuEdges: true)
            .TriangleQuad(1)
            .TriangleQuad(4, materials: false)
            .Int32(0).Int32(0).Int32(0)
            .ToArray();

    private static byte[] Stream(string name) => name switch
    {
        "convex" => new PhysXStreams().ConvexBox().ToArray(),
        "large convex" => new PhysXStreams().ConvexBox(gpuEdges: true, largeConvex: true).ToArray(),
        "mesh8" => new PhysXStreams().TriangleQuad(1).ToArray(),
        "mesh16" => new PhysXStreams().TriangleQuad(2, adjacencies: true).ToArray(),
        "mesh32" => new PhysXStreams().TriangleQuad(4).ToArray(),
        "body" => BodyPayload(),
        _ => new PhysXStreams().HeightField3By2().ToArray(),
    };

    private static void Decode(string name, byte[] data)
    {
        switch (name)
        {
            case "convex":
            case "large convex":
                PhysXCookedData.ReadConvexMesh(data);
                break;
            case "body":
                PhysXCookedData.ReadBodyGeometry(data);
                break;
            case "heightfield":
                PhysXCookedData.ReadHeightField(data);
                break;
            default:
                PhysXCookedData.ReadTriangleMesh(data);
                break;
        }
    }
}
