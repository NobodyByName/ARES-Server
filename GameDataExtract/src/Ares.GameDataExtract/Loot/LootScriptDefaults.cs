using System.Globalization;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.Loot;

namespace Ares.GameDataExtract.Loot;

/// <summary>The literal defaults of the loot container data asset class's members a data asset does not set.</summary>
/// <param name="IgnoreMaterialOverrides">Default of <see cref="LootContainerDataAssetMembers.IgnoreMaterialOverrides"/>.</param>
/// <param name="MaxContainersPerGroup">Default of <see cref="LootContainerDataAssetMembers.MaxContainersPerGroup"/>.</param>
/// <param name="DevelopmentAvailability">Default of <see cref="LootContainerDataAssetMembers.DevelopmentAvailability"/>.</param>
internal sealed record LootContainerDataAssetDefaults(bool IgnoreMaterialOverrides, int MaxContainersPerGroup, byte DevelopmentAvailability);

/// <summary>The search interaction's script defaults a container template does not override.</summary>
/// <param name="InteractionType">Default <see cref="LootContainerBlueprintMembers.InteractionType"/>.</param>
/// <param name="HoldSeconds">Default <see cref="LootContainerBlueprintMembers.HoldDuration"/>.</param>
/// <param name="EquipsRaiderTool">Default <see cref="LootContainerBlueprintMembers.EquipRaiderTool"/>.</param>
/// <param name="AlignmentSeconds">Default <see cref="LootContainerBlueprintMembers.AlignmentTime"/>.</param>
/// <param name="ForceImmobilize">Default <see cref="LootContainerBlueprintMembers.ForceImmobilize"/>.</param>
/// <param name="PromptDistancePreset">Default <see cref="LootContainerBlueprintMembers.PromptDistancePreset"/>.</param>
/// <param name="SlotCount">Default <see cref="ItemContainerComponentMembers.ItemLimit"/>.</param>
/// <param name="StandardPromptDistance">Prompt distance of the standard preset.</param>
/// <param name="StationaryPromptDistance">Prompt distance of the standard preset while the character is immobilized.</param>
/// <param name="OpenDelaySeconds">Default <see cref="LootContainerBlueprintMembers.WaitDelayOnFirstOpen"/>.</param>
internal sealed record LootInteractionDefaults(
    byte InteractionType,
    double HoldSeconds,
    bool EquipsRaiderTool,
    double AlignmentSeconds,
    bool ForceImmobilize,
    byte PromptDistancePreset,
    int SlotCount,
    double StandardPromptDistance,
    double StationaryPromptDistance,
    double OpenDelaySeconds);

/// <summary>The container actor's script defaults that decide how its lid animation sets the open delay.</summary>
/// <param name="UseLidAnimation">Default <see cref="LootContainerBlueprintMembers.UseLidAnimationForOpenDelay"/>.</param>
/// <param name="MinSeconds">Default <see cref="LootContainerBlueprintMembers.MinOpenDelay"/>.</param>
internal sealed record LootOpenDelayDefaults(bool UseLidAnimation, double MinSeconds);

/// <summary>
/// Loot defaults the script cache holds: literals the constructors and default statements (<c>__InitDefaults</c>) of
/// the script class chains store or pass to calls, and the prompt distance preset globals. Members are found by name
/// (<see cref="ScriptCache.MemberOffsets"/>). Functions must be straight-line code the evaluation follows completely.
/// </summary>
internal static class LootScriptDefaults
{
    private const int ByteSize = 1;
    private const int Int32Size = 4;
    private const int DoubleSize = 8;
    private const int ConfigConstants = 3;

    /// <summary>Defaults of the loot container data asset class.</summary>
    /// <exception cref="InvalidDataException">A default is missing or of another size.</exception>
    public static LootContainerDataAssetDefaults DataAsset(ScriptCache cache)
    {
        const string Class = LootContainerDataAssetMembers.ScriptClass;
        ScriptClassDefaults defaults = Straight(cache, Class);
        return new LootContainerDataAssetDefaults(
            Stored(cache, defaults, Class, LootContainerDataAssetMembers.IgnoreMaterialOverrides, ByteSize).AsByte != 0,
            Stored(cache, defaults, Class, LootContainerDataAssetMembers.MaxContainersPerGroup, Int32Size).AsInt32,
            Stored(cache, defaults, Class, LootContainerDataAssetMembers.DevelopmentAvailability, ByteSize).AsByte);
    }

    /// <summary>The static loot spawn manager's tweakables.</summary>
    /// <exception cref="InvalidDataException">A weight or a map entry is missing or not a literal.</exception>
    public static LootSpawnTweakables Tweakables(ScriptCache cache)
    {
        const string Class = StaticLootSpawnManagerTweakablesMembers.ScriptClass;
        ScriptClassDefaults defaults = Straight(cache, Class);
        int map = Offset(cache, Class, StaticLootSpawnManagerTweakablesMembers.ContainerContentsWeightMap);
        SortedDictionary<int, double> weights = [];
        foreach (ScriptCall call in MemberCalls(defaults, map, StaticLootSpawnManagerTweakablesMembers.AddMethod))
        {
            if (call.Constant(0) is not { Size: Int32Size } key || call.Constant(1) is not { Size: DoubleSize } weight || !weights.TryAdd(key.AsInt32, weight.AsDouble))
            {
                throw new InvalidDataException($"{Class}: an entry of {StaticLootSpawnManagerTweakablesMembers.ContainerContentsWeightMap} is not a new literal key and weight");
            }
        }

        return new LootSpawnTweakables(
            Weight(StaticLootSpawnManagerTweakablesMembers.NumPlacementsWeight),
            Weight(StaticLootSpawnManagerTweakablesMembers.NumTargetItemsWeight),
            Weight(StaticLootSpawnManagerTweakablesMembers.NumAvoidItemsWeight),
            Weight(StaticLootSpawnManagerTweakablesMembers.NumMissingTargetStacksWeight),
            Weight(StaticLootSpawnManagerTweakablesMembers.NumStacksWeight),
            Weight(StaticLootSpawnManagerTweakablesMembers.NumItemsWeight),
            weights.Count > 0 ? weights : throw new InvalidDataException($"{Class}: no {StaticLootSpawnManagerTweakablesMembers.ContainerContentsWeightMap} entries"));

        double Weight(string member) => Stored(cache, defaults, Class, member, DoubleSize).AsDouble;
    }

    /// <summary>The weighted target stack counts every item container component starts with, in code order.</summary>
    /// <exception cref="InvalidDataException">An entry is not a literal target and weight.</exception>
    public static IReadOnlyList<LootTargetStacks> TargetStacks(ScriptCache cache)
    {
        const string Class = ItemContainerComponentMembers.ScriptClass;
        ScriptClassDefaults defaults = Straight(cache, Class);
        int member = Offset(cache, Class, ItemContainerComponentMembers.StaticLootSpawnTargetStacks);
        List<LootTargetStacks> stacks = [];
        foreach (ScriptCall call in MemberCalls(defaults, member, ItemContainerComponentMembers.AddMethod))
        {
            if (call.Arguments is not [ScriptConstructed { Call: { Name: ItemContainerComponentMembers.TargetStackWeight } entry }]
                || entry.Constant(0) is not { Size: Int32Size } target || entry.Constant(1) is not { Size: DoubleSize } weight)
            {
                throw new InvalidDataException($"{Class}: an entry of {ItemContainerComponentMembers.StaticLootSpawnTargetStacks} is not a literal {ItemContainerComponentMembers.TargetStackWeight}");
            }

            stacks.Add(new LootTargetStacks(target.AsInt32, weight.AsDouble));
        }

        return stacks.Count > 0 ? stacks : throw new InvalidDataException($"{Class}: no {ItemContainerComponentMembers.StaticLootSpawnTargetStacks} entries");
    }

    /// <summary>The search interaction's and the item container's defaults, and the prompt distances of the standard preset.</summary>
    /// <exception cref="InvalidDataException">A default is of another size, or a preset is missing or not literal.</exception>
    public static LootInteractionDefaults Interaction(ScriptCache cache)
    {
        const string Interaction = LootContainerBlueprintMembers.InteractionScriptClass;
        const string Container = ItemContainerComponentMembers.ScriptClass;
        ScriptClassDefaults interaction = Straight(cache, Interaction);
        ScriptClassDefaults container = Straight(cache, Container);
        return new LootInteractionDefaults(
            Optional(cache, interaction, Interaction, LootContainerBlueprintMembers.InteractionType, ByteSize)?.AsByte ?? 0,
            Optional(cache, interaction, Interaction, LootContainerBlueprintMembers.HoldDuration, Int32Size) is { } hold ? LootNumbers.Single(hold.AsSingle) : 0.0,
            Optional(cache, interaction, Interaction, LootContainerBlueprintMembers.EquipRaiderTool, ByteSize)?.AsByte is > 0,
            Optional(cache, interaction, Interaction, LootContainerBlueprintMembers.AlignmentTime, Int32Size) is { } alignment ? LootNumbers.Single(alignment.AsSingle) : 0.0,
            Optional(cache, interaction, Interaction, LootContainerBlueprintMembers.ForceImmobilize, ByteSize)?.AsByte is > 0,
            Optional(cache, interaction, Interaction, LootContainerBlueprintMembers.PromptDistancePreset, ByteSize)?.AsByte ?? 0,
            Optional(cache, container, Container, ItemContainerComponentMembers.ItemLimit, Int32Size)?.AsInt32 ?? 0,
            PromptDistance(cache, LootContainerBlueprintMembers.StandardPreset),
            PromptDistance(cache, LootContainerBlueprintMembers.StationaryPreset),
            Optional(cache, interaction, Interaction, LootContainerBlueprintMembers.WaitDelayOnFirstOpen, DoubleSize)?.AsDouble ?? 0.0);
    }

    /// <summary>The container actor's open delay defaults, stored by its script constructors and default statements.</summary>
    /// <exception cref="InvalidDataException">A default is of another size.</exception>
    public static LootOpenDelayDefaults OpenDelay(ScriptCache cache)
    {
        const string Class = LootContainerBlueprintMembers.ScriptActor;
        ScriptClassDefaults defaults = Straight(cache, Class);
        return new LootOpenDelayDefaults(
            Optional(cache, defaults, Class, LootContainerBlueprintMembers.UseLidAnimationForOpenDelay, ByteSize)?.AsByte is > 0,
            Optional(cache, defaults, Class, LootContainerBlueprintMembers.MinOpenDelay, DoubleSize)?.AsDouble ?? 0.0);
    }

    /// <summary>
    /// The net cull distance the container actor's script default statements set: the engine setter called with the
    /// square of a literal. The actor must stay initially dormant and not always relevant.
    /// </summary>
    /// <exception cref="InvalidDataException">A setter is not called with a literal, or the dormancy or relevancy changed.</exception>
    public static double NetCullDistance(ScriptCache cache)
    {
        const string Class = LootContainerBlueprintMembers.ScriptActor;
        ScriptClassDefaults defaults = Straight(cache, Class);
        if (Setter(LootContainerBlueprintMembers.SetDefaultNetDormancy).Constant(0)?.AsByte != LootContainerBlueprintMembers.NetDormancyInitial
            || Setter(LootContainerBlueprintMembers.SetAlwaysRelevant).Constant(0) is not { AsByte: 0 })
        {
            throw new InvalidDataException($"{Class}: container actors are no longer initially dormant and culled by distance");
        }

        ScriptValue squared = Setter(LootContainerBlueprintMembers.SetNetCullDistanceSquared).Arguments is [{ } first, ..] ? first : new ScriptUnknown("no argument");
        return squared switch
        {
            ScriptConverted { Value: ScriptReturned { Call: { Name: LootContainerBlueprintMembers.Square } square } } when square.Constant(0) is { Size: Int32Size } distance => distance.AsInt32,
            _ => throw new InvalidDataException($"{Class}: {LootContainerBlueprintMembers.SetNetCullDistanceSquared} is not called with a literal distance"),
        };

        ScriptCall Setter(string name) => defaults.Calls.SingleOrDefault(call => call.Name == name && call.Object is ScriptThis)
            ?? throw new InvalidDataException($"{Class}: the default statements do not call {name} once");
    }

    /// <summary><see cref="LootContainerBlueprintMembers.PromptDistance"/> of the prompt distance config global <paramref name="preset"/>.</summary>
    private static double PromptDistance(ScriptCache cache, string preset)
    {
        const string Config = LootContainerBlueprintMembers.PromptDistanceConfig;
        ScriptGlobal global = cache.GlobalsIn(Config).SingleOrDefault(row => row.Name == preset)
            ?? throw new InvalidDataException($"No single prompt distance preset {Config}::{preset} in the script cache");
        ScriptFunction initializer = global.Initializer ?? throw new InvalidDataException($"{Config}::{preset} has no initializer");
        // The constructor's arguments are pushed last first.
        double[] pushed = [.. ScriptDisassembler.Disassemble(initializer).Where(instruction => instruction.Name == "PshC8")
            .Select(instruction => BitConverter.Int64BitsToDouble((long)instruction.QuadWord)).Reverse()];
        string[] members = [.. cache.Class(Config).ReflectedProperties.Select(property => property.Name)];
        int index = Array.IndexOf(members, LootContainerBlueprintMembers.PromptDistance);
        return pushed.Length == ConfigConstants && members.Length == ConfigConstants && index >= 0
            ? pushed[index]
            : throw new InvalidDataException($"{Config}::{preset}: {pushed.Length} literals for the members {string.Join(", ", members)}");
    }

    private static ScriptClassDefaults Straight(ScriptCache cache, string name)
    {
        ScriptClassDefaults defaults = ScriptClassDefaults.Of(cache, name);
        return defaults is { IsComplete: true, HasBranches: false }
            ? defaults
            : throw new InvalidDataException($"{name}: the constructors and default statements are not straight-line code");
    }

    /// <summary>Calls of method <paramref name="method"/> on the member at <paramref name="offset"/>, in code order.</summary>
    private static IEnumerable<ScriptCall> MemberCalls(ScriptClassDefaults defaults, int offset, string method)
    {
        return defaults.Calls.Where(call => call.Name == method && call.Object is ScriptMember { Owner: ScriptThis, IsObject: false } member && member.Offset == offset);
    }

    private static ScriptConstant Stored(ScriptCache cache, ScriptClassDefaults defaults, string type, string member, int size)
    {
        return Optional(cache, defaults, type, member, size) ?? throw new InvalidDataException($"{type}.{member} has no literal default");
    }

    /// <summary>A member's default: the literal stored last, or <see langword="null"/> (the type's zero) when none is stored.</summary>

    private static ScriptConstant? Optional(ScriptCache cache, ScriptClassDefaults defaults, string type, string member, int size)
    {
        ScriptConstant? constant = TryOffset(cache, type, member) is { } offset ? defaults.Constant(offset) : null;
        return constant is null || constant.Size == size
            ? constant
            : throw new InvalidDataException($"{type}.{member}: a {constant.Size}-byte default, expected {size} bytes");
    }

    /// <summary>Byte offset of <paramref name="member"/>, declared by <paramref name="type"/> or one of its script super classes.</summary>
    /// <exception cref="InvalidDataException">No class of the chain declares the member, or the bytecode never refers to it.</exception>
    private static int Offset(ScriptCache cache, string type, string member)
    {
        return TryOffset(cache, type, member) ?? throw new InvalidDataException($"{type}.{member}: the bytecode never refers to it");
    }

    /// <summary>Byte offset of <paramref name="member"/>, or <see langword="null"/> when the bytecode never refers to it.</summary>
    /// <exception cref="InvalidDataException">No class of the chain declares the member.</exception>
    private static int? TryOffset(ScriptCache cache, string type, string member)
    {
        foreach (ScriptClass owner in cache.Chain(type, out _))
        {
            if (owner.ReflectedProperties.Any(property => property.Name == member))
            {
                return cache.MemberOffsets(owner.Name).TryGetValue(member, out int offset) ? offset : null;
            }
        }

        throw new InvalidDataException($"No class of the chain of {type} declares {member}");
    }
}

/// <summary>Number conventions of the loot tables.</summary>
internal static class LootNumbers
{
    /// <summary>
    /// A float the game stores as the double with the same 7 significant digits (0.2, not 0.20000000298…), the way the
    /// tables give every float member.
    /// </summary>
    public static double Single(float value) => double.Parse(value.ToString("G7", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
