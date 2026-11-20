using Ares.Common.Game.Content;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Raid.World;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests;

/// <summary>
/// The real-data tier of the Raid tests: the game data in <see cref="RealGameData.DirectoryVariable"/>, with the raid rules
/// built from it once per test run. Every accessor skips the calling test when no directory is configured, so a test
/// that touches the Dam's data is a real-data test; read it from test bodies or constructors, never from static fields.
/// </summary>
internal static class TestGameData
{
    private static readonly Lazy<RaidGameData> s_raid = new(() => new RaidGameData(RealGameData.Data, ShippedContent.Value), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The loaded game data.</summary>
    public static GameDataSet Data => RealGameData.Require();

    /// <summary>The raid rules built from <see cref="Data"/>, shared by the tests.</summary>
    public static RaidGameData Raid
    {
        get
        {
            _ = RealGameData.Require();
            return s_raid.Value;
        }
    }

    /// <summary>The loadout structure of the default ARES content.</summary>
    public static LoadoutContent Loadout => ShippedContent.Value.Loadout;

    /// <summary>The rubber duck, the plain item the tests carry.</summary>
    public static ItemDefinition RubberDuck => Data.Items.ByTechnicalName[ItemTechnicalNames.RubberDuck];

    /// <summary>The Dam's data.</summary>
    public static MapData Dam => Data.Names.Dam;

    /// <summary>The raid's item rules.</summary>
    public static RaidItems Items => Raid.Items;

    /// <summary>The loot definitions.</summary>
    public static LootData Loot => Data.Loot;

    /// <summary>The Dam's loot placement limiters with the shipped spawn settings' defaults for what they leave unset.</summary>
    public static IReadOnlyList<LootLimiter> DamLimiters => [.. Dam.LootLimiters.Select(Raid.LootSpawn.LimiterDefaults.Apply)];

    /// <summary>The most containers per group of a container tag's type, with the shipped spawn settings' default.</summary>
    /// <param name="tag">The container tag.</param>
    /// <returns>The group cap.</returns>
    public static int GroupCap(string tag) => Loot.FindContainerType(tag)!.MaxPerGroup ?? Raid.LootSpawn.MaxPerGroup;

    /// <summary>The graph state tag bits.</summary>
    public static GraphTagBits Tags => Data.Names.GraphTags;

    /// <summary>The graph state flag groups.</summary>
    public static GraphStateFlags Flags => Data.Names.GraphFlags;

    /// <summary>Seconds from a press on an elevator's call button to its arrival (<see cref="ExtractionStates.CallSeconds"/>).</summary>
    public static double CallSeconds => new ExtractionStates(Dam, Raid.Settings, 0).CallSeconds;

    /// <summary>The menu asset id of a catalogue item, for theory data that names items by their technical name.</summary>
    /// <param name="technicalName">The item's technical name, or null.</param>
    /// <returns>Its asset id, or null for null.</returns>
    public static long? AssetId(string? technicalName) => technicalName is null ? null : Data.Items.ByTechnicalName[technicalName].AssetId;

    /// <summary>The bit mask of a graph state tag in its tag word, for theory data that names tags.</summary>
    /// <param name="tag">The tag name (<see cref="Structs.Game.GameNames.GraphTags"/>), or null for none.</param>
    /// <returns>The mask, or 0 for null.</returns>
    public static ulong TagMask(string? tag) => tag is null ? 0 : Data.Movement.FindGraphTag(tag)!.Value.Mask;
}
