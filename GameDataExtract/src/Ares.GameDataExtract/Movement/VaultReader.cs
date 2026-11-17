using Ares.GameDataExtract.Animations;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.GameDataExtract.Movement;

/// <summary>
/// Reads the vault definition sets (<see cref="MovementAssets.VaultPackages"/>, <see cref="MovementAssetSchemas.VaultDefinitionsDataAsset"/>)
/// and their animations: each definition's root path (cooked root positions every sample step) joins its sequence, and
/// each sequence's play length comes from its export. Members a definition does not store take the script defaults
/// (play rates 1, <see cref="MovementAssets.DefaultVaultSampleSeconds"/>, <see cref="MovementAssets.DefaultVaultEntrySeconds"/>).
/// </summary>
internal static class VaultReader
{
    private const string AssetClass = "UVaultDefinitionsDataAsset";
    private const double SampleTolerance = 1e-4;
    private static readonly string[] s_tables = ["FallToVaultDefinitions", "InAirVaultDefinitions", "VaultDefinitions"];

    /// <summary>The sets in <see cref="MovementAssets.VaultPackages"/> order.</summary>
    /// <exception cref="InvalidDataException">An asset or an animation does not have the expected layout, or two definitions disagree on a root path.</exception>
    public static IReadOnlyList<VaultDefinitionSet> Read(PackageCache packages, ScriptCache scripts)
    {
        ScriptSchemaCheck.OwnMembers(scripts, AssetClass, MovementAssetSchemas.VaultDefinitionsDataAsset);
        Dictionary<string, RootPath> paths = new(StringComparer.Ordinal);
        SetRows[] sets = [.. MovementAssets.VaultPackages.Select(path => Asset(MovementPackages.Load(packages, path), paths))];
        Dictionary<string, VaultSequence> sequences = Sequences(packages, paths);
        return [.. sets.Select(set => Set(set, sequences))];
    }

    private static SetRows Asset(CookedPackage package, Dictionary<string, RootPath> paths)
    {
        ZenExport export = MovementPackages.OnlyExport(package);
        PropertyBag asset = PropertyReader.ReadPrefix(package, CookedPackage.StartOf(export), MovementAssetSchemas.VaultDefinitionsDataAsset, out bool complete);
        if (complete)
        {
            throw new InvalidDataException($"{package.Name}: the vault definitions do not store their sequence list");
        }

        HeightRow[][] tables =
        [
            .. s_tables.Select(table => Bags(asset, table).Select(vertical => new HeightRow(
                VerticalName(vertical),
                vertical.Get("Height", 0.0),
                [.. Bags(vertical, "Speeds").Select(speed => Speed(package, speed, paths))])).ToArray()),
        ];
        return new SetRows(asset.Get("Settings", Empty()).Get("GlobalPlayRate", 1.0), tables);
    }

    private static VaultDefinitionSet Set(SetRows set, Dictionary<string, VaultSequence> sequences)
    {
        VaultHeightDefinition[][] tables =
        [
            .. set.Tables.Select(table => table.Select(row => new VaultHeightDefinition(row.Name, row.Height,
                [.. row.Speeds.Select(speed => new VaultSpeedDefinition(speed.Speed, Clip(speed.Over, sequences), Clip(speed.Onto, sequences)))])).ToArray()),
        ];
        return new VaultDefinitionSet(set.GlobalPlayRate, tables[0], tables[1], tables[2]);
    }

    /// <summary>The over and onto clips of one speed row.</summary>
    private static SpeedRow Speed(CookedPackage package, PropertyBag speed, Dictionary<string, RootPath> paths)
    {
        ClipDefinition? over = Definition(package, speed.Get("VaultOver", Empty()), paths);
        ClipDefinition? onto = Definition(package, speed.Get("VaultOnTo", Empty()), paths);
        return over is null || onto is null
            ? throw new InvalidDataException($"{package.Name}: a speed row without both clips")
            : new SpeedRow(speed.Get("Speed", 0.0), over, onto);
    }

    /// <summary>One vault definition; its root path joins its sequence's. Null when the definition names no sequence.</summary>
    private static ClipDefinition? Definition(CookedPackage package, PropertyBag definition, Dictionary<string, RootPath> paths)
    {
        if (MovementPackages.ImportedPackage(package, definition.Get("Sequence", new ObjectReference(0)), "a vault sequence") is not { } sequence)
        {
            return null;
        }

        PropertyBag matching = definition.Get("DistanceMatchingRuntimeData", Empty());
        float[][] points =
        [
            .. Bags(matching, "PositionAtTimes").Select(point =>
            {
                Vector3D position = point.Get("Position", default(Vector3D));
                return new[] { (float)position.X, (float)position.Y, (float)position.Z, point.Get("Time", 0f) };
            }),
        ];
        float step = matching.Get("SampleFreq", MovementAssets.DefaultVaultSampleSeconds);
        if (points.Where((point, k) => Math.Abs(point[3] - (k * (double)step)) > SampleTolerance).Any())
        {
            throw new InvalidDataException($"{sequence}: the root path is not sampled every {step} s");
        }

        if (!paths.TryGetValue(sequence, out RootPath? known))
        {
            paths[sequence] = new RootPath(step, points);
        }
        else if (points.Length > 0 && known.Points.Length > 0 && (!SamePoints(known.Points, points) || !known.SampleSeconds.Equals(step)))
        {
            throw new InvalidDataException($"{sequence}: definitions disagree on its root path");
        }
        else if (points.Length > 0 && known.Points.Length == 0)
        {
            paths[sequence] = new RootPath(step, points);
        }

        byte matched = definition.Get("RootMotionAnimMatchingType", (byte)0);
        if (matched > 1 || (matched == 1 && points.Length == 0))
        {
            throw new InvalidDataException($"{sequence}: distance matching without a root path");
        }

        return new ClipDefinition(
            sequence,
            definition.Get("AssetPlayRate", 1.0),
            definition.Get("bKeepDirection", false),
            definition.Get("bStepUp", false),
            matched == 1,
            matching.Get("MaxAllowedEntryTimeFromOnGround", MovementAssets.DefaultVaultEntrySeconds),
            matching.Get("MaxAllowedEntryTime", MovementAssets.DefaultVaultEntrySeconds));
    }

    /// <summary>The sequences with their play lengths, by package path.</summary>
    private static Dictionary<string, VaultSequence> Sequences(PackageCache packages, Dictionary<string, RootPath> paths)
    {
        Dictionary<string, VaultSequence> sequences = new(StringComparer.Ordinal);
        foreach ((string path, RootPath root) in paths.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            CookedPackage package = MovementPackages.Load(packages, path);
            string name = MovementPackages.ShortName(path);
            ZenExport export = MovementPackages.Named(package, name);
            double? last = root.Points.Length > 0 ? root.Points[^1][3] : null;
            float length = RootTrackReader.PlayLength(MovementPackages.Bytes(package, export), MovementAssets.AnimationSampleRate, last, root.SampleSeconds);
            sequences[path] = new VaultSequence(name, SingleRounding.ToSingle(length), SingleRounding.ToSingle(root.SampleSeconds),
                [.. root.Points.Select(point => new VaultPathPoint(SingleRounding.ToSingle(point[0]), SingleRounding.ToSingle(point[1]), SingleRounding.ToSingle(point[2])))]);
        }

        return sequences;
    }

    private static VaultClip Clip(ClipDefinition clip, Dictionary<string, VaultSequence> sequences)
    {
        return new VaultClip(sequences[clip.Sequence], clip.PlayRate, clip.KeepDirection, clip.StepUp, clip.DistanceMatching,
            SingleRounding.ToSingle(clip.MaxEntrySecondsFromGround), SingleRounding.ToSingle(clip.MaxEntrySeconds));
    }

    /// <summary>A vertical row's name; a row marked as zero is named <c>None</c>.</summary>
    private static string VerticalName(PropertyBag vertical) => vertical.Get("Name", "") is { Length: > 0 } name ? name : CookedPackage.NoneName;

    private static IEnumerable<PropertyBag> Bags(PropertyBag bag, string member) => bag.Get(member, Array.Empty<object>()).Cast<PropertyBag>();

    private static bool SamePoints(float[][] left, float[][] right) =>
        left.Length == right.Length && left.Zip(right).All(pair => pair.First.SequenceEqual(pair.Second));

    private static PropertyBag Empty() => new(MovementAssetSchemas.VaultDefinition, []);

    /// <summary>A vault definition set before its sequences are read.</summary>
    private sealed record SetRows(double GlobalPlayRate, HeightRow[][] Tables);

    /// <summary>A vertical row before its sequences are read.</summary>
    private sealed record HeightRow(string Name, double Height, SpeedRow[] Speeds);

    /// <summary>A speed row before its sequences are read.</summary>
    private sealed record SpeedRow(double Speed, ClipDefinition Over, ClipDefinition Onto);

    /// <summary>A sequence's root path: points (X, Y, Z, time) every <paramref name="SampleSeconds"/>.</summary>
    private sealed record RootPath(float SampleSeconds, float[][] Points);

    /// <summary>A vault definition before its sequence is read.</summary>
    private sealed record ClipDefinition(
        string Sequence,
        double PlayRate,
        bool KeepDirection,
        bool StepUp,
        bool DistanceMatching,
        float MaxEntrySecondsFromGround,
        float MaxEntrySeconds);
}
