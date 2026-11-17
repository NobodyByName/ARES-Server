using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Movement;
using static Ares.GameDataExtract.Tests.SyntheticScriptCache;

namespace Ares.GameDataExtract.Tests;

/// <summary>
/// Made-up movement assets for an extraction run: the script classes the movement readers check, and the packages of
/// <see cref="MovementAssets"/> with one roll animation (shared by every dodge roll) and one landing animation (every
/// heavy-landing level). Vault sets are empty and the graph tracks <see cref="TagCount"/> tags.
/// </summary>
internal static class SyntheticMovementGame
{
    /// <summary>Distance the dodge-roll constructor stores.</summary>
    public const double RollDistance = 400;

    /// <summary>Number of tracked graph tags.</summary>
    public const int TagCount = 56;

    private const string RollAnimation = "/Game/Anims/Roll";
    private const string LandingAnimation = "/Game/Anims/Land";
    private const int InputTags = 8;
    private const uint Reflected = ScriptProperty.ReflectedFlag;

    private static readonly PackageObjectIndex s_firstImport = new((ulong)PackageObjectKind.PackageImport << 62);

    /// <summary>Adds the movement classes and structs to a precompiled script cache.</summary>
    public static void AddScripts(SyntheticScriptCache cache)
    {
        uint[] constructor = Code(
            Op("SetV8", 2, Quad(RollDistance)), Op("LoadThisR", 0, 11), Op("WRTV8", 2),
            Op("SetV8", 2, Quad(1.0)), Op("LoadThisR", 8, 11), Op("WRTV8", 2),
            Op("SetV8", 2, Quad(20.0)), Op("LoadThisR", 16, 11), Op("WRTV8", 2),
            Op("SetV8", 2, Quad(0.4)), Op("LoadThisR", 24, 11), Op("WRTV8", 2),
            Op("RET"));
        cache.Classes.Add(Class("UDodgeRollMetrics", MovementAssetSchemas.DodgeRollMetrics.Members));
        cache.Classes.Add(Class("FDodgeRollSequenceData", MovementAssetSchemas.DodgeRollSequenceData.Members) with
        {
            Constructors = [new TestFunction("FDodgeRollSequenceData", constructor)],
        });
        cache.Classes.Add(Class("UHeavyLandingAnimations", MovementAssetSchemas.HeavyLandingAnimations.Members));
        cache.Classes.Add(Class("UVaultDefinitionsDataAsset", [.. MovementAssetSchemas.VaultDefinitionsDataAsset.Members.Select(member => member ?? new PropertyField("AllAnimSequences", PropertyType.Object))]));
        cache.Classes.Add(Class("USnaphookItem",
            [.. Enumerable.Range(0, MovementAssetSchemas.SnaphookItemChain.ChainLength).Select(i => MovementAssetSchemas.SnaphookItemChain.At(i).Field ?? new PropertyField($"Unknown{i}", PropertyType.Int32))]));
    }

    /// <summary>The movement packages by path.</summary>
    public static IEnumerable<(string Path, byte[] Data)> Packages()
    {
        yield return (MovementAssets.DodgeRollMetricsPackage, Asset(MovementAssets.DodgeRollMetricsPackage, RollAnimation,
            [.. PropertyBlocks.Header(0, 1, 2, 3), .. Enumerable.Repeat<byte[]>([.. PropertyBlocks.Header(4), .. BitConverter.GetBytes(-1)], 4).SelectMany(roll => roll)]));
        yield return (RollAnimation, Animation(RollAnimation, [.. AnimationReaderTests.PerTrackExport(AnimationReaderTests.Track(1, 2, [0f, 10f, 20f]), 2 / 30f), .. AnimationReaderTests.Times(2)], tagState: null));
        byte[] level = [.. PropertyBlocks.Header(0, 1), .. BitConverter.GetBytes(0.0), .. BitConverter.GetBytes(-1)];
        yield return (MovementAssets.HeavyLandingAnimationsPackage, Asset(MovementAssets.HeavyLandingAnimationsPackage, LandingAnimation,
            [.. PropertyBlocks.Header(0, 1, 2), .. Enumerable.Repeat<byte[]>([.. BitConverter.GetBytes(1), .. level], 3).SelectMany(array => array)]));
        byte[] landing = [.. AnimationReaderTests.PerTrackExport(AnimationReaderTests.Track(1, 1 | 2, [3f, 4f, 5f, 6f], keys: 2), 1 / 30f, codec: "AnimCompress_PerTrackCompression_2"), 0, 0, 0, 0];
        yield return (LandingAnimation, Animation(LandingAnimation, [.. landing, .. AnimationReaderTests.NotifyEvent(stateClass: 2, start: 0.5f, duration: 0.25f), 0, 0, 0, 0], tagState: 3));
        foreach (string vault in MovementAssets.VaultPackages)
        {
            yield return (vault, Asset(vault, RollAnimation, [.. PropertyBlocks.Header(11), .. BitConverter.GetBytes(0)]));
        }

        yield return (MovementAssets.SimulationLogicPackage, SimulationLogic());
        yield return (MovementAssets.SnapHookBlueprint, SnapHook());
        byte[] keys = [.. Enumerable.Range(0, 3).SelectMany(key => (byte[])[2, 0, 3, .. AnimationReaderTests.Floats(key, key / 2f, 0, 1, 0, 1)])];
        yield return (MovementAssets.SprintBurstCurvePackage, SyntheticArchives.Package(MovementAssets.SprintBurstCurvePackage,
            [(ShortName(MovementAssets.SprintBurstCurvePackage), PackageObjectIndex.Null, PackageObjectIndex.Null, 0, [0x00, 0x03, 0x00, 0x03, .. BitConverter.GetBytes(3), .. keys, 0, 0, 0, 0])]));
    }

    private static TestClass Class(string name, IEnumerable<PropertyField?> members)
    {
        return new TestClass(name, null, [.. members.Select(member => new TestProperty(member!.Name, new ScriptDataType(0, 0), Reflected))]);
    }

    /// <summary>A one-export data asset importing <paramref name="imported"/>'s export as reference −1, then the zero trailer.</summary>
    private static byte[] Asset(string path, string imported, byte[] properties)
    {
        return SyntheticArchives.Package(path, [(ShortName(path), PackageObjectIndex.Null, PackageObjectIndex.Null, 0, [.. properties, 0, 0, 0, 0])],
            importedPackages: [imported], importedHashes: [1], imports: [s_firstImport]);
    }

    /// <summary>An animation package: the sequence export and a branch tag raising <paramref name="tagState"/> (none: no tag).</summary>
    private static byte[] Animation(string path, byte[] sequence, byte? tagState)
    {
        string name = ShortName(path);
        return tagState is { } state
            ? SyntheticArchives.Package(path,
            [
                (name, PackageObjectIndex.Null, PackageObjectIndex.Null, 0, sequence),
                (MovementAssets.BranchTagExportPrefix + "0", PackageObjectIndex.FromExport(0), PackageObjectIndex.Null, 0, [.. PropertyBlocks.Header(1), state, 0, 0, 0, 0]),
            ])
            : SyntheticArchives.Package(path, [(name, PackageObjectIndex.Null, PackageObjectIndex.Null, 0, sequence)]);
    }

    /// <summary>The logic object: the output tags with their settings, then the input tags, then the trailer.</summary>
    private static byte[] SimulationLogic()
    {
        string[] tags =
        [
            .. Enumerable.Range(0, TagCount).Select(i => i switch
            {
                MovementAssets.SprintActiveTagIndex => MovementAssets.SprintActiveTag,
                MovementAssets.FlashlightActiveTagIndex => MovementAssets.FlashlightActiveTag,
                _ => $"Test.Tag{i}",
            }),
        ];
        const int FirstTagName = 2;
        byte[] settings = [.. BitConverter.GetBytes(TagCount), .. Enumerable.Repeat<byte[]>([0x00, 0x07, 0, 0, 0], TagCount).SelectMany(item => item)];
        const string PackagePath = MovementAssets.SimulationLogicPackage;
        return SyntheticArchives.Package(PackagePath, [(ShortName(PackagePath), PackageObjectIndex.Null, PackageObjectIndex.Null, 0, [.. Names(TagCount), .. settings, .. Names(InputTags), 0, 0, 0, 0])],
            extraNames: tags);

        byte[] Names(int count) => [.. BitConverter.GetBytes(count), .. Enumerable.Range(0, count).SelectMany(i => PropertyBlocks.Name(FirstTagName + i))];
    }

    /// <summary>The snap hook class default: range 1800, cost 60, the data asset, a zero member, zoom time 0.2 and a flag.</summary>
    private static byte[] SnapHook()
    {
        const string PackagePath = MovementAssets.SnapHookBlueprint;
        byte[] header =
        [
            .. PropertyBlocks.Fragment(0, 1), .. PropertyBlocks.Fragment(9, 1), .. PropertyBlocks.Fragment(11, 1),
            .. PropertyBlocks.Fragment(7, 2, zeroes: true), .. PropertyBlocks.Fragment(17, 1, last: true), 0x01,
        ];
        byte[] values = [.. BitConverter.GetBytes(1800.0), .. BitConverter.GetBytes(60.0), .. BitConverter.GetBytes(-1), .. BitConverter.GetBytes(0.2), 1, 0, 0, 0, 0];
        return SyntheticArchives.Package(PackagePath, [($"Default__{ShortName(PackagePath)}_C", PackageObjectIndex.Null, PackageObjectIndex.Null, 0, [.. header, .. values])],
            importedPackages: [MovementAssets.SnapHookDataAsset], importedHashes: [1], imports: [s_firstImport]);
    }

    private static string ShortName(string path) => path[(path.LastIndexOf('/') + 1)..];
}
