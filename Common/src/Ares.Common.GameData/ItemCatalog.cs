using System.Collections.Immutable;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.GameData;

/// <summary>
/// The usable-item catalogue and the item tables that go with it: item values and the class data of firearms, shields,
/// augments and loadout frames.
/// </summary>
public sealed class ItemCatalog
{
    internal ItemCatalog(GameDataContent content)
    {
        All = content.UsableItems;
        if (All.Select((item, index) => (item, index)).FirstOrDefault(entry => entry.item.UsableIndex != entry.index) is { item: { } misplaced } at)
        {
            throw new GameDataException($"The usable-item catalogue lists {misplaced.TechnicalName} at position {at.index} with usable index {misplaced.UsableIndex}; rows must be ordered by usable index from 0.");
        }

        ByAssetId = GameDataIndex.Unique(All.SelectMany(item => item.QualityAssetIds.Count > 0 ? item.QualityAssetIds : [item.AssetId], (item, assetId) => (assetId, item)), "usable item with asset id");
        ByTechnicalName = GameDataIndex.Unique(All.Select(item => (item.TechnicalName, item)), "usable item", StringComparer.Ordinal);
        ScrapValues = GameDataIndex.Unique(content.ItemValues.Select(value => (value.AssetId, value.ScrapValue)), "item value of asset id");
        Firearms = content.Firearms;
        FirearmsByClass = GameDataIndex.Unique(Firearms.Select(firearm => (firearm.ItemClass, firearm)), "firearm class", StringComparer.Ordinal);
        Shields = content.Shields;
        ShieldsByClass = GameDataIndex.Unique(Shields.Select(shield => (shield.ItemClass, shield)), "shield class", StringComparer.Ordinal);
        Augments = content.Augments;
        AugmentsByTechnicalName = GameDataIndex.Unique(Augments.Select(augment => (augment.TechnicalName, augment)), "augment", StringComparer.Ordinal);
        LoadoutFrames = content.LoadoutFrames;
        LoadoutFramesByAssetId = GameDataIndex.Unique(LoadoutFrames.Select(frame => (frame.AssetId, frame)), "loadout frame with asset id");
        LoadoutFramesByTechnicalName = GameDataIndex.Unique(LoadoutFrames.Select(frame => (frame.TechnicalName, frame)), "loadout frame", StringComparer.Ordinal);
    }

    /// <summary>Number of items in the catalogue (generated: <c>UsableItems.Count</c>).</summary>
    public int Count => All.Length;

    /// <summary>Every item, by usable index (generated: <c>UsableItems.All</c>).</summary>
    public ImmutableArray<ItemDefinition> All { get; }

    /// <summary>Items by menu asset id, including every quality level's asset id (generated: <c>UsableItems.ByAssetId</c>).</summary>
    public IReadOnlyDictionary<long, ItemDefinition> ByAssetId { get; }

    /// <summary>Items by technical name (generated: <c>UsableItems.ByTechnicalName</c>).</summary>
    public IReadOnlyDictionary<string, ItemDefinition> ByTechnicalName { get; }

    /// <summary>Scrap value of each item by asset id, in the value cache's order (generated: <c>ItemValues.ScrapValues</c>).</summary>
    public IReadOnlyDictionary<long, int> ScrapValues { get; }

    /// <summary>Every firearm item class, ordered by class path.</summary>
    public ImmutableArray<FirearmDefinition> Firearms { get; }

    /// <summary>Firearm item classes by class path (<see cref="ItemDefinition.ItemClass"/>).</summary>
    public IReadOnlyDictionary<string, FirearmDefinition> FirearmsByClass { get; }

    /// <summary>Every shield item class, ordered by class path.</summary>
    public ImmutableArray<ShieldDefinition> Shields { get; }

    /// <summary>Shield item classes by class path (<see cref="ItemDefinition.ItemClass"/>).</summary>
    public IReadOnlyDictionary<string, ShieldDefinition> ShieldsByClass { get; }

    /// <summary>Every augment, by usable index.</summary>
    public ImmutableArray<AugmentDefinition> Augments { get; }

    /// <summary>Augments by technical name.</summary>
    public IReadOnlyDictionary<string, AugmentDefinition> AugmentsByTechnicalName { get; }

    /// <summary>Every loadout frame, ordered by technical name.</summary>
    public ImmutableArray<LoadoutFrameDefinition> LoadoutFrames { get; }

    /// <summary>Loadout frames by asset id.</summary>
    public IReadOnlyDictionary<long, LoadoutFrameDefinition> LoadoutFramesByAssetId { get; }

    /// <summary>Loadout frames by technical name.</summary>
    public IReadOnlyDictionary<string, LoadoutFrameDefinition> LoadoutFramesByTechnicalName { get; }
}
