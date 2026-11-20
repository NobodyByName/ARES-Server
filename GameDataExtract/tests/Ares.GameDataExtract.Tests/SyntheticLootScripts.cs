using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Scripts;
using B = Ares.Structs.Game.Loot.LootContainerBlueprintMembers;

namespace Ares.GameDataExtract.Tests;

/// <summary>
/// Made-up script layouts holding the loot classes the loot extraction reads and the loot area classes (see
/// <see cref="SyntheticLootAreaScripts"/>), and an asset registry that lists no asset.
/// </summary>
internal static class SyntheticLootScripts
{
    /// <summary>The layouts of the loot and loot area classes, structs and enums.</summary>
    public static ScriptLayouts Layouts()
    {
        Dictionary<string, ScriptTypeDefinition> types = new(StringComparer.Ordinal);
        Dictionary<string, ScriptEnumDefinition> enums = new(StringComparer.Ordinal);
        Class(types, LootContainerDataAssetMembers.ScriptClass, "UPrimaryDataAsset",
            (LootContainerDataAssetMembers.IgnoreMaterialOverrides, ScriptMemberType.Bool),
            (LootContainerDataAssetMembers.MaxContainersPerGroup, ScriptMemberType.Int32),
            (LootContainerDataAssetMembers.DevelopmentAvailability, ScriptMemberType.Int32));
        Enum(enums, LootContainerDataAssetMembers.AvailabilityEnum, LootContainerDataAssetMembers.RegisteredAvailability, "Development");
        Class(types, "UStaticValueLootTableDataAsset", "UPrimaryDataAsset", (StaticValueLootTableMembers.Entries, ScriptMemberType.Int32));
        Class(types, "UPlayerSkillLootTableDataAsset", "UPrimaryDataAsset", (SkillLootTableMembers.Entries, ScriptMemberType.Int32));
        Class(types, B.QuestScriptClass, "UQuestComponentBase", ("Unused", ScriptMemberType.Bool));
        Class(types, B.AnimationScriptClass, "UEmbarkActorComponent", (B.AnimationTracks, ScriptMemberType.ArrayOf(ScriptMemberType.Int32)));
        Enum(enums, StaticValueLootTableMembers.QueryTypeEnum, "Exact", "Query");
        Enum(enums, StaticValueLootTableMembers.DistributionEnum, "None", "RoundRobin", "MixAllPlacements");
        Enum(enums, SkillLootTableMembers.EntryTypeEnum, "Nothing", SkillLootTableMembers.ItemEntry);
        Class(types, ItemContainerComponentMembers.ScriptClass, "UItemContainerComponentBase", (ItemContainerComponentMembers.ItemLimit, ScriptMemberType.Int32));
        Class(types, B.InteractionScriptClass, "UStandardInteractionComponent", (B.WaitDelayOnFirstOpen, ScriptMemberType.Double));
        Class(types, "UStandardInteractionComponent", "UBaseInteractionComponent",
            (B.InteractionType, ScriptMemberType.Int32),
            (B.HoldDuration, ScriptMemberType.Float),
            (B.EquipRaiderTool, ScriptMemberType.Bool),
            (B.AlignmentTime, ScriptMemberType.Float));
        Class(types, "UBaseInteractionComponent", "USceneComponent", (B.ForceImmobilize, ScriptMemberType.Bool), (B.PromptDistancePreset, ScriptMemberType.Int32));
        Enum(enums, B.InteractionTypeEnum, "Press", "Hold", B.ContinuousInteraction);
        Enum(enums, B.PromptDistancePresetEnum, B.StandardPreset, "Custom");
        Class(types, B.ScriptActor, "ALootContainerSingle", ("Unused", ScriptMemberType.Bool));
        Class(types, "ALootContainerSingle", "AActor",
            ("ReplicatedState", ScriptMemberType.Int32),
            (B.UseLidAnimationForOpenDelay, ScriptMemberType.Bool),
            (B.MinOpenDelay, ScriptMemberType.Double));
        SyntheticLootAreaScripts.Add(types, enums);
        return new ScriptLayouts(types, enums);
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

    /// <summary>Adds a class or struct with its reflected members, in declaration order.</summary>
    internal static void Class(Dictionary<string, ScriptTypeDefinition> types, string name, string? super, params (string Name, ScriptMemberType Type)[] members) =>
        types.Add(name, new ScriptTypeDefinition(name, super, [.. members.Select(member => new ScriptMemberDefinition(member.Name, member.Type))]));

    /// <summary>Adds an enum whose entries have the values 0, 1, 2, ….</summary>
    internal static void Enum(Dictionary<string, ScriptEnumDefinition> enums, string name, params string[] names) =>
        enums.Add(name, new ScriptEnumDefinition(name, names, [.. Enumerable.Range(0, names.Length).Select(value => (long)value)]));

    private static void Words(MemoryStream stream, params uint[] words)
    {
        foreach (uint word in words)
        {
            stream.Write(BitConverter.GetBytes(word));
        }
    }
}
