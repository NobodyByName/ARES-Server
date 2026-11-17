using System.Collections.Immutable;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Loot;

namespace Ares.GameDataExtract.Loot;

/// <summary>The global loot definition tables (see <see cref="LootDefinitionExtractor"/>).</summary>
/// <param name="ContainerTypes">The registered container types, by tag.</param>
/// <param name="ContainerClasses">The container classes that decode, by class path (ordinal).</param>
/// <param name="UndecodedClasses">The container classes that do not decode, by class path, with the reason.</param>
/// <param name="ContainerDefaults">Defaults every container class shares.</param>
/// <param name="ValueTables">The value loot tables, by package.</param>
/// <param name="SkillTables">The skill loot tables, by package.</param>
/// <param name="Tweakables">The static loot spawn manager's tweakables.</param>
internal sealed record LootDefinitionData(
    ImmutableArray<LootContainerType> ContainerTypes,
    ImmutableArray<LootContainerClass> ContainerClasses,
    ImmutableSortedDictionary<string, string> UndecodedClasses,
    LootContainerDefaults ContainerDefaults,
    ImmutableArray<StaticValueLootTable> ValueTables,
    ImmutableArray<SkillLootTable> SkillTables,
    LootSpawnTweakables Tweakables);

/// <summary>
/// Extracts the loot definitions from the installed game: the container types of the registered loot container data
/// assets, the container classes those types and the maps name, the value and skill loot tables, the container defaults
/// and the spawn manager's tweakables.
/// </summary>
internal static class LootDefinitionExtractor
{
    /// <summary>
    /// Extracts the tables. The container classes are every class the registered types name and every container Blueprint
    /// class the map packages <paramref name="mapPackages"/> import (placed containers and spawner class lists).
    /// </summary>
    /// <exception cref="InvalidDataException">A data asset, table or script default does not decode.</exception>
    public static LootDefinitionData Extract(PackageCache packages, AssetRegistry registry, ScriptCache cache, IEnumerable<string> mapPackages)
    {
        ScriptSchemas schemas = new(cache, new Dictionary<string, StructSchema>(StringComparer.Ordinal)
        {
            [LootContainerBlueprintMembers.ComponentSelectionStruct] = LootContainerBlueprintMembers.ComponentSelection,
        });
        LootContainerTypeData types = LootContainerTypeReader.Read(packages, registry, cache, schemas);
        SortedSet<string> classPaths = new(types.Classes, StringComparer.Ordinal);
        classPaths.UnionWith(MapContainerClasses(packages, mapPackages));
        LootContainerClassReader reader = new(packages, cache, schemas);
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

        LootTableReader tables = new(packages, registry, cache, schemas);
        return new LootDefinitionData(
            [.. types.Types],
            [.. classes],
            undecoded.ToImmutableSortedDictionary(StringComparer.Ordinal),
            new LootContainerDefaults(LootScriptDefaults.NetCullDistance(cache), LootScriptDefaults.TargetStacks(cache)),
            [.. tables.ValueTables()],
            [.. tables.SkillTables()],
            LootScriptDefaults.Tweakables(cache));
    }

    /// <summary>The container Blueprint classes (<see cref="LootContainerBlueprintMembers.BlueprintPrefixes"/>) the packages import.</summary>
    private static IEnumerable<string> MapContainerClasses(PackageCache packages, IEnumerable<string> mapPackages)
    {
        foreach (string name in mapPackages)
        {
            CookedPackage package = packages.Load(name) ?? throw new InvalidDataException($"{name} is not in the game archives");
            foreach (string imported in package.Header.ImportedPackageNames)
            {
                string leaf = imported[(imported.LastIndexOf('/') + 1)..];
                if (LootContainerBlueprintMembers.BlueprintPrefixes.Any(prefix => leaf.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    yield return $"{imported}.{leaf}_C";
                }
            }
        }
    }
}
