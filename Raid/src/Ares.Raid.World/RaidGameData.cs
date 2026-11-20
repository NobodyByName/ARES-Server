using Ares.Common.Game;
using Ares.Common.Game.Content;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Loot;

namespace Ares.Raid.World;

/// <summary>
/// The game data a raid server runs on: the loaded game data with the map it runs, the item and loot rules built
/// from them and the ARES content, and the plain raid values of the Host's configuration. Built once at startup and
/// passed to the world host; every raid reads it, none changes it.
/// </summary>
public sealed class RaidGameData
{
    /// <summary>Builds the rules from loaded game data and ARES content.</summary>
    /// <param name="data">The game data.</param>
    /// <param name="content">The ARES content.</param>
    /// <param name="settings">The plain raid values; <see cref="RaidSettings.Default"/> by default.</param>
    /// <exception cref="AresContentException">The content does not fit the game data.</exception>
    public RaidGameData(GameDataSet data, AresContent content, RaidSettings? settings = null)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        ArgumentNullException.ThrowIfNull(content);
        Map = data.Names.Dam;
        Items = new RaidItems(data, content);
        LootSpawn = content.LootSpawn(data);
        LootContentSettings contents = content.LootContents(data);
        LootRolls = new LootContentRolls(Items.Definitions, data.Loot, contents);
        LooseRolls = new LooseItemRolls(Items.Definitions.PlainItems.Where(item => Items.Carried.ContainsKey(checked((ushort)item.UsableIndex))), data.Loot, contents);
        LootRules = content.LootRules(data, Items.Definitions);
        Settings = settings ?? RaidSettings.Default;
    }

    /// <summary>The loaded game data.</summary>
    public GameDataSet Data { get; }

    /// <summary>The map the raids run: the Dam.</summary>
    public MapData Map { get; }

    /// <summary>The item rules and facts.</summary>
    public RaidItems Items { get; }

    /// <summary>How loot containers spawn.</summary>
    public LootSpawnSettings LootSpawn { get; }

    /// <summary>How a loot container's contents are drawn.</summary>
    public LootContentRolls LootRolls { get; }

    /// <summary>What a loose item socket can hold, and how its stack is drawn.</summary>
    public LooseItemRolls LooseRolls { get; }

    /// <summary>The operator's loot rules: spawn chances and contents of chosen containers.</summary>
    public LootRules LootRules { get; }

    /// <summary>The plain raid values of the Host's configuration.</summary>
    public RaidSettings Settings { get; }
}
