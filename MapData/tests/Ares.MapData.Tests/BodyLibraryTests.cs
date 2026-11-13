using System.Collections.Immutable;
using System.Numerics;
using Ares.Common.Maps;
using Ares.MapData.Bodies;
using Ares.MapData.Packages;
using Ares.Structs.Game.Collision;
using Vector3D = Ares.Structs.Core.Geometry.Vector3D;

namespace Ares.MapData.Tests;

public sealed class BodyLibraryTests
{
    internal const string MeshPackage = "/Game/Meshes/SM_Rock";
    internal const string MeshPath = MeshPackage + ".SM_Rock";
    internal const ulong MeshHash = 1;
    private const int ChaosName = 3;
    private const int ProfileName = 4;

    private static readonly PackageObjectIndex s_staticMesh = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.StaticMesh);
    private static readonly PackageObjectIndex s_bodySetup = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.BodySetup);

    [Fact]
    public void A_body_setup_reads_its_simple_shapes_settings_and_cooked_data()
    {
        CookedPackage package = MeshPackageWith(FullBodySetup(NativeData(cooked: true, format: true)));

        BodySetupContents contents = BodySetupReader.Read(package, package.Exports[1]);

        CollisionSphere sphere = Assert.Single(contents.Spheres);
        Assert.Equal(new CollisionSphere(new Vector3(1, 2, 3), 7), sphere);
        CollisionBox box = Assert.Single(contents.Boxes);
        Assert.Equal(new Vector3(4, 5, 6), box.Center);
        Assert.Equal(new Vector3(5, 10, 15), box.HalfExtents);
        Assert.Equal(Vector3.UnitY, Vector3.Transform(Vector3.UnitX, box.Rotation), new VectorComparer());
        CollisionCapsule capsule = Assert.Single(contents.Capsules);
        Assert.Equal(5, capsule.Radius);
        Assert.Equal(20, capsule.HalfLength);
        ConvexElement convex = Assert.Single(contents.ConvexElements);
        Assert.Equal(new Vector3D(1, 1, 1), Assert.Single(convex.Vertices));
        Assert.Equal(new Vector3D(1, 2, 3), convex.Transform?.Translation);
        Assert.Equal(new WalkableSlope(1, 45), contents.WalkableSlope);
        Assert.Equal(CollisionTraceFlag.SimpleAsComplex, contents.TraceFlag);
        Assert.Equal("BlockAll", contents.DefaultInstance?.ProfileName);
        Assert.Equal(CollisionEnabled.QueryAndPhysics, contents.DefaultInstance?.CollisionEnabled);
        Assert.NotNull(contents.Cooked);
        Assert.Empty(contents.Cooked.ConvexHulls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public void Bodies_without_cooked_data_read_their_properties_up_to_the_native_data(bool cooked, bool format)
    {
        CookedPackage package = MeshPackageWith(TraceFlagOnly(CollisionTraceFlag.ComplexAsSimple, NativeData(cooked, format)));

        BodySetupContents contents = BodySetupReader.Read(package, package.Exports[1]);

        Assert.Equal(CollisionTraceFlag.ComplexAsSimple, contents.TraceFlag);
        Assert.Null(contents.Cooked);
        Assert.Null(contents.DefaultInstance);
    }

    [Fact]
    public void Malformed_body_setups_are_rejected()
    {
        byte[] trailing = [.. TraceFlagOnly(CollisionTraceFlag.UseDefault, NativeData(cooked: false, format: false)), 0];
        byte[] unknownMember = [.. Header(5), 1, .. NativeData(cooked: false, format: false)];
        byte[] badFlag = TraceFlagOnly((CollisionTraceFlag)4, NativeData(cooked: false, format: false));
        byte[] tapered = [.. Header(BodySetupMembers.AggregateGeometry), .. Header(AggregateGeometryMembers.TaperedCapsuleElements), .. Int32(1), .. Header(), .. NativeData(cooked: false, format: false)];

        foreach (byte[] bytes in new[] { trailing, unknownMember, badFlag, tapered })
        {
            CookedPackage package = MeshPackageWith(bytes);
            Assert.Throws<InvalidDataException>(() => BodySetupReader.Read(package, package.Exports[1]));
        }
    }

    [Fact]
    public void Simple_bodies_keep_their_shapes_and_cooked_hulls_but_not_mirrored_hulls()
    {
        ConvexHull hull = Hull(1);
        BodySetupContents contents = Contents(CollisionTraceFlag.UseDefault, [hull, null], [Hull(2)], [Mesh()]);

        AssetBody body = BodyLibrary.FromContents("Body", contents, CollisionProfileConfig.EngineDefaultShapeComplexity);

        Assert.False(body.IsEmpty);
        Assert.Same(hull, Assert.Single(body.Body!.ConvexHulls));
        Assert.Single(body.Body.Boxes);
        Assert.Empty(body.Body.TriangleMeshes);
    }

    [Fact]
    public void Complex_as_simple_bodies_keep_only_their_triangle_meshes()
    {
        TriangleMesh mesh = Mesh();
        BodySetupContents contents = Contents(CollisionTraceFlag.ComplexAsSimple, [Hull(1)], [], [mesh]);

        AssetBody body = BodyLibrary.FromContents("Body", contents, CollisionProfileConfig.EngineDefaultShapeComplexity);

        Assert.Same(mesh, Assert.Single(body.Body!.TriangleMeshes));
        Assert.Empty(body.Body.Boxes);
        Assert.Empty(body.Body.ConvexHulls);
        Assert.Equal(CollisionTraceFlag.ComplexAsSimple, body.TraceFlag);
    }

    [Fact]
    public void Default_trace_flags_follow_the_project_default_shape_complexity()
    {
        TriangleMesh mesh = Mesh();
        BodySetupContents contents = Contents(CollisionTraceFlag.UseDefault, [Hull(1)], [], [mesh]);

        AssetBody body = BodyLibrary.FromContents("Body", contents, CollisionTraceFlag.ComplexAsSimple);

        Assert.Same(mesh, Assert.Single(body.Body!.TriangleMeshes));
        Assert.Empty(body.Body.ConvexHulls);
        Assert.Equal(CollisionTraceFlag.ComplexAsSimple, body.TraceFlag);
    }

    [Fact]
    public void Bodies_without_geometry_are_empty()
    {
        BodySetupContents simple = Contents(CollisionTraceFlag.SimpleAsComplex, [], [], [Mesh()]) with { Boxes = [] };
        BodySetupContents complex = Contents(CollisionTraceFlag.ComplexAsSimple, [Hull(1)], [], []);

        Assert.True(BodyLibrary.FromContents("Simple", simple, CollisionProfileConfig.EngineDefaultShapeComplexity).IsEmpty);
        Assert.True(BodyLibrary.FromContents("Complex", complex, CollisionProfileConfig.EngineDefaultShapeComplexity).IsEmpty);
    }

    [Fact]
    public void Mesh_bodies_are_found_by_object_path_and_decoded_once()
    {
        CookedPackage package = MeshPackageWith(FullBodySetup(NativeData(cooked: true, format: true)));
        int loads = 0;
        BodyLibrary library = new(new PackageCache(path =>
        {
            loads++;
            return path == MeshPackage ? package : null;
        }, _ => null), CollisionProfileConfig.EngineDefaultShapeComplexity);

        AssetBody body = library.GetMeshBody(MeshPath);

        Assert.Same(body, library.GetMeshBody(MeshPath));
        Assert.Equal(1, loads);
        Assert.Equal(MeshPath, body.Path);
        Assert.Equal(MeshPath, body.Body?.Name);
        Assert.Single(body.Body!.Spheres);
        Assert.Equal("BlockAll", body.DefaultInstance?.ProfileName);
        Assert.Throws<InvalidDataException>(() => library.GetMeshBody("/Game/Meshes/Missing.Missing"));
        AssetBody cell = library.GetCellBody(new ExportRef(package, package.Exports[1]));
        Assert.Equal(MeshPath + ":BodySetup_0", cell.Path);
        Assert.Same(cell, library.GetCellBody(new ExportRef(package, package.Exports[1])));
        Assert.Throws<InvalidDataException>(() => library.GetCellBody(new ExportRef(package, package.Exports[0])));
    }

    [Fact]
    public void Meshes_without_a_body_setup_have_no_collision()
    {
        CookedPackage package = CookedPackage.Parse(SyntheticArchives.Package(MeshPackage, [("SM_Rock", PackageObjectIndex.Null, s_staticMesh, 1, [])]));
        BodyLibrary library = new(new PackageCache(path => path == MeshPackage ? package : null, _ => null), CollisionProfileConfig.EngineDefaultShapeComplexity);

        AssetBody body = library.GetMeshBody(MeshPath);

        Assert.True(body.IsEmpty);
        Assert.Null(body.DefaultInstance);
    }

    /// <summary>
    /// The mesh package of <see cref="MeshPath"/>: one sphere (radius 7 at (1, 2, 3)), box and capsule, simple collision,
    /// default instance profile BlockAll.
    /// </summary>
    internal static byte[] MeshPackageBytes() => MeshPackageBytes(FullBodySetup(NativeData(cooked: true, format: true)));

    private static byte[] MeshPackageBytes(byte[] bodySetup)
    {
        return SyntheticArchives.Package(MeshPackage,
            [
                ("SM_Rock", PackageObjectIndex.Null, s_staticMesh, MeshHash, []),
                ("BodySetup_0", PackageObjectIndex.FromExport(0), s_bodySetup, 0, bodySetup),
            ],
            extraNames: ["Chaos", "BlockAll"]);
    }

    private static CookedPackage MeshPackageWith(byte[] bodySetup) => CookedPackage.Parse(MeshPackageBytes(bodySetup));

    /// <summary>A body setup with one element of each simple kind, a walkable slope, a default instance, a build scale and a trace flag.</summary>
    private static byte[] FullBodySetup(byte[] nativeData)
    {
        byte[] sphere = [.. Header(0, 1, 4, 5), .. Vector(1, 2, 3), .. Single(7), 1, 3];
        byte[] box = [.. Header(0, 1, 2, 3, 4), .. Vector(4, 5, 6), .. Vector(0, 90, 0), .. Single(10), .. Single(20), .. Single(30)];
        byte[] capsule = [.. Header(2, 3, 4), .. Single(5), .. Single(40), .. Single(0.5f)];
        byte[] convex =
        [
            .. Header(0, 1, 2, 3),
            .. Int32(1), .. Vector(1, 1, 1),
            .. Int32(3), .. Int32(0), .. Int32(0), .. Int32(0),
            .. new byte[ConvexElementMembers.ElementBoxSize],
            .. Header(TransformMembers.Translation), .. Vector(1, 2, 3),
        ];
        byte[] geometry = [.. Header(0, 1, 2, 3), .. Int32(1), .. sphere, .. Int32(1), .. box, .. Int32(1), .. capsule, .. Int32(1), .. convex];
        byte[] slope = [.. Header(0, 1), 1, .. Single(45)];
        byte[] instance = [.. Header(BodyInstanceMembers.CollisionEnabled, BodyInstanceMembers.CollisionProfileName), (byte)CollisionEnabled.QueryAndPhysics, .. Int32(ProfileName), .. Int32(0)];
        return
        [
            .. Header(BodySetupMembers.AggregateGeometry, BodySetupMembers.Unknown4, BodySetupMembers.WalkableSlopeOverride, BodySetupMembers.DefaultInstance,
                BodySetupMembers.BuildScale3D, BodySetupMembers.CollisionTraceFlag),
            .. geometry,
            1,
            .. slope,
            .. instance,
            .. Vector(1, 1, 1),
            (byte)CollisionTraceFlag.SimpleAsComplex,
            .. nativeData,
        ];
    }

    private static byte[] TraceFlagOnly(CollisionTraceFlag flag, byte[] nativeData) => [.. Header(BodySetupMembers.CollisionTraceFlag), (byte)flag, .. nativeData];

    /// <summary>
    /// The native data after the properties: no object GUID, the body GUID, the cooked flag and, when cooked, an unknown
    /// word and the format count, then the format's name, bulk data index and an empty body payload.
    /// </summary>
    private static byte[] NativeData(bool cooked, bool format)
    {
        byte[] head = [.. Int32(0), .. new byte[16], .. Int32(cooked ? 1 : 0)];
        if (!cooked)
        {
            return head;
        }

        if (!format)
        {
            return [.. head, .. Int32(0), .. Int32(0)];
        }

        byte[] payload = [1, .. Int32(0), .. Int32(0), .. Int32(0), .. Int32(0), .. Int32(0), .. Int32(0)];
        return [.. head, .. Int32(1), .. Int32(1), .. Int32(ChaosName), .. Int32(0), .. Int32(0), .. payload];
    }

    /// <summary>An unversioned header listing <paramref name="members"/> (ascending), all stored, with no zero mask.</summary>
    private static byte[] Header(params int[] members)
    {
        List<byte> bytes = [];
        int next = 0;
        int i = 0;
        do
        {
            int start = i < members.Length ? members[i] : next;
            int count = 0;
            while (i < members.Length && members[i] == start + count)
            {
                count++;
                i++;
            }

            int value = (start - next) | (i >= members.Length ? 0x100 : 0) | (count << 9);
            bytes.Add((byte)value);
            bytes.Add((byte)(value >> 8));
            next = start + count;
        }
        while (i < members.Length);

        return [.. bytes];
    }

    private static byte[] Int32(int value) => BitConverter.GetBytes(value);

    private static byte[] Single(float value) => BitConverter.GetBytes(value);

    private static byte[] Vector(double x, double y, double z) => [.. BitConverter.GetBytes(x), .. BitConverter.GetBytes(y), .. BitConverter.GetBytes(z)];

    private static BodySetupContents Contents(CollisionTraceFlag flag, ImmutableArray<ConvexHull?> hulls, ImmutableArray<ConvexHull?> mirrored, ImmutableArray<TriangleMesh> meshes)
    {
        return new BodySetupContents(
            [],
            [new CollisionBox(Vector3.Zero, Quaternion.Identity, Vector3.One)],
            [],
            [],
            null,
            flag,
            null,
            new CookedBodyGeometry(hulls, mirrored, meshes));
    }

    private static ConvexHull Hull(float size)
    {
        return new ConvexHull([Vector3.Zero, new Vector3(size, 0, 0), new Vector3(0, size, 0), new Vector3(0, 0, size)], [], [],
            new BoundingBox(Vector3.Zero, new Vector3(size)));
    }

    private static TriangleMesh Mesh()
    {
        return new TriangleMesh([Vector3.Zero, Vector3.UnitX, Vector3.UnitY], [0, 1, 2], [], new BoundingBox(Vector3.Zero, new Vector3(1, 1, 0)));
    }

    private sealed class VectorComparer : IEqualityComparer<Vector3>
    {
        public bool Equals(Vector3 x, Vector3 y) => Vector3.Distance(x, y) < 1e-5f;

        public int GetHashCode(Vector3 obj) => 0;
    }
}
