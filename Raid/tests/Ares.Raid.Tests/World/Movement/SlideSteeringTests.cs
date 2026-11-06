using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;

namespace Ares.Raid.Tests.World.Movement;

public sealed class SlideSteeringTests
{
    [Fact]
    public void Slope_input_begins_strictly_below_the_slope_threshold()
    {
        Assert.Equal(0, SlideSteering.SlopeSteepness(-10, 0.1));
        Assert.Equal(0.55, SlideSteering.SlopeSteepness(-27.5, 0.1), 12);
        Assert.Equal(1, SlideSteering.SlopeSteepness(-45, 0.1));
        Assert.Equal(1, SlideSteering.SlopeSteepness(-90, 0.1));
        Assert.Equal(45, SlideSteering.FreeMoveLimit(1300, 0, 0));
        Assert.Equal(45, SlideSteering.FreeMoveLimit(1300, 1300, -45));
    }

    [Fact]
    public void Alignment_changes_branches_at_the_free_movement_boundary()
    {
        Assert.Equal(1, SlideSteering.SlopeAlignment(45, 0, -20, 0.1));
        Assert.Equal(0.7, SlideSteering.SlopeAlignment(45, 30, -20, 0.1), 12);
        Assert.Equal(0.1, SlideSteering.SlopeAlignment(45, 45, -20, 0.1));
        Assert.Equal(0.1, SlideSteering.SlopeAlignment(45, 0, -10, 0.1));
        Assert.Equal(0, SlideSteering.SlopeAlignment(45, 90, -10, 0.1));
    }

    [Fact]
    public void Downhill_input_respects_ground_direction_and_alignment()
    {
        Vector3D normal = new(0.6, 0, 0.8);
        Assert.Equal(default, SlideSteering.SlopeNudge(650, normal, 1300, 0, 0));
        Assert.Equal(default, SlideSteering.SlopeNudge(650, normal, 1300, 1, -45));
        Assert.Equal(default, SlideSteering.SlopeNudge(650, new Vector3D(0, 0, 1), 1300, 0, -45));
        Assert.Equal(new Vector3D(0.35, 0, 0), SlideSteering.SlopeNudge(650, normal, 1300, 0, -45));
        Vector3D brake = SlideSteering.Brake(new Vector3D(1, 0, 0), new Vector3D(1, 0, 0), -45);
        Assert.Equal(1 - 0.8f, brake.X);
    }

    [Fact]
    public void Steering_outside_the_free_zone_requires_downhill_alignment()
    {
        Vector3D raw = new(0, 1, 0);
        Vector3D slide = new(1, 0, 0);
        Vector3D level = new(0, 0, 1);
        Assert.Equal(slide, SlideSteering.Rotate(raw, false, level, slide, 90));
        Vector3D free = SlideSteering.Rotate(raw, true, level, slide, 90);
        Assert.Equal(0, free.X, 12);
        Assert.Equal(1, free.Y, 12);
        Vector3D downhill = SlideSteering.Rotate(raw, false, new Vector3D(0, 0.6, 0.8), slide, 90);
        Assert.Equal(0, downhill.X, 12);
        Assert.Equal(1, downhill.Y, 12);
        Vector3D preserved = SlideSteering.Rotate(raw with { Y = 0 }, true, level, slide, 90);
        Assert.Equal(slide, preserved);
    }
}
