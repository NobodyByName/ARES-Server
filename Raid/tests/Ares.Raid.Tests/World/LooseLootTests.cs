using Ares.Common.Game.Loot;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Loot;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class LooseLootTests
{
    private const int Seeds = 40;
    private const string NatureTag = "Socket.Loot.Nature";
    private const string AmmoTag = "Item.Drop.Type.Ammo";
    private const string HealingTag = "Item.Drop.Type.Consumable.Healing";

    private static readonly Lazy<LootSpawnRules> s_rules = new(() => LootSpawnRules.Of(TestGameData.Dam, TestGameData.Raid.LootSpawn));

    private static readonly Lazy<LooseLoot[]> s_raids = new(() => [
        .. Enumerable.Range(1, Seeds).Select(seed => LooseLoot.Roll(TestGameData.Raid.LooseRolls, TestGameData.Dam.LooseItemSockets, s_rules.Value, LootRules.None, [], seed, null)),
    ]);

    [Fact]
    public void A_dam_raid_places_about_as_many_loose_items_as_the_official_game()
    {
        double average = s_raids.Value.Average(raid => raid.Counts.Spawned);

        Assert.InRange(average, 220, 250);
        Assert.All(s_raids.Value, raid => Assert.InRange(raid.Counts.Spawned, 190, 280));
        Assert.All(s_raids.Value, raid => Assert.Equal(raid.Counts.Spawned, raid.Counts.SpawnedByLootValue.Values.Sum()));
        Assert.All(s_raids.Value, raid => Assert.Equal(raid.Counts.Sockets,
            raid.Counts.Spawned + raid.Counts.InactiveLayer + raid.Counts.NothingToHold + raid.Counts.SkippedByRoll + raid.Counts.SkippedByLimiter));

        // Nature sockets are most of the sockets but a small share of the items.
        double nature = s_raids.Value.Average(raid => raid.Placements.Count(placement => LootTags.IsTagOrBelow(placement.Socket.Tag, NatureTag)));
        Assert.InRange(nature, 5, 40);
    }

    [Fact]
    public void Loose_items_never_exceed_a_limiter_cap()
    {
        LootLimiter[] limiters = [.. TestGameData.DamLimiters.Where(limiter => limiter.Limits.Any(limit => limit.Unlimited == false && LooseItemTags.IsLooseItemTag(limit.Tag)))];
        Assert.NotEmpty(limiters);
        int reached = 0;
        foreach (LooseLoot raid in s_raids.Value)
        {
            foreach (LootLimiter limiter in limiters)
            {
                LooseItemPlacement[] inside = [.. raid.Placements.Where(placement => LootLimiterRules.IsPointInside(limiter, placement.Socket.Location))];
                foreach (LootLimit limit in limiter.Limits.Where(limit => limit.Unlimited == false))
                {
                    int count = inside.Count(placement => LootLimiterRules.Matches(limit.Tag, placement.Socket.Tag));
                    Assert.True(count <= limit.Limit, $"{limiter.Name} {limit.Tag}: {count} > {limit.Limit}");
                    reached += count == limit.Limit && count > 0 ? 1 : 0;
                }
            }
        }

        // The caps do bind somewhere, so the check is not vacuous.
        Assert.True(reached > 0);
        Assert.Contains(s_raids.Value, raid => raid.Counts.SkippedByLimiter > 0);
    }

    [Fact]
    public void Each_loose_item_is_one_stack_its_socket_may_hold_at_full_durability()
    {
        LooseItemRolls rolls = TestGameData.Raid.LooseRolls;
        foreach (LooseItemPlacement placement in s_raids.Value.SelectMany(raid => raid.Placements))
        {
            Assert.InRange(placement.Stack.Amount, 1, placement.Stack.Item.MaxStack);
            Assert.Equal(1, placement.Stack.Durability);
            Assert.True(TestGameData.Items.Carried.ContainsKey(checked((ushort)placement.Stack.Item.UsableIndex)), placement.Stack.Item.TechnicalName);
            bool fromTables = rolls.TableItems(placement.Socket.Tag).Contains(placement.Stack.Item);
            bool fromPool = rolls.Pool(placement.Socket.Tag)?.Entries.Any(entry => placement.Stack.Item.DropTags.Any(tag => LootTags.IsTagOrBelow(tag, entry.DropTag))) == true;
            Assert.True(fromTables || fromPool, $"{placement.Stack.Item.TechnicalName} on {placement.Socket.Tag}");
        }

        // Every socket appears at most once in a raid.
        Assert.All(s_raids.Value, raid => Assert.Equal(raid.Placements.Count, raid.Placements.Select(placement => placement.Socket).Distinct().Count()));

        // The supply pools give ammo and healing.
        LooseItemPlacement[] all = [.. s_raids.Value.SelectMany(raid => raid.Placements)];
        Assert.Contains(all, placement => placement.Stack.Item.DropTags.Any(tag => LootTags.IsTagOrBelow(tag, AmmoTag)));
        Assert.Contains(all, placement => placement.Stack.Item.DropTags.Any(tag => LootTags.IsTagOrBelow(tag, HealingTag)));
    }

    [Fact]
    public void Without_loose_item_chances_no_socket_gets_an_item()
    {
        LooseLoot none = LooseLoot.Roll(TestGameData.Raid.LooseRolls, TestGameData.Dam.LooseItemSockets, LootSpawnRules.EverySocket, LootRules.None, [], 1, null);

        Assert.Empty(none.Placements);
        Assert.Equal(TestGameData.Dam.LooseItemSockets.Length, none.Counts.Sockets);
    }

    [Fact]
    public void A_raid_places_its_loose_items_as_the_first_pickups_and_they_never_take_drops()
    {
        ScriptedPlayer session = new(lootRules: s_rules.Value);
        LooseLoot loose = session.Raid.LooseLoot;

        Assert.InRange(loose.Counts.Spawned, 190, 280);
        Assert.Equal(loose.Placements.Count, session.Raid.Pickups.All.Count);
        Assert.All(session.Raid.Pickups.All.Zip(loose.Placements), pair =>
        {
            (WorldPickup pickup, LooseItemPlacement placement) = pair;
            Assert.True(pickup.FromMap);
            Assert.Equal(placement.Location, pickup.Location);
            Assert.Equal(placement.Stack.Amount, pickup.Item.Amount);
            Assert.Equal((0L, 0, 0.0), (pickup.DroppedBy, pickup.DroppingPlayerId, pickup.DroppedAt));
        });
        Assert.Equal(loose.Placements.Count, session.Raid.Pickups.All.Select(pickup => pickup.Item.Id).Distinct().Count());
    }

    [Fact]
    public void A_map_pickup_opens_with_the_official_member_set_and_is_never_merged_into()
    {
        ScriptedPlayer session = new();
        WorldPickups pickups = session.Raid.Pickups;
        ushort ammo = TestGameData.Items.HeavyAmmoIndex;
        Vector3D at = new(100, 200, 300);
        WorldPickup placed = pickups.Place(new ContainerItem(ammo, Guid.NewGuid(), 12, 1) { AmmoInClip = 3 }, at, new Rotator(0, 30, 0));

        ChannelBunch open = pickups.OpenBunch(placed, 20, 0);
        BitReader reader = new(open.Payload, open.PayloadBits);
        MustMapGuids.Read(reader);
        BitWriter spawn = new();
        new ActorSpawn(placed.Guid, pickups.Archetype, 0, placed.Location, placed.Rotation).Write(spawn);
        _ = reader.ReadBitArray(spawn.Length);
        ActorContentBlock block = ActorContentBlock.Read(reader);
        Dictionary<ulong, string> objects = new() { [placed.DataAsset] = Pickups.DataAssetClass };
        PropertyLayout layout = new(GameReplication.Catalog, Pickups.ActorClass);
        Dictionary<string, object?> values = new PropertyCodec(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects)).Decode(layout, block, swapActorRoles: false);
        Assert.Equal([5, 12, 14, 17, 18, 19, 23, 27, 28, 29, 30], values.Keys.Select(path => layout.Command(path).Handle).Order());

        // A player's drop of the same item next to it starts its own pickup.
        WorldPickup dropped = pickups.Drop(0, 0, new ContainerItem(ammo, Guid.NewGuid(), 5, 1), at, new Rotator(0, 0, 0), 1);
        Assert.NotEqual(placed.Index, dropped.Index);
        Assert.Equal(12, pickups.All[placed.Index].Item.Amount);
    }
}
