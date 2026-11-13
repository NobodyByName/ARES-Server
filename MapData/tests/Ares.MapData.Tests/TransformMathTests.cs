using Ares.MapData.Components;
using Ares.Structs.Core.Geometry;

namespace Ares.MapData.Tests;

public sealed class TransformMathTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void A_yaw_becomes_a_rotation_about_z()
    {
        Quaternion q = TransformMath.ToQuaternion(new Rotator(0, 90, 0));

        AssertClose(new Quaternion(0, 0, Math.Sqrt(0.5), Math.Sqrt(0.5)), q);
        AssertClose(new Vector3D(0, 1, 0), TransformMath.Rotate(q, new Vector3D(1, 0, 0)));
    }

    [Fact]
    public void A_child_is_scaled_rotated_and_moved_by_its_parent()
    {
        Transform parent = TransformMath.FromParts(new Vector3D(10, 20, 30), new Rotator(0, 90, 0), new Vector3D(2, 2, 2));
        Transform child = TransformMath.FromParts(new Vector3D(100, 0, 0), new Rotator(0, 90, 0), new Vector3D(1, 0.5, 1));

        Transform world = TransformMath.Compose(child, parent);

        AssertClose(new Vector3D(10, 220, 30), world.Translation);
        AssertClose(TransformMath.ToQuaternion(new Rotator(0, 180, 0)), world.Rotation);
        AssertClose(new Vector3D(2, 1, 2), world.Scale3D);
    }

    [Fact]
    public void Absolute_members_ignore_the_parent()
    {
        Transform parent = TransformMath.FromParts(new Vector3D(10, 20, 30), new Rotator(0, 90, 0), new Vector3D(2, 2, 2));
        Transform child = TransformMath.FromParts(new Vector3D(100, 0, 0), new Rotator(10, 0, 0), new Vector3D(1, 1, 1));

        Transform world = TransformMath.Compose(child, parent, new AbsoluteMembers(Location: true, Rotation: true, Scale: true));

        Assert.Equal(child, world);
    }

    [Fact]
    public void A_negative_scale_composes_like_the_matrix_product()
    {
        Transform parent = TransformMath.FromParts(new Vector3D(5, -7, 2), new Rotator(20, 70, -15), new Vector3D(1.5, 1.5, 1.5));
        Transform child = TransformMath.FromParts(new Vector3D(30, 40, -10), new Rotator(-5, 30, 60), new Vector3D(-1, 2, 0.5));

        Transform world = TransformMath.Compose(child, parent);

        Assert.True(world.Scale3D.X < 0);
        TransformMatrix expected = TransformMath.ToMatrix(child).Then(TransformMath.ToMatrix(parent));
        TransformMatrix actual = TransformMath.ToMatrix(world);
        AssertClose(expected.AxisX, actual.AxisX);
        AssertClose(expected.AxisY, actual.AxisY);
        AssertClose(expected.AxisZ, actual.AxisZ);
        AssertClose(expected.Origin, actual.Origin);
    }

    [Fact]
    public void An_absolute_member_with_a_negative_scale_is_rejected()
    {
        Transform parent = TransformMath.FromParts(default, default, new Vector3D(-1, 1, 1));

        Assert.Throws<InvalidDataException>(() => TransformMath.Compose(parent, parent, new AbsoluteMembers(Location: true, Rotation: false, Scale: false)));
    }

    [Fact]
    public void Composition_matches_the_matrix_product_for_positive_scales()
    {
        Transform parent = TransformMath.FromParts(new Vector3D(1000, 2000, -300), new Rotator(10, -45, 5), new Vector3D(2, 2, 2));
        Transform child = TransformMath.FromParts(new Vector3D(12, -8, 40), new Rotator(0, 15, 30), new Vector3D(1, 1, 1));

        TransformMatrix composed = TransformMath.ToMatrix(TransformMath.Compose(child, parent));
        TransformMatrix product = TransformMath.ToMatrix(child).Then(TransformMath.ToMatrix(parent));

        AssertClose(product.AxisX, composed.AxisX);
        AssertClose(product.AxisY, composed.AxisY);
        AssertClose(product.AxisZ, composed.AxisZ);
        AssertClose(product.Origin, composed.Origin);
    }

    [Fact]
    public void An_instance_matrix_is_taken_to_world_space_by_its_parent()
    {
        TransformMatrix instance = new(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), new Vector3D(100, 0, 0));
        TransformMatrix parent = TransformMath.ToMatrix(TransformMath.FromParts(new Vector3D(0, 0, 50), new Rotator(0, 90, 0), Vector3D.One));

        TransformMatrix world = instance.Then(parent);

        AssertClose(new Vector3D(0, 100, 50), world.Origin);
        AssertClose(new Vector3D(0, 1, 0), world.AxisX);
        AssertClose(new Vector3D(-1, 0, 0), world.AxisY);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(2, 0.5, 3)]
    [InlineData(-1, 2, 0.5)]
    [InlineData(1, -1, 1)]
    public void A_matrix_decomposes_into_the_transform_it_was_built_from(double scaleX, double scaleY, double scaleZ)
    {
        TransformMatrix matrix = TransformMath.ToMatrix(TransformMath.FromParts(new Vector3D(5, -7, 2), new Rotator(20, 70, -15), new Vector3D(scaleX, scaleY, scaleZ)));

        Transform transform = TransformMath.FromMatrix(matrix);
        TransformMatrix rebuilt = TransformMath.ToMatrix(transform);

        AssertClose(new Vector3D(5, -7, 2), transform.Translation);
        Assert.True(transform.Scale3D is { Y: > 0, Z: > 0 });
        Assert.Equal(scaleX * scaleY * scaleZ < 0, transform.Scale3D.X < 0);
        AssertClose(matrix.AxisX, rebuilt.AxisX);
        AssertClose(matrix.AxisY, rebuilt.AxisY);
        AssertClose(matrix.AxisZ, rebuilt.AxisZ);
    }

    [Fact]
    public void A_matrix_with_a_zero_axis_is_rejected()
    {
        TransformMatrix matrix = new(new Vector3D(1, 0, 0), default, new Vector3D(0, 0, 1), default);

        Assert.Throws<InvalidDataException>(() => TransformMath.FromMatrix(matrix));
    }

    private static void AssertClose(Vector3D expected, Vector3D actual)
    {
        Assert.Equal(expected.X, actual.X, Tolerance);
        Assert.Equal(expected.Y, actual.Y, Tolerance);
        Assert.Equal(expected.Z, actual.Z, Tolerance);
    }

    private static void AssertClose(Quaternion expected, Quaternion actual)
    {
        Assert.Equal(expected.X, actual.X, Tolerance);
        Assert.Equal(expected.Y, actual.Y, Tolerance);
        Assert.Equal(expected.Z, actual.Z, Tolerance);
        Assert.Equal(expected.W, actual.W, Tolerance);
    }
}
