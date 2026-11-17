using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.Loot;
using static Ares.GameDataExtract.Tests.SyntheticScriptCache;
using B = Ares.Structs.Game.Loot.LootContainerBlueprintMembers;
using T = Ares.Structs.Game.Loot.StaticLootSpawnManagerTweakablesMembers;

namespace Ares.GameDataExtract.Tests;

/// <summary>
/// A made-up script cache holding the loot classes the loot extraction reads defaults from, with known literals, and an
/// asset registry that lists no asset.
/// </summary>
internal static class SyntheticLootScripts
{
    public const int SlotCount = 6;
    public const double CullDistance = 1500;
    public const float AlignmentSeconds = 0.25f;
    public const double StandardDistance = 120;
    public const double StationaryDistance = 180;
    public const int MaxPerGroup = 3;

    private const uint Reflected = ScriptProperty.ReflectedFlag;
    private const long ArrayType = 0x7000;
    private const long MapType = 0x7001;
    private const long StackWeightType = 0x7002;
    private const long AddKey = 0x9000;
    private const long SquareKey = 0x9001;
    private const long DormancyKey = 0x9002;
    private const long RelevantKey = 0x9003;
    private const long CullKey = 0x9004;
    private const int StackWeightFunction = 50;
    private const int DataAssetId = 11;
    private const int TweakablesId = 12;
    private const int ContainerId = 13;
    private const int StandardId = 14;
    private const int BaseInteractionId = 15;

    private static readonly ScriptDataType s_bool = new(0, 65);
    private static readonly ScriptDataType s_int = new(0, 68);
    private static readonly ScriptDataType s_float = new(0, 80);
    private static readonly ScriptDataType s_double = new(0, 81);

    /// <summary>Weighted target stack counts the item container's default statements add.</summary>
    public static LootTargetStacks[] TargetStacks { get; } = [new(2, 2.0), new(4, 1.5)];

    /// <summary>The tweakables the cache's tweakables class stores.</summary>
    public static LootSpawnTweakables Tweakables { get; } = new(0.5, 1.0, 0.25, 0.125, 0.0625, 0.03125, new Dictionary<int, double> { [0] = 1.0, [2] = 4.5 });

    public static SyntheticScriptCache Cache()
    {
        SyntheticScriptCache cache = new();
        DataAsset(cache);
        TweakablesClass(cache);
        Interaction(cache);
        Actor(cache);
        cache.Classes.Add(new TestClass("UStaticValueLootTableDataAsset", "UPrimaryDataAsset", [new TestProperty(StaticValueLootTableMembers.Entries, s_int, Reflected)]));
        cache.Classes.Add(new TestClass("UPlayerSkillLootTableDataAsset", "UPrimaryDataAsset", [new TestProperty(SkillLootTableMembers.Entries, s_int, Reflected)]));
        cache.Classes.Add(new TestClass(B.QuestScriptClass, "UQuestComponentBase", [new TestProperty("Unused", s_bool, Reflected)]));
        cache.Enums.Add((StaticValueLootTableMembers.QueryTypeEnum, ["Exact", "Query"], [0, 1]));
        cache.Enums.Add((StaticValueLootTableMembers.DistributionEnum, ["None", "RoundRobin", "MixAllPlacements"], [0, 1, 2]));
        cache.Enums.Add((SkillLootTableMembers.EntryTypeEnum, ["Nothing", SkillLootTableMembers.ItemEntry], [0, 1]));
        cache.Types[ArrayType] = ("TArray", [s_int]);
        cache.Types[MapType] = ("TMap", [s_int, s_double]);
        cache.Types[StackWeightType] = (ItemContainerComponentMembers.TargetStackWeight, []);
        cache.Functions[AddKey] = (ItemContainerComponentMembers.AddMethod, true, ArrayType, 1);
        cache.Functions[AddKey + 0x100] = (T.AddMethod, true, MapType, 2);
        cache.Functions[0x9200] = (ItemContainerComponentMembers.TargetStackWeight, true, StackWeightType, 2);
        cache.FunctionIds[StackWeightFunction] = 0x9200;
        return cache;
    }

    /// <summary>An asset registry without assets.</summary>
    public static byte[] EmptyRegistry()
    {
        MemoryStream stream = new();
        stream.Write(Convert.FromHexString("E79E7F713A49B0E93291B3880781381B"));
        Words(stream, AssetRegistry.SupportedVersion, 1);
        stream.Write(SyntheticArchives.NameBatch(["None"]));
        Words(stream, 0x12345679, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x87654321, 0);
        return stream.ToArray();
    }

    private static void DataAsset(SyntheticScriptCache cache)
    {
        const string Class = LootContainerDataAssetMembers.ScriptClass;
        Member(cache, Class, DataAssetId, LootContainerDataAssetMembers.IgnoreMaterialOverrides, 0x30);
        Member(cache, Class, DataAssetId, LootContainerDataAssetMembers.MaxContainersPerGroup, 0x34);
        Member(cache, Class, DataAssetId, LootContainerDataAssetMembers.DevelopmentAvailability, 0x38);
        uint[] constructor = Code(
            Op("SetV1", 2, 1), Op("LoadThisR", 0x30, DataAssetId), Op("WRTV1", 2),
            Op("SetV4", 2, MaxPerGroup), Op("LoadThisR", 0x34, DataAssetId), Op("WRTV4", 2),
            Op("SetV1", 2, 0), Op("LoadThisR", 0x38, DataAssetId), Op("WRTV1", 2),
            Op("RET"));
        cache.Classes.Add(new TestClass(Class, "UPrimaryDataAsset",
            [
                new TestProperty(LootContainerDataAssetMembers.IgnoreMaterialOverrides, s_bool, Reflected),
                new TestProperty(LootContainerDataAssetMembers.MaxContainersPerGroup, s_int, Reflected),
                new TestProperty(LootContainerDataAssetMembers.DevelopmentAvailability, s_int, Reflected),
            ],
            Constructors: [new TestFunction(Class, constructor, Class[1..])]));
        cache.Enums.Add((LootContainerDataAssetMembers.AvailabilityEnum, [LootContainerDataAssetMembers.RegisteredAvailability, "Development"], [0, 1]));
    }

    private static void TweakablesClass(SyntheticScriptCache cache)
    {
        string[] weights = [T.NumPlacementsWeight, T.NumTargetItemsWeight, T.NumAvoidItemsWeight, T.NumMissingTargetStacksWeight, T.NumStacksWeight, T.NumItemsWeight];
        double[] values = [Tweakables.NumPlacementsWeight, Tweakables.NumTargetItemsWeight, Tweakables.NumAvoidItemsWeight,
            Tweakables.NumMissingTargetStacksWeight, Tweakables.NumStacksWeight, Tweakables.NumItemsWeight];
        List<(string, int, uint[])> constructor = [];
        for (int i = 0; i < weights.Length; i++)
        {
            Member(cache, T.ScriptClass, TweakablesId, weights[i], 0x30 + (8 * i));
            constructor.AddRange([Op("SetV8", 2, Quad(values[i])), Op("LoadThisR", 0x30 + (8 * i), TweakablesId), Op("WRTV8", 2)]);
        }

        constructor.Add(Op("RET"));
        Member(cache, T.ScriptClass, TweakablesId, T.ContainerContentsWeightMap, 0x80);
        List<(string, int, uint[])> defaults = [];
        foreach ((int key, double weight) in Tweakables.ContainerContentsWeights)
        {
            defaults.AddRange([Op("SetV8", 2, Quad(weight)), Op("PSF", 2), Op("SetV4", 3, (uint)key), Op("PSF", 3), Op("PshThisMemRef", 0x80, TweakablesId), Op("CALLSYS", 0, Quad((ulong)AddKey + 0x100))]);
        }

        defaults.Add(Op("RET"));
        cache.Classes.Add(new TestClass(T.ScriptClass, "UBaseTweakables",
            [.. weights.Select(name => new TestProperty(name, s_double, Reflected)), new TestProperty(T.ContainerContentsWeightMap, new ScriptDataType(MapType, 5), Reflected)],
            [new TestFunction(ScriptClass.InitDefaults, Code([.. defaults]))],
            [new TestFunction(T.ScriptClass, Code([.. constructor]), T.ScriptClass[1..])]));
    }

    private static void Interaction(SyntheticScriptCache cache)
    {
        Member(cache, ItemContainerComponentMembers.ScriptClass, ContainerId, ItemContainerComponentMembers.ItemLimit, 0x40);
        Member(cache, ItemContainerComponentMembers.ScriptClass, ContainerId, ItemContainerComponentMembers.StaticLootSpawnTargetStacks, 0x48);
        List<(string, int, uint[])> stacks = [Op("SetV4", 2, SlotCount), Op("LoadThisR", 0x40, ContainerId), Op("WRTV4", 2)];
        foreach (LootTargetStacks stack in TargetStacks)
        {
            stacks.AddRange([Op("PshC8", 0, Quad(stack.Weight)), Op("PshC4", 0, (uint)stack.Target), Op("PSF", 4), Op("CALL", 0, StackWeightFunction),
                Op("PSF", 4), Op("PshThisMemRef", 0x48, ContainerId), Op("CALLSYS", 0, Quad((ulong)AddKey))]);
        }

        stacks.Add(Op("RET"));
        cache.Classes.Add(new TestClass(ItemContainerComponentMembers.ScriptClass, "UItemContainerComponentBase",
            [new TestProperty(ItemContainerComponentMembers.ItemLimit, s_int, Reflected), new TestProperty(ItemContainerComponentMembers.StaticLootSpawnTargetStacks, new ScriptDataType(ArrayType, 5), Reflected)],
            [new TestFunction(ScriptClass.InitDefaults, Code([.. stacks]))]));

        Member(cache, "UStandardInteractionComponent", StandardId, B.AlignmentTime, 0x50);
        uint[] constructor = Code(Op("SetV4", 2, (uint)BitConverter.SingleToInt32Bits(AlignmentSeconds)), Op("LoadThisR", 0x50, StandardId), Op("WRTV4", 2), Op("RET"));
        cache.Classes.Add(new TestClass(B.InteractionScriptClass, "UStandardInteractionComponent", [new TestProperty("SearchPromptText", s_int, 0)]));
        cache.Classes.Add(new TestClass("UStandardInteractionComponent", "UBaseInteractionComponent",
            [
                new TestProperty(B.InteractionType, s_int, Reflected),
                new TestProperty(B.HoldDuration, s_float, Reflected),
                new TestProperty(B.EquipRaiderTool, s_bool, Reflected),
                new TestProperty(B.AlignmentTime, s_float, Reflected),
            ],
            Constructors: [new TestFunction("UStandardInteractionComponent", constructor, "StandardInteractionComponent")]));
        TypeId(cache, "UBaseInteractionComponent", BaseInteractionId);
        cache.Classes.Add(new TestClass("UBaseInteractionComponent", "USceneComponent",
            [new TestProperty(B.ForceImmobilize, s_bool, Reflected), new TestProperty(B.PromptDistancePreset, s_int, Reflected)]));
        cache.Classes.Add(new TestClass(B.PromptDistanceConfig, null,
            [new TestProperty(B.PromptDistance, s_double, Reflected), new TestProperty("MarkerMidDistance", s_double, Reflected), new TestProperty("MarkerFarDistance", s_double, Reflected)]));
        cache.Globals.Add(new TestGlobal(B.StandardPreset, B.PromptDistanceConfig, Initializer: Preset(StandardDistance)));
        cache.Globals.Add(new TestGlobal(B.StationaryPreset, B.PromptDistanceConfig, Initializer: Preset(StationaryDistance)));
        cache.Enums.Add((B.InteractionTypeEnum, ["Press", "Hold", B.ContinuousInteraction], [0, 1, 2]));
        cache.Enums.Add((B.PromptDistancePresetEnum, [B.StandardPreset, "Custom"], [0, 1]));
    }

    private static void Actor(SyntheticScriptCache cache)
    {
        uint[] defaults = Code(
            Op("SetV1", 2, B.NetDormancyInitial), Op("PshV1", 2), Op("PshVPtr", 0), Op("CALLSYS", 0, Quad((ulong)DormancyKey)),
            Op("SetV1", 2, 0), Op("PshV1", 2), Op("PshVPtr", 0), Op("CALLSYS", 0, Quad((ulong)RelevantKey)),
            Op("PshC4", 0, (uint)CullDistance), Op("CALLSYS", 0, Quad((ulong)SquareKey)), Op("CpyRtoV4", 3), Op("iTOf", 3, 3), Op("PshV4", 3), Op("PshVPtr", 0),
            Op("CALLSYS", 0, Quad((ulong)CullKey)),
            Op("RET"));
        cache.Classes.Add(new TestClass(B.ScriptActor, "ALootContainerSingle", [new TestProperty("Unused", s_bool, Reflected)]));
        cache.Classes.Add(new TestClass("ALootContainerSingle", "AActor",
            [new TestProperty("ReplicatedState", s_int, Reflected | ScriptProperty.ReplicatedFlag)],
            [new TestFunction(ScriptClass.InitDefaults, defaults)]));
        cache.Functions[DormancyKey] = (B.SetDefaultNetDormancy, true, 0, 1);
        cache.Functions[RelevantKey] = (B.SetAlwaysRelevant, true, 0, 1);
        cache.Functions[SquareKey] = (B.Square, false, 0, 1);
        cache.Functions[CullKey] = (B.SetNetCullDistanceSquared, true, 0, 1);
    }

    private static TestFunction Preset(double distance)
    {
        return new TestFunction("Initializer", Code(Op("PshC8", 0, Quad(distance * 3)), Op("PshC8", 0, Quad(distance * 2)), Op("PshC8", 0, Quad(distance)), Op("RET")));
    }

    /// <summary>Gives <paramref name="type"/> the type id <paramref name="id"/> and its member <paramref name="name"/> the offset.</summary>
    private static void Member(SyntheticScriptCache cache, string type, int id, string name, int offset)
    {
        TypeId(cache, type, id);
        cache.Properties.Add((name, id, offset));
    }

    private static void TypeId(SyntheticScriptCache cache, string type, int id)
    {
        long key = 0x8000 + id;
        cache.Types[key] = (type, []);
        cache.TypeIds[id] = key;
    }

    private static void Words(MemoryStream stream, params uint[] words)
    {
        foreach (uint word in words)
        {
            stream.Write(BitConverter.GetBytes(word));
        }
    }
}
