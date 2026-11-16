using Ares.Raid.World.Loot;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.Loot;

/// <summary>
/// Every game name the loot defaults spell out resolves in the decoded game data, so a misspelt name cannot
/// silently leave a pool or a chance without effect.
/// </summary>
public sealed class LootDefaultNamesTests
{
    private static readonly string[] s_knownTags =
    [
        .. LootContainerTypes.All.Select(type => type.Tag),
        .. DamLootSockets.All.Select(socket => socket.ContainerTag),
    ];

    private static readonly HashSet<string> s_knownClasses =
    [
        .. LootContainerClasses.All.Select(container => container.Container.Class),
        .. LootContainerClasses.Unsupported.Keys,
        .. DamDynamicLoot.Spawners.SelectMany(spawner => spawner.Classes).Select(entry => entry.Class),
    ];

    public static TheoryData<string> SocketTags =>
    [
        .. LootSpawnDefaults.Chances.Select(entry => entry.Tag)
            .Concat(LootContentDefaults.TablePools.Select(pool => pool.Tag))
            .Concat(LootContentDefaults.TypePools.SelectMany(pool => pool.Tags))
            .Distinct(StringComparer.OrdinalIgnoreCase),
    ];

    public static TheoryData<string> TableNames =>
    [
        .. LootContentDefaults.SharedTables
            .Concat(LootContentDefaults.TablePools.SelectMany(pool => pool.Tables))
            .Distinct(StringComparer.OrdinalIgnoreCase),
    ];

    public static TheoryData<string> ClassPaths => [.. LootContentDefaults.TypePools.SelectMany(pool => pool.Classes).Distinct(StringComparer.Ordinal)];

    [Theory]
    [MemberData(nameof(SocketTags))]
    public void A_socket_tag_is_a_container_tag_or_a_parent_of_one(string tag)
    {
        Assert.True(s_knownTags.Any(known => known.Equals(tag, StringComparison.OrdinalIgnoreCase)
            || known.StartsWith(tag + ".", StringComparison.OrdinalIgnoreCase)), $"Unknown socket tag {tag}.");
    }

    [Theory]
    [MemberData(nameof(TableNames))]
    public void A_table_name_is_a_value_loot_table(string name)
    {
        Assert.True(StaticValueLootTables.All.Any(table => table.Name.Equals(name, StringComparison.OrdinalIgnoreCase)), $"Unknown loot table {name}.");
    }

    [Theory]
    [MemberData(nameof(ClassPaths))]
    public void A_type_pool_class_is_a_known_container_class(string classPath)
    {
        Assert.Contains(classPath, s_knownClasses);
    }
}
