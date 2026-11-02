using Ares.Structs.Game.Movement;

namespace Ares.Structs.Tests.Game;

public sealed class PendulumRotationTests
{
    private static readonly PendulumQuaternionField s_field = new(0, 4096U, 8U, 33281361UL, new PendulumQuaternion(0U, 0U, 0U, 0U, 15, 0, 1, 2), PendulumFieldFlags.Replicated);

    [Theory]
    [InlineData(44U, 881U, 286U, 0U, 11, 0, 2, 1, -5.8)]
    [InlineData(612U, 610U, 0U, 4028U, 1, 1, 0, 3, 90.1)]
    [InlineData(0U, 0U, 3991U, 0U, 15, 0, 1, 2, 87.5)]
    public void Headings_are_read_from_quantized_rotations(uint c0, uint c1, uint c2, uint c3, byte signs, byte smallest, byte middle, byte largest, double degrees)
    {
        PendulumQuaternion rotation = new(c0, c1, c2, c3, signs, smallest, middle, largest);
        Assert.Equal(degrees, PendulumRotations.Yaw(rotation, s_field) * 180.0 / Math.PI, 1);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(30.0)]
    [InlineData(-87.5)]
    [InlineData(135.0)]
    [InlineData(-179.0)]
    public void Headings_survive_quantization(double degrees)
    {
        PendulumQuaternion rotation = PendulumRotations.FromYaw(degrees * Math.PI / 180.0, s_field);
        Assert.Equal(degrees, PendulumRotations.Yaw(rotation, s_field) * 180.0 / Math.PI, 1);
        Assert.Equal(0U, rotation.Omitted switch
        {
            2 => rotation.Component2,
            _ => rotation.Component3,
        });
    }

    [Fact]
    public void A_heading_rotation_stores_the_smaller_vertical_component()
    {
        PendulumQuaternion rotation = PendulumRotations.FromYaw(87.5 * Math.PI / 180.0, s_field);
        Assert.Equal(new PendulumQuaternion(0, 0, 3989, 0, 15, 0, 1, 2), rotation);
    }
}
