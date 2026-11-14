using Ares.Structs.Game.Movement;

namespace Ares.Structs.Tests.Game;

public sealed class PendulumRotationTests
{
    private static readonly PendulumQuaternionField s_field = new(0, 4096U, 8U, 33281361UL, new PendulumQuaternion(0U, 0U, 0U, 0U, 15, 0, 1, 2), PendulumFieldFlags.Owner);

    [Theory]
    [InlineData(44U, 881U, 286U, 0U, 11, 0, 2, 1, -5.8)]
    [InlineData(612U, 610U, 0U, 4028U, 1, 1, 0, 3, 90.1)]
    [InlineData(0U, 0U, 3991U, 0U, 15, 0, 1, 2, 87.6)]
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

    [Theory]
    [InlineData(90.2)]
    [InlineData(-89.8)]
    public void A_sideways_heading_fits_a_coarse_rotation_field(double degrees)
    {
        // The pawn's action rotation: a component reaches the field's last step near a quarter turn.
        PendulumQuaternionField coarse = s_field with { Maximum = 64U, Total = 8100UL };
        PendulumQuaternion rotation = PendulumRotations.FromYaw(degrees * Math.PI / 180.0, coarse);
        Assert.Equal(63U, Math.Max(rotation.Component2, rotation.Component3));
        Assert.InRange(Math.Abs((PendulumRotations.Yaw(rotation, coarse) * 180.0 / Math.PI) - degrees), 0.0, 1.0);
    }

    [Fact]
    public void A_heading_rotation_stores_the_smaller_vertical_component()
    {
        PendulumQuaternion rotation = PendulumRotations.FromYaw(87.5 * Math.PI / 180.0, s_field);
        Assert.Equal(new PendulumQuaternion(0, 0, 3989, 0, 15, 0, 1, 2), rotation);
    }

    [Fact]
    public void A_level_ground_normal_is_stored_as_a_quarter_turn_without_its_last_component()
    {
        PendulumQuaternionField coarse = s_field with { Maximum = 64U, Total = 8100UL };
        Assert.Equal(new PendulumQuaternion(0, 0, 63, 0, 0b1011, 0, 1, 2), PendulumRotations.FromGroundNormal(0, 0, 1, coarse));
    }

    [Fact]
    public void A_sloped_ground_normal_is_read_back_from_its_rotation()
    {
        (double x, double y, double z) = (0.1, -0.2, Math.Sqrt(1 - 0.05));
        (double qx, double qy, double qz, double qw) = PendulumRotations.Components(PendulumRotations.FromGroundNormal(x, y, z, s_field), s_field);
        Assert.Equal(x, 2 * ((qx * qz) + (qw * qy)), 2);
        Assert.Equal(y, 2 * ((qy * qz) - (qw * qx)), 2);
    }
}
