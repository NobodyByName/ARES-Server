using System.Globalization;
using System.Numerics;
using Ares.GameDataExtract.World;

namespace Ares.GameDataExtract.Movement;

/// <summary>
/// Converts computed values to the floats the movement tables store: the exact binary value is rounded to
/// <see cref="SignificantDigits"/> significant decimal digits (ties to even), and that decimal is read as the nearest
/// float. A float widened to double comes back unchanged; zero keeps its sign.
/// </summary>
internal static class SingleRounding
{
    /// <summary>Significant digits kept, enough to tell every float apart.</summary>
    public const int SignificantDigits = 9;

    /// <summary><paramref name="value"/> rounded to <see cref="SignificantDigits"/> significant digits, as a float.</summary>
    /// <exception cref="InvalidDataException">The value is not finite.</exception>
    public static float ToSingle(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidDataException($"The value {value} is not finite");
        }

        if (value is 0)
        {
            return (float)value;
        }

        (BigInteger numerator, BigInteger denominator) = DecimalRounding.Exact(value);

        // Scale by 10^-power so that the quotient has SignificantDigits digits before rounding.
        int power = (int)Math.Floor(Math.Log10(Math.Abs(value))) - SignificantDigits + 1;
        (BigInteger quotient, BigInteger remainder) = Divide(numerator, denominator, power);
        if (quotient >= BigInteger.Pow(10, SignificantDigits))
        {
            power++;
            (quotient, remainder) = Divide(numerator, denominator, power);
        }
        else if (quotient < BigInteger.Pow(10, SignificantDigits - 1))
        {
            power--;
            (quotient, remainder) = Divide(numerator, denominator, power);
        }

        BigInteger divisor = power >= 0 ? denominator * BigInteger.Pow(10, power) : denominator;
        int half = (remainder * 2).CompareTo(divisor);
        if (half > 0 || (half == 0 && !quotient.IsEven))
        {
            quotient += 1;
        }

        float rounded = float.Parse(string.Create(CultureInfo.InvariantCulture, $"{quotient}e{power}"), NumberStyles.Float, CultureInfo.InvariantCulture);
        return double.IsNegative(value) ? -rounded : rounded;
    }

    /// <summary>numerator / denominator / 10^power as an integer quotient and the remainder over the matching divisor.</summary>
    private static (BigInteger Quotient, BigInteger Remainder) Divide(BigInteger numerator, BigInteger denominator, int power)
    {
        return power >= 0
            ? (BigInteger.DivRem(numerator, denominator * BigInteger.Pow(10, power), out BigInteger remainder), remainder)
            : (BigInteger.DivRem(numerator * BigInteger.Pow(10, -power), denominator, out BigInteger scaledRemainder), scaledRemainder);
    }
}
