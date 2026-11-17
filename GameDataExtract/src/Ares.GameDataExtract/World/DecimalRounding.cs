using System.Globalization;
using System.Numerics;
using Ares.Structs.Core.Geometry;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Rounds extracted values to a number of decimal places the way the game data tables store them: the exact binary
/// value is rounded to the nearest multiple of 10^−digits (ties to even), and that decimal is read back as the nearest
/// double. A rounded zero is always positive zero.
/// </summary>
internal static class DecimalRounding
{
    private const int MantissaBits = 52;
    private const int ExponentBias = 1075;
    private const long MantissaMask = (1L << MantissaBits) - 1;

    /// <summary><paramref name="value"/> rounded to <paramref name="digits"/> decimal places.</summary>
    /// <exception cref="InvalidDataException">The value is not finite.</exception>
    public static double Round(double value, int digits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(digits);
        if (!double.IsFinite(value))
        {
            throw new InvalidDataException($"The value {value} is not finite");
        }

        if (value is 0)
        {
            return 0.0;
        }

        // value = ±mantissa · 2^exponent, exactly.
        long bits = BitConverter.DoubleToInt64Bits(value);
        int biased = (int)((bits >> MantissaBits) & 0x7FF);
        long mantissa = bits & MantissaMask;
        if (biased == 0)
        {
            biased = 1;
        }
        else
        {
            mantissa |= 1L << MantissaBits;
        }

        int exponent = biased - ExponentBias;
        BigInteger numerator = mantissa * BigInteger.Pow(10, digits);
        BigInteger denominator = BigInteger.One;
        if (exponent > 0)
        {
            numerator <<= exponent;
        }
        else
        {
            denominator <<= -exponent;
        }

        BigInteger quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
        int half = (remainder * 2).CompareTo(denominator);
        if (half > 0 || (half == 0 && !quotient.IsEven))
        {
            quotient += 1;
        }

        if (quotient.IsZero)
        {
            return 0.0;
        }

        double rounded = double.Parse(string.Create(CultureInfo.InvariantCulture, $"{quotient}e-{digits}"), NumberStyles.Float, CultureInfo.InvariantCulture);
        return bits < 0 ? -rounded : rounded;
    }

    /// <summary>Each component of <paramref name="value"/> rounded to <paramref name="digits"/> decimal places.</summary>
    public static Vector3D Round(Vector3D value, int digits) => new(Round(value.X, digits), Round(value.Y, digits), Round(value.Z, digits));

    /// <summary>Each angle of <paramref name="value"/> rounded to <paramref name="digits"/> decimal places.</summary>
    public static Rotator Round(Rotator value, int digits) => new(Round(value.Pitch, digits), Round(value.Yaw, digits), Round(value.Roll, digits));

    /// <summary>
    /// An angle in degrees wrapped to [−180, 180) as <c>((angle + 180) mod 360) − 180</c> (a floored modulo), rounded to
    /// <paramref name="digits"/> decimal places, with −180 given as 180: the range (−180, 180].
    /// </summary>
    public static double WrappedAngle(double degrees, int digits)
    {
        const double Turn = 360.0;
        double shifted = degrees + 180.0;
        double modulo = shifted % Turn;
        modulo = modulo is 0 ? 0.0 : modulo < 0 ? modulo + Turn : modulo;
        double rounded = Round(modulo - 180.0, digits);
        return rounded is -180.0 ? 180.0 : rounded;
    }

    /// <summary>Each angle of <paramref name="value"/> as <see cref="WrappedAngle"/> gives it.</summary>
    public static Rotator WrappedAngles(Rotator value, int digits) =>
        new(WrappedAngle(value.Pitch, digits), WrappedAngle(value.Yaw, digits), WrappedAngle(value.Roll, digits));
}
