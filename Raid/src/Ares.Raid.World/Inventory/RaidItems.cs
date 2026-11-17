using Ares.Common.Game.Content;
using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Raid.World.Inventory;

/// <summary>
/// The item rules and item facts of a raid server: the shared item definitions and traits, the items the raid handles
/// by name, and the carried items by usable index. Built once from the loaded game data and the ARES content and passed
/// to what needs it.
/// </summary>
public sealed class RaidItems
{
    /// <summary>Builds the item rules from loaded game data and ARES content.</summary>
    /// <param name="data">The game data.</param>
    /// <param name="content">The ARES content.</param>
    /// <exception cref="AresContentException">The content does not fit the game data.</exception>
    public RaidItems(GameDataSet data, AresContent content)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(content);
        ResolvedGameNames names = data.Names;
        Definitions = content.ItemDefinitions(data);
        LoadoutBranches = content.Loadout.Branches;
        Traits = new ItemTraitsTable(data, Definitions);
        Unarmed = names.Unarmed;
        RaiderTool = names.RaiderTool;
        FerroI = names.FerroI;
        FerroIFirearm = names.FerroIFirearm;
        HeavyAmmo = names.HeavyAmmo;
        SnapHook = names.SnapHook;
        LightShield = names.LightShieldClass;
        UnarmedIndex = checked((ushort)Unarmed.UsableIndex);
        RaiderToolIndex = checked((ushort)RaiderTool.UsableIndex);
        FerroIIndex = checked((ushort)FerroI.UsableIndex);
        HeavyAmmoIndex = checked((ushort)HeavyAmmo.UsableIndex);
        SnapHookIndex = checked((ushort)SnapHook.UsableIndex);
        RubberDuckIndex = checked((ushort)Definitions.UsableIndex(Definitions.AssetIds[ItemTechnicalNames.RubberDuck]));
        HeldItemClasses = Traits.All.Where(traits => traits.HeldClass is not null)
            .ToDictionary(traits => checked((ushort)traits.UsableIndex), traits => traits.HeldClass!);
        Carried = Definitions.CarriedItems.ToDictionary(item => checked((ushort)item.UsableIndex));
        UnitWeights = Definitions.CarriedItems.ToDictionary(item => checked((ushort)item.UsableIndex), item => item.Weight);
        MaxAmounts = Definitions.All.Where(item => Definitions.SupportsCarriedItem(item.GameAssetId))
            .ToDictionary(item => checked((ushort)Definitions.UsableIndex(item.GameAssetId)), item => checked((int)item.MaxAmount));
        AssetIds = Definitions.All.Where(item => Definitions.SupportsCarriedItem(item.GameAssetId))
            .ToDictionary(item => checked((ushort)Definitions.UsableIndex(item.GameAssetId)), item => item.GameAssetId);
    }

    /// <summary>The shared item definitions.</summary>
    public LoadoutDefinitions Definitions { get; }

    /// <summary>The standard loadout frame's branch of each equipment place.</summary>
    public LoadoutBranches LoadoutBranches { get; }

    /// <summary>The traits of the handled items.</summary>
    public ItemTraitsTable Traits { get; }

    /// <summary>The empty-handed item.</summary>
    public ItemDefinition Unarmed { get; }

    /// <summary>The Raider Tool.</summary>
    public ItemDefinition RaiderTool { get; }

    /// <summary>The Ferro I.</summary>
    public ItemDefinition FerroI { get; }

    /// <summary>The Ferro I's firearm class.</summary>
    public FirearmDefinition FerroIFirearm { get; }

    /// <summary>Heavy Ammo, the Ferro's ammunition.</summary>
    public ItemDefinition HeavyAmmo { get; }

    /// <summary>The Snap Hook.</summary>
    public ItemDefinition SnapHook { get; }

    /// <summary>The Light Shield's shield class.</summary>
    public ShieldDefinition LightShield { get; }

    /// <summary>Usable index of the empty-handed item.</summary>
    public ushort UnarmedIndex { get; }

    /// <summary>Usable index of the Raider Tool.</summary>
    public ushort RaiderToolIndex { get; }

    /// <summary>Usable index of the Ferro I.</summary>
    public ushort FerroIIndex { get; }

    /// <summary>Usable index of Heavy Ammo.</summary>
    public ushort HeavyAmmoIndex { get; }

    /// <summary>Usable index of the Snap Hook.</summary>
    public ushort SnapHookIndex { get; }

    /// <summary>Usable index of the rubber duck, the placeholder of a container that drew nothing.</summary>
    public ushort RubberDuckIndex { get; }

    /// <summary>Rounds the Ferro I's magazine holds.</summary>
    public int FerroClipSize => (int)FerroIFirearm.ClipSize;

    /// <summary>The Ferro I's full reload time, in seconds: its base reload time with the modifier of quality 0.</summary>
    public double FerroReloadSeconds => FerroIFirearm.BaseReloadSeconds + FerroIFirearm.ReloadModifierSeconds[0];

    /// <summary>Number of mod slots on the Ferro I.</summary>
    public int FerroModSlotCount => FerroIFirearm.ModSlots.Count;

    /// <summary>Shield health of a full Light Shield.</summary>
    public double ShieldMaxHealth => LightShield.MaxArmorPlates * LightShield.ArmorPerPlate;

    /// <summary>Shield health of one Light Shield plate.</summary>
    public double ShieldHealthPerPlate => LightShield.ArmorPerPlate;

    /// <summary>Gameplay classes of the items a character can hold, by usable index, in the order the raid exports them.</summary>
    public IReadOnlyDictionary<ushort, string> HeldItemClasses { get; }

    /// <summary>The items players can carry in a raid (and so drop), by usable index.</summary>
    public IReadOnlyDictionary<ushort, ItemDefinition> Carried { get; }

    /// <summary>Unit weight of each carried item, by usable index.</summary>
    public IReadOnlyDictionary<ushort, double> UnitWeights { get; }

    /// <summary>Maximum stack size of each carried item, by usable index; items without one, such as hand items, never stack.</summary>
    public IReadOnlyDictionary<ushort, int> MaxAmounts { get; }

    /// <summary>Asset id of each carried item by its usable index, for reporting carried items as Host item records.</summary>
    public IReadOnlyDictionary<ushort, long> AssetIds { get; }

    /// <summary>The maximum stack size of an item; 0 for items that never stack or leave the inventory, such as hand items.</summary>
    /// <param name="assetIndex">Index of the item in the usable item table.</param>
    /// <returns>The maximum amount of one stack.</returns>
    public int MaxAmount(ushort assetIndex) => MaxAmounts.GetValueOrDefault(assetIndex);
}
