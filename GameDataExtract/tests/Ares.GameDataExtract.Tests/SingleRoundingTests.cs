using Ares.GameDataExtract.Movement;

namespace Ares.GameDataExtract.Tests;

public sealed class SingleRoundingTests
{
    [Theory]
    [InlineData(0.1f)]
    [InlineData(-372.361145f)]
    [InlineData(1.75466184e-07f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.Epsilon)]
    public void A_widened_float_comes_back_unchanged(float value)
    {
        Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(SingleRounding.ToSingle(value)));
    }

    [Theory]
    [InlineData(0.1, 0.1f)]
    [InlineData(-9.947457313537598, -9.94745731f)]
    [InlineData(123456.789012, 123456.789f)]
    [InlineData(2.0000000149011612, 2.00000000f)]
    public void A_double_becomes_the_float_nearest_its_nine_digit_decimal(double value, float expected)
    {
        Assert.Equal(expected, SingleRounding.ToSingle(value));
    }

    [Fact]
    public void Zero_keeps_its_sign_and_non_finite_values_fail()
    {
        Assert.True(float.IsNegative(SingleRounding.ToSingle(-0.0)));
        Assert.True(float.IsPositive(SingleRounding.ToSingle(0.0)));
        Assert.Throws<InvalidDataException>(() => SingleRounding.ToSingle(double.NaN));
        Assert.Throws<InvalidDataException>(() => SingleRounding.ToSingle(double.PositiveInfinity));
    }
}
