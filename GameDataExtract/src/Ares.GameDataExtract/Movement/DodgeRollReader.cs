using Ares.GameDataExtract.Animations;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.Structs.Game.Movement;

namespace Ares.GameDataExtract.Movement;

/// <summary>
/// Reads the dodge-roll variants: the metrics data asset (<see cref="MovementAssetSchemas.DodgeRollMetrics"/>, members a
/// variant does not store take the <c>FDodgeRollSequenceData</c> defaults of <see cref="DodgeRollParameters"/>) and each
/// variant's animation: its forward root keys at <see cref="MovementAssets.AnimationSampleRate"/> and its branch-tag windows.
/// </summary>
internal static class DodgeRollReader
{
    /// <summary>The variants in <see cref="DodgeRollKind"/> order.</summary>
    /// <exception cref="InvalidDataException">An asset or an animation does not have the expected layout.</exception>
    public static IReadOnlyList<DodgeRollSequence> Read(PackageCache packages)
    {
        CookedPackage package = MovementPackages.Load(packages, MovementAssets.DodgeRollMetricsPackage);
        PropertyBag metrics = MovementPackages.ReadAsset(package, MovementPackages.OnlyExport(package), MovementAssetSchemas.DodgeRollMetrics, [0, 1, 2, 3]);
        return [.. Enum.GetValues<DodgeRollKind>().Select(Variant)];

        DodgeRollSequence Variant(DodgeRollKind kind)
        {
            string member = MovementAssetSchemas.DodgeRollMetrics.Members[(int)kind]!.Name;
            PropertyBag roll = metrics.Get<PropertyBag>(member);
            string sequence = MovementPackages.ImportedPackage(package, roll.Get("Sequence", new ObjectReference(0)), $"the {kind} roll sequence")
                ?? throw new InvalidDataException($"{package.Name}: the {kind} roll has no sequence");
            return Roll(packages, roll, sequence);
        }
    }

    private static DodgeRollSequence Roll(PackageCache packages, PropertyBag roll, string sequence)
    {
        CookedPackage animation = MovementPackages.Load(packages, sequence);
        ZenExport export = MovementPackages.Named(animation, MovementPackages.ShortName(sequence));
        ReadOnlySpan<byte> bytes = MovementPackages.Bytes(animation, export);
        float[] keys = RootTrackReader.ReadForwardKeys(bytes, out int codecEnd);
        RootTrackReader.CheckKeyTimes(bytes, codecEnd, keys.Length, MovementAssets.AnimationSampleRate);
        DodgeRollWindow[] windows =
        [
            .. BranchWindowReader.Read(animation, export, MovementAssets.BranchTagExportPrefix, MovementAssets.BranchTagStateIndex).Select(window =>
                Enum.IsDefined((DodgeRollTransientState)window.State)
                    ? new DodgeRollWindow((DodgeRollTransientState)window.State, SingleRounding.ToSingle(window.Start), SingleRounding.ToSingle(window.End))
                    : throw new InvalidDataException($"{sequence}: unknown branch state {window.State}")),
        ];
        return new DodgeRollSequence(
            roll.Get("Distance", DodgeRollParameters.DefaultDistance),
            roll.Get("Speed", DodgeRollParameters.DefaultSpeed),
            roll.Get("StaminaCost", DodgeRollParameters.DefaultStaminaCost),
            roll.Get("ContactAnimationTime", DodgeRollParameters.DefaultContactAnimationSeconds),
            MovementAssets.AnimationSampleRate,
            [.. keys.Select(key => SingleRounding.ToSingle(key))],
            windows);
    }
}
