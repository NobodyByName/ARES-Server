using Ares.GameDataExtract.Animations;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.Movement;

namespace Ares.GameDataExtract.Movement;

/// <summary>
/// Reads the dodge-roll variants: the metrics data asset (<see cref="MovementAssetSchemas.DodgeRollMetrics"/>, members a
/// variant does not store take the <c>FDodgeRollSequenceData</c> constructor's defaults) and each variant's animation:
/// its forward root keys at <see cref="MovementAssets.AnimationSampleRate"/> and its branch-tag windows.
/// </summary>
internal static class DodgeRollReader
{
    private const string MetricsClass = "UDodgeRollMetrics";
    private const string SequenceStruct = "FDodgeRollSequenceData";

    /// <summary>Byte offsets of the constructor's stores of Distance, Speed, StaminaCost and ContactAnimationTime.</summary>
    private static readonly int[] s_defaultOffsets = [0, 8, 16, 24];

    /// <summary>The variants in <see cref="DodgeRollKind"/> order.</summary>
    /// <exception cref="InvalidDataException">An asset, the script defaults or an animation does not have the expected layout.</exception>
    public static IReadOnlyList<DodgeRollSequence> Read(PackageCache packages, ScriptCache scripts)
    {
        ScriptSchemaCheck.OwnMembers(scripts, MetricsClass, MovementAssetSchemas.DodgeRollMetrics);
        double[] defaults = Defaults(scripts);
        CookedPackage package = MovementPackages.Load(packages, MovementAssets.DodgeRollMetricsPackage);
        PropertyBag metrics = MovementPackages.ReadAsset(package, MovementPackages.OnlyExport(package), MovementAssetSchemas.DodgeRollMetrics, [0, 1, 2, 3]);
        List<DodgeRollSequence> rolls = [];
        foreach (DodgeRollKind kind in Enum.GetValues<DodgeRollKind>())
        {
            string member = MovementAssetSchemas.DodgeRollMetrics.Members[(int)kind]!.Name;
            PropertyBag roll = metrics.Get<PropertyBag>(member);
            string sequence = MovementPackages.ImportedPackage(package, roll.Get("Sequence", new ObjectReference(0)), $"the {kind} roll sequence")
                ?? throw new InvalidDataException($"{package.Name}: the {kind} roll has no sequence");
            rolls.Add(Roll(packages, roll, defaults, sequence));
        }

        return rolls;
    }

    private static DodgeRollSequence Roll(PackageCache packages, PropertyBag roll, double[] defaults, string sequence)
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
            roll.Get("Distance", defaults[0]),
            roll.Get("Speed", defaults[1]),
            roll.Get("StaminaCost", defaults[2]),
            roll.Get("ContactAnimationTime", defaults[3]),
            MovementAssets.AnimationSampleRate,
            [.. keys.Select(key => SingleRounding.ToSingle(key))],
            windows);
    }

    /// <summary>Distance, Speed, StaminaCost and ContactAnimationTime as the struct's constructor stores them.</summary>
    private static double[] Defaults(ScriptCache scripts)
    {
        ScriptClassDefaults defaults = ScriptClassDefaults.Of(scripts, SequenceStruct);
        if (!defaults.Stores.Keys.Order().SequenceEqual(s_defaultOffsets))
        {
            throw new InvalidDataException($"Unexpected {SequenceStruct} constructor");
        }

        return
        [
            .. s_defaultOffsets.Select(offset => defaults.Stores[offset] is { Size: sizeof(double), Constant: { } constant }
                ? constant.AsDouble
                : throw new InvalidDataException($"{SequenceStruct}: the store at offset {offset} is not a double literal")),
        ];
    }
}
