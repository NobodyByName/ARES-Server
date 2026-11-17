using Ares.GameDataExtract.Animations;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Movement;

namespace Ares.GameDataExtract.Movement;

/// <summary>The heavy-landing tables (see <see cref="HeavyLandingReader"/>).</summary>
/// <param name="Heavy">Levels of an ordinary heavy landing.</param>
/// <param name="Recovery">Levels of a heavy landing softened by a recovery roll.</param>
internal sealed record HeavyLandingTables(IReadOnlyList<HeavyLandingLevel> Heavy, IReadOnlyList<HeavyLandingLevel> Recovery);

/// <summary>
/// Reads the heavy-landing levels (<see cref="MovementAssetSchemas.HeavyLandingAnimations"/>) and the animations of
/// the heavy and recovery-roll levels: each one's horizontal root path at <see cref="MovementAssets.AnimationSampleRate"/>
/// and its branch-tag windows. Crash-pad levels are checked but not kept.
/// </summary>
internal static class HeavyLandingReader
{
    private const string AnimationsClass = "UHeavyLandingAnimations";
    private const int MaxLevels = 64;
    private static readonly string[] s_directions = ["NoDirection", "Forward", "Left", "Right", "Backward"];

    /// <summary>Reads the tables.</summary>
    /// <exception cref="InvalidDataException">The asset or an animation does not have the expected layout.</exception>
    public static HeavyLandingTables Read(PackageCache packages, ScriptCache scripts)
    {
        ScriptSchemaCheck.OwnMembers(scripts, AnimationsClass, MovementAssetSchemas.HeavyLandingAnimations);
        CookedPackage package = MovementPackages.Load(packages, MovementAssets.HeavyLandingAnimationsPackage);
        PropertyBag asset = MovementPackages.ReadAsset(package, MovementPackages.OnlyExport(package), MovementAssetSchemas.HeavyLandingAnimations, [0, 1, 2]);
        IReadOnlyList<PropertyField> arrays = [.. MovementAssetSchemas.HeavyLandingAnimations.Members.Select(member => member!)];
        LevelPaths[][] levels = [.. arrays.Select(array => Levels(package, asset.Get<object[]>(array.Name), array.Name))];

        // The crash-pad levels (the last array) use another bone codec; only the heavy and recovery animations are read.
        Dictionary<string, HeavyLandingClip> clips = new(StringComparer.Ordinal);
        foreach (string sequence in levels[0].Concat(levels[1]).SelectMany(level => level.Sequences).OfType<string>().Distinct().Order(StringComparer.Ordinal))
        {
            clips[sequence] = Clip(packages, sequence);
        }

        return new HeavyLandingTables(Build(levels[0], clips), Build(levels[1], clips));
    }

    private static LevelPaths[] Levels(CookedPackage package, object[] entries, string array)
    {
        if (entries.Length is <= 0 or >= MaxLevels)
        {
            throw new InvalidDataException($"{package.Name}: {array} has {entries.Length} levels");
        }

        return
        [
            .. entries.Select(entry =>
            {
                PropertyBag level = (PropertyBag)entry;
                string?[] sequences = [.. s_directions.Select(direction => MovementPackages.ImportedPackage(package, level.Get(direction, new ObjectReference(0)), $"{array} {direction}"))];
                return sequences[0] is null
                    ? throw new InvalidDataException($"{package.Name}: a level of {array} has no no-direction sequence")
                    : new LevelPaths(level.Get("HeightDistanceThreshold", 0.0), sequences);
            }),
        ];
    }

    private static HeavyLandingClip Clip(PackageCache packages, string sequence)
    {
        CookedPackage animation = MovementPackages.Load(packages, sequence);
        string name = MovementPackages.ShortName(sequence);
        ZenExport export = MovementPackages.Named(animation, name);
        IReadOnlyList<BranchWindow> windows = BranchWindowReader.Read(animation, export, MovementAssets.BranchTagExportPrefix, MovementAssets.BranchTagStateIndex);
        if (windows.Count == 0 || windows.Any(window => !Enum.IsDefined((HeavyLandingTransientState)window.State)))
        {
            throw new InvalidDataException($"{sequence}: unexpected branch windows");
        }

        IReadOnlyList<RootPoint> root = RootTrackReader.ReadHorizontalPath(MovementPackages.Bytes(animation, export), MovementAssets.AnimationSampleRate);
        return new HeavyLandingClip(
            name,
            MovementAssets.AnimationSampleRate,
            [.. root.Select(point => new HeavyLandingRootPoint(SingleRounding.ToSingle(point.Right), SingleRounding.ToSingle(point.Forward)))],
            [.. windows.Select(window => new HeavyLandingWindow((HeavyLandingTransientState)window.State, SingleRounding.ToSingle(window.Start), SingleRounding.ToSingle(window.End)))]);
    }

    private static HeavyLandingLevel[] Build(IEnumerable<LevelPaths> levels, Dictionary<string, HeavyLandingClip> clips)
    {
        return
        [
            .. levels.Select(level => new HeavyLandingLevel(
                level.Height,
                clips[level.Sequences[0]!],
                Optional(level.Sequences[1]),
                Optional(level.Sequences[2]),
                Optional(level.Sequences[3]),
                Optional(level.Sequences[4]))),
        ];

        HeavyLandingClip? Optional(string? sequence) => sequence is null ? null : clips[sequence];
    }

    /// <summary>A level's fall height and its sequences by direction (<see cref="s_directions"/> order; null when none).</summary>
    private sealed record LevelPaths(double Height, string?[] Sequences);
}
