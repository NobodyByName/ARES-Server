using System.Numerics;
using Ares.GameDataExtract.Bodies;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using static Ares.GameDataExtract.Tests.PropertyBlocks;

namespace Ares.GameDataExtract.Tests;

public sealed class SplineMeshTests
{
    private const double Precision = 1e-4;

    private static readonly PackageObjectIndex s_splineMesh = PackageObjectIndex.FromScriptPath(SplineMeshComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_staticMesh = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.StaticMesh);

    [Fact]
    public void A_segment_keeps_the_class_default_for_members_it_does_not_store()
    {
        byte[] component =
        [
            .. Header(SplineMeshComponentMembers.SplineParams, SplineMeshComponentMembers.CachedMeshBodySetupGuid, SplineMeshComponentMembers.BodySetup),
            .. Header(SplineMeshParamsMembers.StartPosition, SplineMeshParamsMembers.StartRoll, SplineMeshParamsMembers.EndPosition),
            .. Vector(1, 2, 3),
            .. BitConverter.GetBytes(0.5f),
            .. Vector(201, 2, 3),
            .. new byte[16],
            .. BitConverter.GetBytes(2),
        ];

        CookedPackage package = Component(component);
        SplineMeshParams spline = ComponentReader.SplineParams(package, package.Exports[0]);

        Assert.Equal(new Vector3D(1, 2, 3), spline.StartPosition);
        Assert.Equal(new Vector3D(201, 2, 3), spline.EndPosition);
        Assert.Equal(new Vector3D(100, 0, 0), spline.StartTangent);
        Assert.Equal(new Vector3D(100, 0, 0), spline.EndTangent);
        Assert.Equal((1.0, 1.0), spline.StartScale);
        Assert.Equal((1.0, 1.0), spline.EndScale);
        Assert.Equal(0.5, spline.StartRoll, Precision);
        Assert.Equal(0, spline.EndRoll);
        Assert.Equal((0.0, 0.0), spline.EndOffset);
    }

    [Fact]
    public void A_component_storing_an_undecoded_own_member_is_refused()
    {
        byte[] component =
        [
            .. Header(SplineMeshComponentMembers.SplineParams, 1),
            .. Header(SplineMeshParamsMembers.StartPosition),
            .. Vector(1, 2, 3),
            .. Vector(0, 0, 1),
        ];
        CookedPackage package = Component(component);

        Assert.Throws<InvalidDataException>(() => ComponentReader.SplineParams(package, package.Exports[0]));
    }

    [Fact]
    public void A_straight_segment_moves_the_mesh_along_x_and_keeps_its_cross_section()
    {
        SplineMeshParams spline = Straight(400) with { StartScale = (2, 3), EndScale = (2, 3) };

        Vector3 bent = BendPoint(spline, new Vector3(0, 10, 5));

        // X 0 of bounds −50…50 is the segment's middle; Y and Z take the scale.
        AssertNear(new Vector3(200, 20, 15), bent);
        AssertNear(new Vector3(400, 0, 0), BendPoint(spline, new Vector3(50, 0, 0)));
    }

    [Fact]
    public void Roll_and_offset_turn_and_shift_the_cross_section()
    {
        SplineMeshParams spline = Straight(400) with { StartRoll = Math.PI / 2, EndRoll = Math.PI / 2, StartOffset = (7, 0), EndOffset = (7, 0) };

        // A quarter roll turns cross Y onto −Z and cross Z onto Y; the offset moves along the unrolled cross Y.
        AssertNear(new Vector3(200, 12, -10), BendPoint(spline, new Vector3(0, 10, 5)));
    }

    [Fact]
    public void A_curved_segment_follows_the_hermite_curve_and_its_direction()
    {
        // A quarter turn from +X to +Y: halfway the curve heads diagonally.
        SplineMeshParams spline = Straight(100) with { EndPosition = new Vector3D(100, 100, 0), StartTangent = new Vector3D(150, 0, 0), EndTangent = new Vector3D(0, 150, 0) };

        Vector3 middle = BendPoint(spline, Vector3.Zero);
        Vector3 side = BendPoint(spline, new Vector3(0, 10, 0));

        AssertNear(new Vector3(68.75f, 31.25f, 0), middle);
        Vector3 cross = Vector3.Normalize(side - middle);
        AssertNear(new Vector3(-MathF.Sqrt(0.5f), MathF.Sqrt(0.5f), 0), cross);
    }

    [Fact]
    public void A_static_mesh_stores_its_bounds_after_its_known_members()
    {
        byte[] mesh =
        [
            .. Header(StaticMeshMembers.Unknown4, StaticMeshMembers.Unknown5, StaticMeshMembers.ExtendedBounds),
            .. BitConverter.GetBytes(2471.6f),
            .. BitConverter.GetBytes(64),
            .. Header(BoxSphereBoundsMembers.Origin, BoxSphereBoundsMembers.BoxExtent, BoxSphereBoundsMembers.SphereRadius),
            .. Vector(0, 1, -4),
            .. Vector(909, 454, 13),
            .. BitConverter.GetBytes(1016.9),
        ];
        CookedPackage package = CookedPackage.Parse(SyntheticArchives.Package("/Game/Meshes/Road", [("SM_Road", PackageObjectIndex.Null, s_staticMesh, 1, mesh)]));

        (Vector3D origin, Vector3D extent) = StaticMeshValues.ExtendedBounds(package, package.Exports[0]);

        Assert.Equal(new Vector3D(0, 1, -4), origin);
        Assert.Equal(new Vector3D(909, 454, 13), extent);
    }

    private static CookedPackage Component(byte[] payload) =>
        CookedPackage.Parse(SyntheticArchives.Package("/Game/Maps/Cell", [("SplineMeshComponent_0", PackageObjectIndex.Null, s_splineMesh, 0, payload)]));

    private static SplineMeshParams Straight(double length) =>
        new(default, new Vector3D(length, 0, 0), (1, 1), 0, (0, 0), new Vector3D(length, 0, 0), new Vector3D(length, 0, 0), (1, 1), 0, (0, 0));

    /// <summary>Bends one point of a mesh whose bounds run from −50 to 50 along X.</summary>
    private static Vector3 BendPoint(SplineMeshParams spline, Vector3 point)
    {
        TriangleMesh mesh = new([point, point, point], [0, 1, 2], [], new BoundingBox(point, point));
        return SplineMeshDeformation.Bend(mesh, spline, -50, 100).Vertices[0];
    }

    private static void AssertNear(Vector3 expected, Vector3 actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
        Assert.Equal(expected.Z, actual.Z, 3);
    }
}
