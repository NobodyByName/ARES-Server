using Ares.Raid.Replication;
using Ares.Raid.World;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.Tests.World;

public sealed class WorldItemClassTests
{
    private const int ReferenceBits = 905;

    // A twelve-word bitfield alone in a game state delta, as the game's server encodes it.
    private const string ReferenceEncoding =
        "DC180004081800004000000408008001000800F0520C0000000904C642201040413104560000C214D8014004001410F41810000000010021471C" +
        "968508C07F45051821148012421138024424000001E19A88AC08293E008023000200002C0000000000000000300000000000000000000000";

    private static readonly ulong[] s_referenceWords =
    [
        0x0200002000000C04,
        0x297800040000C000,
        0x1021630204800000,
        0x6100002B0218A0A0,
        0x7A080A00022000EC,
        0x2390800080000008,
        0x8C02A2BFE00442CB,
        0x22011C08A109400A,
        0x8456444D70808000,
        0x0000010011C0001F,
        0,
        0,
    ];

    [Theory]
    [InlineData("/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C")]
    [InlineData("/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C")]
    [InlineData("/BP_SocketContainer_Wrh_Machine_M_02_DoorLeft.BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_C")]
    public void A_dam_raid_flags_its_free_standing_container_classes(string classSuffix)
    {
        LootCatalog catalog = new ScriptedPlayer(lootRules: LootSpawnRules.Dam).Raid.Loot;
        string path = Assert.Single(WorldItemList.Classes, item => item.EndsWith(classSuffix, StringComparison.Ordinal));

        Assert.True(IsFlagged(catalog.WorldItemClassBits, path));
    }

    [Fact]
    public void Every_container_class_the_raid_can_spawn_is_flagged()
    {
        LootCatalog catalog = new ScriptedPlayer(lootRules: LootSpawnRules.Dam).Raid.Loot;

        Assert.Equal(WorldItemList.WordCount, catalog.WorldItemClassBits.Count);
        Assert.Equal(12, WorldItemList.WordCount);
        Assert.All(catalog.Containers, container => Assert.True(IsFlagged(catalog.WorldItemClassBits, container.Class.Container.Class)));
        Assert.Contains(catalog.Containers, container => container.Source == LootContainerSource.Spawner);
        Assert.All(DamPlacedLoot.Containers.Where(placed => LootContainerClasses.Find(placed.Class) is not null), placed => Assert.True(IsFlagged(catalog.WorldItemClassBits, placed.Class)));
        Assert.Equal(LootContainerClasses.All.Count, catalog.WorldItemClassBits.Sum(word => (int)ulong.PopCount(word)));
    }

    [Fact]
    public void The_game_state_opens_with_the_bitfield()
    {
        ScriptedPlayer session = new();
        SpawnedWorldActor gameState = Assert.Single(session.Raid.SpawnedActors);
        Dictionary<ulong, string> classes = session.Raid.Imports.Classes.ToDictionary(entry => entry.Value, entry => entry.Key);
        ValueCodec values = new(GameReplication.Catalog, [KnownEnums.NetRole, KnownEnums.GamePhase], session.Raid.State.Objects([]), classes);
        Dictionary<string, object?> content = new PropertyCodec(values).Decode(new PropertyLayout(GameReplication.Catalog, DamMap.GameState.Class), Assert.Single(gameState.Content));

        IReadOnlyList<object?> words = Assert.IsType<IReadOnlyList<object?>>(content[PropertyNames.PossibleWorldItemClassIndexBitFields], exactMatch: false);
        Assert.Equal(session.Raid.Loot.WorldItemClassBits, words.Cast<ulong>());
    }

    [Fact]
    public void The_bitfield_encodes_like_the_reference()
    {
        ActorContentBlock block = new ScriptedPlayer().Raid.State.Replication.Encode(
            DamMap.GameState.Class,
            new Dictionary<string, object?> { [PropertyNames.PossibleWorldItemClassIndexBitFields] = s_referenceWords.Select(word => (object?)word).ToArray() });

        Assert.Equal(ReferenceBits, block.PayloadBits);
        Assert.Equal(ReferenceEncoding, Convert.ToHexString(block.Payload));
        Dictionary<string, object?> decoded = new PropertyCodec(new ValueCodec(GameReplication.Catalog, []))
            .Decode(new PropertyLayout(GameReplication.Catalog, DamMap.GameState.Class), block);
        Assert.Equal(s_referenceWords, Assert.IsType<IReadOnlyList<object?>>(decoded[PropertyNames.PossibleWorldItemClassIndexBitFields], exactMatch: false).Cast<ulong>());
    }

    private static bool IsFlagged(IReadOnlyList<ulong> words, string classPath)
    {
        int index = WorldItemList.IndexOf(classPath) ?? throw new ArgumentException($"{classPath} is not listed", nameof(classPath));
        return ((words[index / WorldItemList.WordBits] >> (index % WorldItemList.WordBits)) & 1) != 0;
    }
}
