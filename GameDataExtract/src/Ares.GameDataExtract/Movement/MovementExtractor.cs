using Ares.GameDataExtract.Packages;
using Ares.Structs.Game.Movement;

namespace Ares.GameDataExtract.Movement;

/// <summary>The movement tables (see <see cref="MovementExtractor"/>).</summary>
/// <param name="DodgeRolls">The dodge-roll variants, in <see cref="DodgeRollKind"/> order.</param>
/// <param name="VaultDefinitions">The vault definition sets, in <see cref="MovementAssets.VaultPackages"/> order.</param>
/// <param name="HeavyLanding">The heavy-landing levels.</param>
/// <param name="GraphStateTags">The tags the character graph tracks, in bit order.</param>
/// <param name="SnapHook">The snap hook item's class defaults.</param>
/// <param name="SprintBurstCurve">The sprint burst alpha curve.</param>
internal sealed record MovementData(
    IReadOnlyList<DodgeRollSequence> DodgeRolls,
    IReadOnlyList<VaultDefinitionSet> VaultDefinitions,
    HeavyLandingTables HeavyLanding,
    IReadOnlyList<string> GraphStateTags,
    SnapHookItemDefaults SnapHook,
    IReadOnlyList<WeightedCurveKey> SprintBurstCurve);

/// <summary>
/// Extracts the movement tuning tables from the movement data assets, their animations, the character graph's logic
/// object, the snap hook Blueprint and the sprint curve (see <see cref="MovementAssets"/>).
/// </summary>
internal static class MovementExtractor
{
    /// <summary>Extracts the tables.</summary>
    /// <exception cref="InvalidDataException">An asset or animation is missing or not laid out as expected.</exception>
    public static MovementData Extract(PackageCache packages)
    {
        return new MovementData(
            DodgeRollReader.Read(packages),
            VaultReader.Read(packages),
            HeavyLandingReader.Read(packages),
            GraphTagReader.Read(packages),
            SnapHookReader.Read(packages),
            SprintCurveReader.Read(packages));
    }
}
