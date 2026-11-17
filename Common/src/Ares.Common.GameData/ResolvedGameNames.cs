using System.Runtime.CompilerServices;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

namespace Ares.Common.GameData;

/// <summary>
/// <see cref="GameNames"/> resolved against loaded game data: the rows the names stand for, from which the values the
/// generated constants held follow (for example <c>WeaponItems.FerroIAssetId</c> is <c>FerroI.AssetId</c> and
/// <c>FerroDefaults.ClipSize</c> is <c>FerroIFirearm.ClipSize</c>).
/// </summary>
public sealed class ResolvedGameNames
{
    internal ResolvedGameNames(GameData data)
    {
        Resolver resolver = new();
        ItemCatalog items = data.Items;
        Dam = resolver.One("map", GameNames.Maps.Dam, data.Maps.Where(map => map.WorldName == GameNames.Maps.Dam));
        Unarmed = resolver.Item(items, GameNames.Items.Unarmed);
        RaiderTool = resolver.Item(items, GameNames.Items.RaiderTool);
        FerroI = resolver.Item(items, GameNames.Items.FerroI);
        HeavyAmmo = resolver.Item(items, GameNames.Items.HeavyAmmo);
        SnapHook = resolver.Item(items, GameNames.Items.SnapHook);
        LightShield = resolver.Item(items, GameNames.Items.LightShield);
        Coins = resolver.Item(items, GameNames.Items.Coins);
        CombatMk1 = resolver.Item(items, GameNames.Items.CombatMk1);
        LootingMk1 = resolver.Item(items, GameNames.Items.LootingMk1);
        TacticalMk1 = resolver.Item(items, GameNames.Items.TacticalMk1);
        FerroIFirearm = resolver.Dependent("firearm class", FerroI, item => items.FirearmsByClass.GetValueOrDefault(item.ItemClass), "class of GameNames.Items.FerroI");
        LightShieldClass = resolver.Dependent("shield class", LightShield, item => items.ShieldsByClass.GetValueOrDefault(item.ItemClass), "class of GameNames.Items.LightShield");
        MuzzleModSlotAssetId = resolver.Structure(data.Menu.Weapons, GameNames.ItemStructures.MuzzleModSlot);
        UnderBarrelModSlotAssetId = resolver.Structure(data.Menu.Weapons, GameNames.ItemStructures.UnderBarrelModSlot);
        StockModSlotAssetId = resolver.Structure(data.Menu.Weapons, GameNames.ItemStructures.StockModSlot);
        AugmentSlotAssetId = resolver.Structure(data.Menu.Augments, GameNames.ItemStructures.AugmentSlot);
        AugmentContainerAssetId = resolver.Structure(data.Menu.Augments, GameNames.ItemStructures.AugmentContainer);
        AugmentContentSlotAssetId = resolver.Structure(data.Menu.Augments, GameNames.ItemStructures.AugmentContentSlot);
        StandardLoadoutFrame = resolver.One("loadout frame", GameNames.ItemStructures.StandardLoadoutFrame,
            items.LoadoutFrames.Where(frame => frame.TechnicalName == GameNames.ItemStructures.StandardLoadoutFrame));
        DoorBreachAnimation = resolver.One("interaction animation container", GameNames.InteractionAnimations.DoorBreach,
            data.Movement.InteractionAnimationContainers.Where(container => container.Path == GameNames.InteractionAnimations.DoorBreach));
        GraphTags = new ResolvedGraphTags(resolver, data.Movement);
        resolver.ThrowIfMissing(data.Source);
    }

    /// <summary>The Dam (<see cref="GameNames.Maps.Dam"/>).</summary>
    public MapData Dam { get; }

    /// <summary>The empty-handed item (generated: <c>HandItems.Unarmed…</c>).</summary>
    public ItemDefinition Unarmed { get; }

    /// <summary>The Raider Tool (generated: <c>HandItems.RaiderTool…</c>).</summary>
    public ItemDefinition RaiderTool { get; }

    /// <summary>The Ferro I (generated: <c>WeaponItems.FerroI…</c>, <c>FerroDefaults</c> item timings and class).</summary>
    public ItemDefinition FerroI { get; }

    /// <summary>The Ferro I's firearm class (generated: <c>FerroDefaults</c> firearm values).</summary>
    public FirearmDefinition FerroIFirearm { get; }

    /// <summary>Heavy Ammo (generated: <c>WeaponItems.HeavyAmmo…</c>).</summary>
    public ItemDefinition HeavyAmmo { get; }

    /// <summary>The Snap Hook (generated: <c>GadgetItems.SnapHook…</c>).</summary>
    public ItemDefinition SnapHook { get; }

    /// <summary>The Light Shield (generated: <c>ShieldItems.LightShield…</c>).</summary>
    public ItemDefinition LightShield { get; }

    /// <summary>The Light Shield's shield class (generated: <c>ShieldDefaults</c>).</summary>
    public ShieldDefinition LightShieldClass { get; }

    /// <summary>Coins (generated: <c>CurrencyItems.CoinsAssetId</c>).</summary>
    public ItemDefinition Coins { get; }

    /// <summary>The Combat Mk. 1 augment (generated: <c>AugmentItems.CombatMk1AssetId</c>).</summary>
    public ItemDefinition CombatMk1 { get; }

    /// <summary>The Looting Mk. 1 augment (generated: <c>AugmentItems.LootingMk1AssetId</c>).</summary>
    public ItemDefinition LootingMk1 { get; }

    /// <summary>The Tactical Mk. 1 augment (generated: <c>AugmentItems.TacticalMk1AssetId</c>).</summary>
    public ItemDefinition TacticalMk1 { get; }

    /// <summary>Asset id of a firearm's muzzle mod slot (generated: <c>WeaponItems.MuzzleModSlotAssetId</c>).</summary>
    public long MuzzleModSlotAssetId { get; }

    /// <summary>Asset id of a firearm's under-barrel mod slot (generated: <c>WeaponItems.UnderBarrelModSlotAssetId</c>).</summary>
    public long UnderBarrelModSlotAssetId { get; }

    /// <summary>Asset id of a firearm's stock mod slot (generated: <c>WeaponItems.StockModSlotAssetId</c>).</summary>
    public long StockModSlotAssetId { get; }

    /// <summary>Asset id of the loadout's augment slot (generated: <c>AugmentItems.AugmentSlotAssetId</c>).</summary>
    public long AugmentSlotAssetId { get; }

    /// <summary>Asset id of the augment container (generated: <c>AugmentItems.AugmentContainerAssetId</c>).</summary>
    public long AugmentContainerAssetId { get; }

    /// <summary>Asset id of the augment container's content slot (generated: <c>AugmentItems.AugmentContentSlotAssetId</c>).</summary>
    public long AugmentContentSlotAssetId { get; }

    /// <summary>The loadout frame without an augment.</summary>
    public LoadoutFrameDefinition StandardLoadoutFrame { get; }

    /// <summary>The door breach animation (generated: <c>InteractionAnimationContainers.DoorBreach</c>).</summary>
    public InteractionAnimationContainer DoorBreachAnimation { get; }

    /// <summary>The graph state tags a server reads or sets (generated: <c>GraphStateTags</c>, <c>GraphStateUpperTags</c>).</summary>
    public ResolvedGraphTags GraphTags { get; }

    /// <summary>The error for a game name the data does not have.</summary>
    internal static GameDataException Missing(string kind, string name, string member) => new(
        $"The game data has no {kind} '{name}' ({member}), which ARES needs. Run the ARES-GameDataExtract of this ARES version on the supported game build; "
        + "if the game renamed or removed it, ARES needs an update.");

    /// <summary>Resolves names, collecting every problem so that one error lists them all.</summary>
    internal sealed class Resolver
    {
        private readonly List<string> _problems = [];

        /// <summary>The only row a name stands for; records a problem and returns null when there is none or more than one.</summary>
        public T One<T>(string kind, string name, IEnumerable<T> candidates, [CallerArgumentExpression(nameof(name))] string member = "")
        {
            T[] found = [.. candidates.Take(2)];
            if (found.Length == 1)
            {
                return found[0];
            }

            _problems.Add(found.Length == 0 ? $"{kind} '{name}' ({member}) is missing" : $"{kind} '{name}' ({member}) is ambiguous");
            return default!;
        }

        /// <summary>
        /// The row an item's class names; records a problem and returns null when there is none. When the item itself is
        /// missing (null), that is the problem already recorded.
        /// </summary>
        public T Dependent<T>(string kind, ItemDefinition? item, Func<ItemDefinition, T?> find, string member)
            where T : class
        {
            if (item is null)
            {
                return null!;
            }

            if (find(item) is { } found)
            {
                return found;
            }

            _problems.Add($"{kind} '{item.ItemClass}' ({member}) is missing");
            return null!;
        }

        public ItemDefinition Item(ItemCatalog items, string name, [CallerArgumentExpression(nameof(name))] string member = "") =>
            One("usable item", name, items.All.Where(item => item.TechnicalName == name), member);

        public long Structure(MenuTable table, string name, [CallerArgumentExpression(nameof(name))] string member = "") =>
            One<MenuItem?>("inventory structure", name, table.Items.Where(item => item.TechnicalName == name), member)?.Description.GameAssetId ?? 0;

        public void ThrowIfMissing(string? source)
        {
            if (_problems.Count > 0)
            {
                string where = source is null ? "The game data" : $"The game data in {source}";
                throw new GameDataException($"{where} does not have every game name ARES needs: {string.Join("; ", _problems)}. "
                    + "Run the ARES-GameDataExtract of this ARES version on the supported game build; if the game renamed or removed them, ARES needs an update.");
            }
        }
    }
}
