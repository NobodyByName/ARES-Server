using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Tests.Core;

public sealed class RotatorTests
{
    [Theory]
    [InlineData(0, 90, 0, 1, 0, 0, 0, 1, 0)]
    [InlineData(90, 0, 0, 1, 0, 0, 0, 0, 1)]
    [InlineData(0, 0, 90, 0, 1, 0, 0, 0, -1)]
    [InlineData(0, 30, 180, 0, 1, 0, 0.5, -0.8660254, 0)]
    public void Rotate_follows_the_engine_rotation_matrix(double pitch, double yaw, double roll, double x, double y, double z, double ex, double ey, double ez)
    {
        Vector3D rotated = new Rotator(pitch, yaw, roll).Rotate(new Vector3D(x, y, z));

        Assert.Equal(ex, rotated.X, 6);
        Assert.Equal(ey, rotated.Y, 6);
        Assert.Equal(ez, rotated.Z, 6);
    }
}
