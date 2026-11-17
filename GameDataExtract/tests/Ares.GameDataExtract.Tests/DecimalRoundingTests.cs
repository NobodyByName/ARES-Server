using Ares.GameDataExtract.World;

namespace Ares.GameDataExtract.Tests;

public sealed class DecimalRoundingTests
{
    [Theory]
    [InlineData(0.125, 2, 0.12)]
    [InlineData(0.375, 2, 0.38)]
    [InlineData(2.675, 2, 2.67)]
    [InlineData(505658.224, 2, 505658.22)]
    [InlineData(-16.3314999, 3, -16.331)]
    [InlineData(0.99995, 4, 1)]
    [InlineData(1e-310, 4, 0)]
    public void Values_round_to_the_nearest_decimal_with_ties_to_even(double value, int digits, double expected)
    {
        Assert.Equal(expected, DecimalRounding.Round(value, digits));
    }

    [Fact]
    public void A_value_that_rounds_to_zero_is_positive_zero()
    {
        Assert.True(double.IsPositive(DecimalRounding.Round(-0.004, 2)));
        Assert.True(double.IsPositive(DecimalRounding.Round(-0.0, 2)));
        Assert.Throws<InvalidDataException>(() => DecimalRounding.Round(double.NaN, 2));
    }

    [Theory]
    [InlineData(-180, 180)]
    [InlineData(180, 180)]
    [InlineData(190, -170)]
    [InlineData(-190.0004, 169.9996)]
    [InlineData(-0.00001, 0)]
    public void Wrapped_angles_lie_in_the_half_open_turn(double degrees, double expected)
    {
        double wrapped = DecimalRounding.WrappedAngle(degrees, 4);

        Assert.Equal(expected, wrapped);
        Assert.False(wrapped is 0 && double.IsNegative(wrapped));
    }
}
