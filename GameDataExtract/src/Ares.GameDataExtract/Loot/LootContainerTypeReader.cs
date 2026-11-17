using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.Loot;

/// <summary>The registered container types and every container class they name.</summary>
/// <param name="Types">The types, ordered by tag (ordinal).</param>
/// <param name="Classes">Every class the registered types name, material variants included (also those an asset ignores).</param>
internal sealed record LootContainerTypeData(IReadOnlyList<LootContainerType> Types, IReadOnlyCollection<string> Classes);

/// <summary>
/// Reads the loot container types (<see cref="LootContainerType"/>) from the loot container data assets the asset
/// registry lists (<see cref="LootContainerDataAssetMembers"/>): each asset's properties over the class's script
/// defaults. Only assets the repository registers outside development mode become types; two of them may not share a tag.
/// </summary>
internal static class LootContainerTypeReader
{
    /// <summary>Reads the types.</summary>
    /// <exception cref="InvalidDataException">An asset or a class it names does not decode, or two registered assets share a tag.</exception>
    public static LootContainerTypeData Read(PackageCache packages, AssetRegistry registry, ScriptCache cache, ScriptSchemas schemas)
    {
        LootContainerDataAssetDefaults defaults = LootScriptDefaults.DataAsset(cache);
        ScriptEnum availability = cache.Enum(LootContainerDataAssetMembers.AvailabilityEnum);
        StructSchema schema = schemas.ForPath(LootContainerDataAssetMembers.ClassPath)
            ?? throw new InvalidDataException($"No script class {LootContainerDataAssetMembers.ClassPath}");
        List<LootContainerType> types = [];
        HashSet<string> classes = new(StringComparer.Ordinal);
        foreach (RegistryAsset asset in LootAssets.OfClass(registry, LootContainerDataAssetMembers.ClassPath, LootContainerDataAssetMembers.AssetPrefix))
        {
            (CookedPackage package, PropertyBag bag) = LootAssets.Read(packages, asset, schema);
            byte registered = bag.Get(LootContainerDataAssetMembers.DevelopmentAvailability, defaults.DevelopmentAvailability);
            if (registered >= availability.Names.Count)
            {
                throw new InvalidDataException($"{asset.PackageName}: availability {registered}");
            }

            if (availability.Names[registered] != LootContainerDataAssetMembers.RegisteredAvailability)
            {
                continue;
            }

            string tag = LootAssets.Tag(bag.Get<PropertyBag>(LootContainerDataAssetMembers.LootContainerTag))
                ?? throw new InvalidDataException($"{asset.PackageName}: no {LootContainerDataAssetMembers.LootContainerTag}");
            ArchetypeClass[] own = Classes(packages, package, bag.Get<object[]>(LootContainerDataAssetMembers.ContainerClasses, []));
            List<LootContainerVariant> variants = [];
            foreach (KeyValuePair<object, object> pair in bag.Get<KeyValuePair<object, object>[]>(LootContainerDataAssetMembers.MaterialOverrideContainerClasses, []))
            {
                if (pair.Key is not SoftObjectPath { SubPath.Length: 0, IsNull: false } material || pair.Value is not PropertyBag variant)
                {
                    throw new InvalidDataException($"{asset.PackageName}: a material override is not a material asset and its classes");
                }

                variants.Add(new LootContainerVariant(material.Path, Classes(packages, package, variant.Get<object[]>(LootContainerDataAssetMembers.VariantContainerClasses, []))));
            }

            if (own.Length == 0)
            {
                throw new InvalidDataException($"{asset.PackageName}: no container classes");
            }

            classes.UnionWith(own.Concat(variants.SelectMany(variant => variant.Classes)).Select(type => type.Class));
            bool ignore = bag.Get(LootContainerDataAssetMembers.IgnoreMaterialOverrides, defaults.IgnoreMaterialOverrides);
            types.Add(new LootContainerType(
                tag,
                own,
                ignore ? [] : [.. variants.OrderBy(variant => variant.Material, StringComparer.Ordinal)],
                bag.Get(LootContainerDataAssetMembers.MaxContainersPerGroup, defaults.MaxContainersPerGroup)));
        }

        string? shared = types.GroupBy(type => type.Tag, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1)?.Key;
        return shared is null
            ? new LootContainerTypeData([.. types.OrderBy(type => type.Tag, StringComparer.Ordinal)], classes)
            : throw new InvalidDataException($"Two registered loot container assets serve {shared}");
    }

    private static ArchetypeClass[] Classes(PackageCache packages, CookedPackage package, object[] references)
    {
        return [.. references.Select(reference => LootAssets.BlueprintClass(packages, package, reference))];
    }
}
