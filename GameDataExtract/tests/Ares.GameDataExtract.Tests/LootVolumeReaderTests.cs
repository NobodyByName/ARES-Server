using Ares.GameDataExtract.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.Tests;

public sealed class LootVolumeReaderTests
{
    private static readonly Vector3D[] s_box =
    [
        new(0, 0, 0), new(2, 0, 0), new(0, 3, 0), new(2, 3, 0),
        new(0, 0, 4), new(2, 0, 4), new(0, 3, 4), new(2, 3, 4),
    ];

    [Fact]
    public void A_box_has_its_six_faces_as_outward_planes()
    {
        IReadOnlyList<LootVolumePlane>? planes = LootVolumeReader.BoundingPlanes(s_box, "box", allowFlat: false);

        Assert.NotNull(planes);
        Assert.Equal(6, planes.Count);
        Assert.Contains(new LootVolumePlane(new Vector3D(0, 0, -1), 0), planes);
        Assert.Contains(new LootVolumePlane(new Vector3D(0, 0, 1), 4), planes);
        Assert.Contains(new LootVolumePlane(new Vector3D(-1, 0, 0), 0), planes);
        Assert.Contains(new LootVolumePlane(new Vector3D(1, 0, 0), 2), planes);
        Assert.Contains(new LootVolumePlane(new Vector3D(0, -1, 0), 0), planes);
        Assert.Contains(new LootVolumePlane(new Vector3D(0, 1, 0), 3), planes);
        Assert.All(planes, plane => Assert.True(double.IsPositive(plane.Distance)));
    }

    [Fact]
    public void Flat_points_are_left_out_only_when_allowed()
    {
        Vector3D[] square = [new(0, 0, 5), new(1, 0, 5), new(0, 1, 5), new(1, 1, 5)];

        Assert.Null(LootVolumeReader.BoundingPlanes(square, "square", allowFlat: true));
        Assert.Throws<InvalidDataException>(() => LootVolumeReader.BoundingPlanes(square, "square", allowFlat: false));
        Assert.Throws<InvalidDataException>(() => LootVolumeReader.BoundingPlanes([new(0, 0, 0), new(1, 1, 1), new(2, 2, 2), new(3, 3, 3)], "line", allowFlat: true));
    }

    [Fact]
    public void Sums_are_compensated()
    {
        Assert.Equal(1.0, CompensatedFloat.Sum([1e16, 1.0, -1e16]));
        Assert.Equal(0.30000000000000004, CompensatedFloat.Sum([0.1, 0.2]));
        Assert.Equal(11.0, CompensatedFloat.Dot(new Vector3D(1, 2, 3), new Vector3D(3, 1, 2)));
    }

    [Theory]
    [InlineData(3, 4, 0, 5)]
    [InlineData(-1e300, 0, 0, 1e300)]
    [InlineData(1, 2, 2, 3)]
    [InlineData(0, 0, 0, 0)]
    public void Distances_are_euclidean(double x, double y, double z, double expected)
    {
        Assert.Equal(expected, CompensatedFloat.Distance(new Vector3D(x, y, z), default));
    }
}
