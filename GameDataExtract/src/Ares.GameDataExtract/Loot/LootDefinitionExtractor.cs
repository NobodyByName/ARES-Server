using System.Collections.Immutable;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Scripts;

namespace Ares.GameDataExtract.Loot;

/// <summary>The global loot definition tables (see <see cref="LootDefinitionExtractor"/>).</summary>
/// <param name="ContainerTypes">The registered container types, by tag.</param>
/// <param name="ContainerClasses">The container classes that decode, by class path (ordinal).</param>
/// <param name="UndecodedClasses">The container classes that do not decode, by class path, with the reason.</param>
/// <param name="ValueTables">The value loot tables, by package.</param>
/// <param name="SkillTables">The skill loot tables, by package.</param>
internal sealed record LootDefinitionData(
    ImmutableArray<LootContainerType> ContainerTypes,
    ImmutableArray<LootContainerClass> ContainerClasses,
    ImmutableSortedDictionary<string, string> UndecodedClasses,
    ImmutableArray<StaticValueLootTable> ValueTables,
    ImmutableArray<SkillLootTable> SkillTables);

/// <summary>
/// Extracts the loot definitions from the installed game: the container types of the registered loot container data
/// assets, the container classes those types and the maps name, and the value and skill loot tables.
/// </summary>
internal static class LootDefinitionExtractor
{
    /// <summary>
    /// Extracts the tables. The container classes are every class the registered types name and every container Blueprint
    /// class the map packages <paramref name="mapPackages"/> import (placed containers and spawner class lists). Blocks
    /// are read with the chains of <paramref name="layouts"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">A data asset or table does not decode.</exception>
    public static LootDefinitionData Extract(PackageCache packages, AssetRegistry registry, ScriptLayouts layouts, IEnumerable<string> mapPackages)
    {
        ScriptTypeSchemas schemas = layouts.Schemas(new Dictionary<string, StructSchema>(StringComparer.Ordinal)
        {
            [LootContainerBlueprintMembers.ComponentSelectionStruct] = LootContainerBlueprintMembers.ComponentSelection,
        });
        LootContainerTypeData types = LootContainerTypeReader.Read(packages, registry, layouts, schemas);
        SortedSet<string> classPaths = new(types.Classes, StringComparer.Ordinal);
        classPaths.UnionWith(MapContainerClasses(packages, mapPackages));
        LootContainerClassReader reader = new(packages, layouts, schemas);
        List<LootContainerClass> classes = [];
        SortedDictionary<string, string> undecoded = new(StringComparer.Ordinal);
        foreach (string path in classPaths)
        {
            try
            {
                classes.Add(reader.Read(path));
            }
            catch (InvalidDataException error)
            {
                undecoded[path] = error.Message;
            }
        }

        LootTableReader tables = new(packages, registry, layouts, schemas);
        return new LootDefinitionData(
            [.. types.Types],
            [.. classes],
            undecoded.ToImmutableSortedDictionary(StringComparer.Ordinal),
            [.. tables.ValueTables()],
            [.. tables.SkillTables()]);
    }

    /// <summary>The container Blueprint classes (<see cref="LootContainerBlueprintMembers.BlueprintPrefixes"/>) the packages import.</summary>
    private static IEnumerable<string> MapContainerClasses(PackageCache packages, IEnumerable<string> mapPackages) => mapPackages
        .SelectMany(name => (packages.Load(name) ?? throw new InvalidDataException($"{name} is not in the game archives")).Header.ImportedPackageNames)
        .Select(imported => (Imported: imported, Leaf: imported[(imported.LastIndexOf('/') + 1)..]))
        .Where(entry => LootContainerBlueprintMembers.BlueprintPrefixes.Any(prefix => entry.Leaf.StartsWith(prefix, StringComparison.Ordinal)))
        .Select(entry => $"{entry.Imported}.{entry.Leaf}_C");
}
