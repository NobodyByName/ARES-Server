using System.Globalization;

namespace Ares.GameDataExtract.Loot;

/// <summary>How the loot tables give the numbers they read.</summary>
internal static class LootNumbers
{
    /// <summary>
    /// A float the game stores as the double with the same 7 significant digits (0.2, not 0.20000000298…), the way the
    /// tables give every float member.
    /// </summary>
    public static double Single(float value) => double.Parse(value.ToString("G7", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
