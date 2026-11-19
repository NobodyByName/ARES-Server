using Ares.Common.Game.Content;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Loot;

namespace Ares.Raid.World;

/// <summary>
/// The game data a raid server runs on: the loaded game data with the map it runs and the item and loot rules built
/// from them and the ARES content. Built once at startup and passed to the world host; every raid reads it, none changes it.
/// </summary>
public sealed class RaidGameData
{
    /// <summary>Builds the rules from loaded game data and ARES content.</summary>
    /// <param name="data">The game data.</param>
    /// <param name="content">The ARES content.</param>
    /// <exception cref="AresContentException">The content does not fit the game data.</exception>
    public RaidGameData(GameDataSet data, AresContent content)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        ArgumentNullException.ThrowIfNull(content);
        Map = data.Names.Dam;
        Items = new RaidItems(data, content);
        LootRolls = new LootContentRolls(Items.Definitions, data.Loot);
        LootRules = content.LootRules(data, Items.Definitions);
    }

    /// <summary>The loaded game data.</summary>
    public GameDataSet Data { get; }

    /// <summary>The map the raids run: the Dam.</summary>
    public MapData Map { get; }

    /// <summary>The item rules and facts.</summary>
    public RaidItems Items { get; }

    /// <summary>How a loot container's contents are drawn.</summary>
    public LootContentRolls LootRolls { get; }

    /// <summary>The operator's loot rules: spawn chances and contents of chosen containers.</summary>
    public LootRules LootRules { get; }
}
