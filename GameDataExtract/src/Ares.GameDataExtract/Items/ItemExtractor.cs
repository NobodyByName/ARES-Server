using System.Collections.Immutable;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.GameDataExtract.Items;

/// <summary>The global item tables (see <c>GameTables</c>).</summary>
internal sealed record ItemTables(
    ImmutableArray<ItemDefinition> UsableItems,
    ImmutableArray<ItemValue> ItemValues,
    ImmutableArray<string> WorldItemList,
    ImmutableArray<AugmentDefinition> Augments,
    ImmutableArray<LoadoutFrameDefinition> LoadoutFrames,
    ImmutableArray<FirearmDefinition> Firearms,
    ImmutableArray<ShieldDefinition> Shields,
    MenuItemTables Menu);

/// <summary>
/// Extracts the global item tables from the installed game: the usable-item catalogue, the item value cache, the
/// world item class list, augments, loadout frames, firearm and shield classes, and the menu item definitions.
/// </summary>
internal static class ItemExtractor
{
    /// <summary>Extracts the tables.</summary>
    /// <param name="packages">The game's packages.</param>
    /// <param name="registry">The game's asset registry.</param>
    /// <param name="layouts">The script layouts.</param>
    /// <exception cref="InvalidDataException">An asset the tables need does not decode or breaks a rule the tables rely on.</exception>
    public static ItemTables Extract(PackageCache packages, AssetRegistry registry, ScriptLayouts layouts)
    {
        ItemSources sources = new(packages, registry, layouts);
        ItemClasses classes = new(sources, sources.AssetsOf(InventoryStructures.ModSlotClass).Select(asset => asset.PackageName));
        ImmutableArray<ItemAsset> items = UsableItemReader.Read(sources, classes);
        InventoryStructures structures = new(sources, items);
        ImmutableArray<FirearmDefinition> firearms = ItemDefinitionReaders.Firearms(sources, classes, structures, items);
        return new ItemTables(
            [.. items.Select(item => item.Row)],
            ItemValueReader.Read(sources),
            WorldItemListReader.Read(packages),
            ItemDefinitionReaders.Augments(sources, structures, items),
            ItemDefinitionReaders.LoadoutFrames(structures),
            firearms,
            ItemDefinitionReaders.Shields(sources, classes, items),
            MenuItems.Build(items, structures, firearms));
    }
}
