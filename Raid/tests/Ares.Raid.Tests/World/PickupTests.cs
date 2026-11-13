using Ares.Common.Game.Inventory;
using Ares.Raid.Replication;
using Ares.Raid.World;
using Ares.Raid.World.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class PickupTests
{
    private static readonly ushort s_duck = checked((ushort)LoadoutDefinitions.UsableIndex(StorageItems.RubberDuckAssetId));
    private static readonly int s_maxDucks = RaidInventory.MaxAmount(s_duck);

    [Fact]
    public void Every_item_a_player_can_drop_has_a_data_asset_and_hand_items_stay_in_the_inventory()
    {
        Assert.All(LoadoutDefinitions.All.Where(item => LoadoutDefinitions.SupportsCarriedItem(item.GameAssetId)), item =>
        {
            ushort index = checked((ushort)LoadoutDefinitions.UsableIndex(item.GameAssetId));
            Assert.True(WorldPickups.Supports(index));
            Assert.True(RaidInventory.MaxAmount(index) > 0);
        });

        RaidInventory inventory = new(new PlayerLoadout(43, 0, []));
        Assert.False(inventory.Apply(Drop(inventory.Loadout, InventoryBlackboard.UnarmedSlot), out string? rejection));
        Assert.Equal("not droppable", rejection);
        Assert.False(inventory.TryTakeDropped(out _));
    }

    [Fact]
    public void A_dropped_stack_leaves_the_inventory_and_appears_ahead_of_the_character_for_its_client()
    {
        ScriptedPlayer session = new();
        Vector3D at = session.Member.Spawn.Location;
        (uint frame, double time) = Ready(session, at);
        ContainerItem ducks = Give(session, Ducks(4));
        int frames = session.Channels.BlackboardFrames;
        session.Sent.Clear();

        session.Input(frame, frame + 3, position: at, inventory: Drop(session.Member.Inventory.Backpack, Slot(session, ducks)));
        Assert.Null(session.Member.Inventory.Containers[session.Member.Inventory.Backpack].Items[0]);
        WorldPickup pickup = Assert.Single(session.Raid.Pickups.All);
        Assert.Equal(ducks, pickup.Item);
        Vector3D reported = Assert.IsType<Vector3D>(session.Channels.InputPosition);
        // The reported position is at the feet: the item appears ahead along the facing, at the capsule centre's height.
        double heading = pickup.Rotation.Yaw * Math.PI / 180;
        Vector3D ahead = new(
            Math.Round(reported.X + (Pickups.DropDistance * Math.Cos(heading))),
            Math.Round(reported.Y + (Pickups.DropDistance * Math.Sin(heading))),
            Math.Round(reported.Z + DamPlayerStarts.Capsule.HalfHeight));
        Assert.Equal(ahead, pickup.Location);
        Assert.Equal((session.Principal.UserId, 0, 0.0, 0.0), (pickup.DroppedBy, pickup.DroppingPlayerId, pickup.Rotation.Pitch, pickup.Rotation.Roll));
        Assert.Equal(session.Raid.WorldSeconds(time - 0.3), pickup.DroppedAt, 6);
        Assert.Equal(1, session.Channels.ItemsDropped);
        Assert.StartsWith("drop #0 item ", Assert.Single(session.Channels.PickupEvents), StringComparison.Ordinal);

        Stand(session, frame + 3, time, at, 2);

        // The client's inventory loses the item, so it does not come back.
        Assert.True(session.Channels.BlackboardFrames > frames);
        Assert.True(session.Channels.HasPickup(pickup.Index));
        Assert.Equal(0, session.Channels.PickupChannelsInUse);

        // Class default and data asset exported, spawned with its properties, components named, interaction idle, dormant.
        ChannelBunch exports = Assert.Single(session.Sent, bunch => bunch.PackageExports && ExportRoots(bunch).Contains(pickup.DataAsset));
        Assert.Equal([session.Raid.Pickups.Archetype, pickup.DataAsset], ExportRoots(exports));
        ChannelBunch open = Assert.Single(session.Sent, bunch => Spawns(bunch, pickup));
        PickupProperties properties = OpenProperties(session, open, pickup);
        Assert.Equal(new PickupProperties
        {
            Role = NetRole.Authority,
            RemoteRole = NetRole.SimulatedProxy,
            Movement = new RepMovement(pickup.Location, pickup.Rotation),
            DataAsset = pickup.DataAsset,
            Item = new PickupItem { DataAssetIndex = s_duck, Quality = 0, Amount = 4, AmmoInClip = 0, Guid = ducks.Id },
            DropServerWorldTime = pickup.DroppedAt,
            DroppingPlayerId = 0,
        }, properties);
        Assert.Contains(session.Sent, bunch => bunch.PackageExports && ExportRoots(bunch).SequenceEqual([pickup.Interaction, pickup.RootCollider]));
        Assert.Equal((ulong)CurrentInteractionState.Idle, Interaction(LastOn(session, open.Channel, bunch => bunch.CloseReason is null && !bunch.Opened), pickup)[PropertyNames.CurrentInteractionState]);
        Assert.Equal(Bunches.Dormancy, session.Sent.Last(bunch => bunch.Channel == open.Channel).CloseReason);

        // The client only sends channel bookkeeping there.
        session.Channels.Receive(session.Io, new ChannelBunch(open.Channel, [], 0) { Name = ChannelName.Actor });
    }

    [Fact]
    public void Part_of_a_stack_drops_as_a_new_item_and_the_rest_stays()
    {
        ScriptedPlayer session = new();
        Vector3D at = session.Member.Spawn.Location;
        (uint frame, _) = Ready(session, at);
        ContainerItem ducks = Give(session, Ducks(5));

        session.Input(frame, frame + 3, position: at, inventory: Drop(session.Member.Inventory.Backpack, Slot(session, ducks), 2));
        Assert.Equal(ducks with { Amount = 3 }, session.Member.Inventory.Containers[session.Member.Inventory.Backpack].Items[Slot(session, ducks)]);
        WorldPickup pickup = Assert.Single(session.Raid.Pickups.All);
        Assert.Equal((s_duck, 2), (pickup.Item.AssetIndex, pickup.Item.Amount));
        Assert.NotEqual(ducks.Id, pickup.Item.Id);
    }

    [Fact]
    public void A_drop_merges_into_the_same_players_nearby_stack_and_the_client_gets_the_new_amount()
    {
        ScriptedPlayer session = new();
        Vector3D at = session.Member.Spawn.Location;
        (uint frame, double time) = Ready(session, at);
        ContainerItem ducks = Give(session, Ducks(5));
        int slot = Slot(session, ducks);
        session.Input(frame, frame + 3, position: at, inventory: Drop(session.Member.Inventory.Backpack, slot, 2));
        (frame, time) = Stand(session, frame + 3, time, at, 2);
        WorldPickup first = Assert.Single(session.Raid.Pickups.All);
        Assert.True(session.Channels.HasPickup(first.Index));
        session.Sent.Clear();

        Vector3D near = at with { X = at.X + Pickups.MergeDistance - 10 };
        session.Input(frame, frame + 3, position: near, inventory: Drop(session.Member.Inventory.Backpack, slot, 3));
        WorldPickup merged = Assert.Single(session.Raid.Pickups.All);
        Assert.Equal((5, 1, first.Location, first.Item.Id), (merged.Item.Amount, merged.Revision, merged.Location, merged.Item.Id));
        Assert.Null(session.Member.Inventory.Containers[session.Member.Inventory.Backpack].Items[slot]);
        Assert.StartsWith("merge #0 ", session.Channels.PickupEvents[^1], StringComparison.Ordinal);

        // The dormant pickup opens again with its whole state and goes dormant once more.
        Stand(session, frame + 3, time, near, 2);
        ChannelBunch reopen = Assert.Single(session.Sent, bunch => Spawns(bunch, merged));
        Assert.Equal(5, OpenProperties(session, reopen, merged).Item!.Amount);
        Assert.Equal(Bunches.Dormancy, session.Sent.Last(bunch => bunch.Channel == reopen.Channel).CloseReason);
    }

    [Fact]
    public void Drops_out_of_range_of_another_player_or_of_items_that_hardly_stack_do_not_merge()
    {
        ScriptedPlayer session = new();
        WorldPickups pickups = session.Raid.Pickups;
        Vector3D at = session.Member.Spawn.Location;
        WorldPickup first = pickups.Drop(1, 0, Ducks(2), at, default, 1);
        Assert.Same(pickups.Drop(1, 0, Ducks(2), at with { Y = at.Y + Pickups.MergeDistance }, default, 2), pickups.All[first.Index]);
        Assert.Equal(4, pickups.All[first.Index].Item.Amount);

        Assert.NotEqual(first.Index, pickups.Drop(1, 0, Ducks(2), at with { Y = at.Y + Pickups.MergeDistance + 1 }, default, 3).Index);
        Assert.NotEqual(first.Index, pickups.Drop(2, 1, Ducks(2), at, default, 4).Index);
        Assert.NotEqual(first.Index, pickups.Drop(1, 0, Ducks(s_maxDucks - 3), at, default, 5).Index);

        const ushort Ferro = WeaponItems.FerroIUsableIndex;
        Assert.True(RaidInventory.MaxAmount(Ferro) <= Pickups.MergeStackSizeThreshold);
        WorldPickup rifle = pickups.Drop(1, 0, new ContainerItem(Ferro, Guid.NewGuid(), 1, 0.5), at, default, 6);
        Assert.NotEqual(rifle.Index, pickups.Drop(1, 0, new ContainerItem(Ferro, Guid.NewGuid(), 1, 1), at, default, 7).Index);
        Assert.Equal(6, pickups.All.Count);
        Assert.Equal(6, pickups.All.Select(pickup => pickup.Guid).Distinct().Count());
        Assert.All(pickups.All, pickup => Assert.Same(pickup, pickups.ByTarget(pickup.Interaction)));
        Assert.Null(pickups.ByTarget(first.RootCollider));
    }

    [Fact]
    public void Picking_up_a_whole_stack_fills_the_inventory_floats_it_to_the_picker_and_destroys_it_a_second_later()
    {
        ScriptedPlayer session = new();
        Vector3D at = session.Member.Spawn.Location;
        (uint frame, double time) = Ready(session, at);
        ContainerItem ducks = Give(session, Ducks(4));
        session.Input(frame, frame + 3, position: at, inventory: Drop(session.Member.Inventory.Backpack, Slot(session, ducks)));
        (frame, time) = Stand(session, frame + 3, time, at, 2);
        WorldPickup pickup = Assert.Single(session.Raid.Pickups.All);
        session.Sent.Clear();

        session.Input(frame, frame + 3, pickup.Interaction, pickup.Interaction, at);
        Assert.Equal(ducks, session.Member.Inventory.Containers[session.Member.Inventory.Backpack].Items[0]);
        WorldPickup taken = session.Raid.Pickups.All[pickup.Index];
        Assert.Equal((0, session.Player.Pawn, false), (taken.Item.Amount, taken.PickedUpBy, taken.Destroyed));
        Assert.Equal(1, session.Channels.PickupsTaken);

        // Woken with the picker, no amount, no movement, and the interaction succeeded; then dormant again.
        (frame, time) = Stand(session, frame + 3, time, at, 0.6);
        ChannelBunch wake = Assert.Single(session.Sent, bunch => Spawns(bunch, taken));
        PickupProperties properties = OpenProperties(session, wake, taken);
        Assert.Equal((false, 0, session.Player.Pawn), (properties.ReplicateMovement, properties.Item!.Amount, properties.PickedUpBy));
        Assert.Equal((ulong)CurrentInteractionState.SuccessDisabled,
            Interaction(LastOn(session, wake.Channel, bunch => bunch.CloseReason is null && !bunch.Opened), taken)[PropertyNames.CurrentInteractionState]);
        Assert.True(session.Channels.HasPickup(pickup.Index));
        session.Sent.Clear();

        // A second later the actor is destroyed: the client's dormant channel opens and closes naming only the actor.
        Stand(session, frame, time, at, 1);
        Assert.True(session.Raid.Pickups.All[pickup.Index].Destroyed);
        Assert.StartsWith("destroy #0 ", session.Channels.PickupEvents[^1], StringComparison.Ordinal);
        ChannelBunch destroy = Assert.Single(session.Sent, bunch => bunch.CloseReason == Bunches.Destroyed);
        Assert.True(destroy.Opened);
        BitReader reader = new(destroy.Payload, destroy.PayloadBits);
        Assert.Equal(pickup.Guid, NetGuid.Read(reader));
        Assert.Equal(0, reader.Remaining);
        Assert.False(session.Channels.HasPickup(pickup.Index));
    }

    [Fact]
    public void With_a_full_backpack_only_what_fits_is_picked_up_and_the_rest_stays()
    {
        ScriptedPlayer session = new();
        Vector3D at = session.Member.Spawn.Location;
        (uint frame, double time) = Ready(session, at);
        while (session.Member.Inventory.PickUp(Ducks(s_maxDucks)) == s_maxDucks)
        {
        }

        int backpack = session.Member.Inventory.Backpack;
        session.Input(frame, frame + 3, position: at, inventory: Drop(backpack, 0, 3));
        session.Input(frame + 3, frame + 6, position: at, inventory: Drop(backpack, 1, 2));
        WorldPickup pickup = Assert.Single(session.Raid.Pickups.All);
        Assert.Equal(5, pickup.Item.Amount);
        Assert.Equal(3, session.Member.Inventory.PickUp(Ducks(3)));
        (frame, time) = Stand(session, frame + 6, time, at, 2);
        session.Sent.Clear();

        session.Input(frame, frame + 3, pickup.Interaction, pickup.Interaction, at);
        WorldPickup rest = session.Raid.Pickups.All[pickup.Index];
        Assert.Equal((3, (ulong?)null, 2), (rest.Item.Amount, rest.PickedUpBy, rest.Revision));
        Assert.Equal(s_maxDucks, session.Member.Inventory.Containers[backpack].Items[1]!.Amount);
        Assert.EndsWith("2 of item " + s_duck + " x5 by 42, 3 left", session.Channels.PickupEvents[^1], StringComparison.Ordinal);

        Stand(session, frame + 3, time, at, 3);
        Assert.False(session.Raid.Pickups.All[pickup.Index].Destroyed);
        Assert.Equal(3, OpenProperties(session, Assert.Single(session.Sent, bunch => Spawns(bunch, rest)), rest).Item!.Amount);
        Assert.True(session.Channels.HasPickup(pickup.Index));
    }

    [Fact]
    public void The_first_of_two_players_grabbing_a_pickup_gets_it_and_the_other_sees_it_destroyed()
    {
        ScriptedPlayer first = new();
        ScriptedPlayer second = new(first, 43);
        Vector3D at = first.Member.Spawn.Location;
        Vector3D other = second.Member.Spawn.Location;
        (uint frame, double time) = Ready(first, at);
        (uint otherFrame, time) = Ready(second, other, time);
        ContainerItem ducks = Give(first, Ducks(4));
        first.Input(frame, frame + 3, position: at, inventory: Drop(first.Member.Inventory.Backpack, Slot(first, ducks)));
        WorldPickup pickup = Assert.Single(first.Raid.Pickups.All);
        frame += 3;
        // Until both clients have it and it went dormant for the other.
        for (int step = 0; step < 20 && !(first.Channels.HasPickup(pickup.Index) && second.Channels.HasPickup(pickup.Index) && second.Channels.PickupChannelsInUse == 0); step++)
        {
            first.Input(frame, frame + 3, position: at);
            first.Service(time);
            second.Input(otherFrame, otherFrame + 3, position: other);
            second.Service(time);
            (frame, otherFrame, time) = (frame + 3, otherFrame + 3, time + 0.3);
        }

        Assert.True(first.Channels.HasPickup(pickup.Index));
        Assert.True(second.Channels.HasPickup(pickup.Index));
        uint version = second.Member.Inventory.Version;

        first.Input(frame, frame + 3, pickup.Interaction, pickup.Interaction, at);
        second.Input(otherFrame, otherFrame + 3, pickup.Interaction, pickup.Interaction, other);
        Assert.Equal(first.Player.Pawn, first.Raid.Pickups.All[pickup.Index].PickedUpBy);
        Assert.Equal((1, 0), (first.Channels.PickupsTaken, second.Channels.PickupsTaken));
        Assert.Equal(version, second.Member.Inventory.Version);
        Assert.EndsWith("by 43 ignored: already taken", first.Channels.PickupEvents[^1], StringComparison.Ordinal);

        // The other client gets the pickup state, then the destroy.
        second.Sent.Clear();
        for (int step = 0; step < 8; step++)
        {
            (otherFrame, time) = (otherFrame + 3, time + 0.3);
            second.Input(otherFrame, otherFrame + 3, position: other);
            second.Service(time);
        }

        Assert.Equal(0, second.Channels.InputDecodeFailures);
        Assert.Equal(first.Player.Pawn, OpenProperties(second, Assert.Single(second.Sent, bunch => Spawns(bunch, pickup)), pickup).PickedUpBy);
        Assert.Contains(second.Sent, bunch => bunch.CloseReason == Bunches.Destroyed);
        Assert.False(second.Channels.HasPickup(pickup.Index));
    }

    [Fact]
    public void A_pickup_is_relevant_within_its_net_cull_distance_until_it_is_picked_up()
    {
        ScriptedPlayer session = new();
        WorldPickups pickups = session.Raid.Pickups;
        Vector3D at = session.Member.Spawn.Location;
        WorldPickup pickup = pickups.Drop(1, 0, Ducks(2), at, default, 1);
        Assert.Same(pickup, Assert.Single(pickups.Relevant(at with { X = at.X + Pickups.NetCullDistance })));
        Assert.Empty(pickups.Relevant(at with { X = at.X + Pickups.NetCullDistance + 1 }));

        Assert.Equal(2, pickups.PickUp(pickup.Index, 1, session.Member.Inventory, session.Player.Pawn, 10));
        Assert.Empty(pickups.Relevant(at));
        pickups.Expire(10 + WorldPickups.DestroySeconds - 0.01);
        Assert.False(pickups.All[pickup.Index].Destroyed);
        pickups.Expire(10 + WorldPickups.DestroySeconds);
        Assert.True(pickups.All[pickup.Index].Destroyed);
    }

    [Fact]
    public void An_open_pickup_channel_gets_only_what_changed()
    {
        ScriptedPlayer session = new();
        WorldPickups pickups = session.Raid.Pickups;
        WorldPickup pickup = pickups.Drop(1, 0, Ducks(2), session.Member.Spawn.Location, default, 1);
        pickups.PickUp(pickup.Index, 1, session.Member.Inventory, session.Player.Pawn, 10);
        WorldPickup taken = pickups.All[pickup.Index];

        ChannelBunch update = pickups.UpdateBunch(taken, 300);
        BitReader reader = new(update.Payload, update.PayloadBits);
        ActorContentBlock actor = ActorContentBlock.Read(reader);
        Dictionary<ulong, string> objects = new() { [session.Player.Pawn] = PlayerClasses.Pawn.Class };
        PickupProperties properties = PickupProperties.FromValues(new PropertyCodec(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects))
            .Decode(new PropertyLayout(GameReplication.Catalog, Pickups.ActorClass), actor, swapActorRoles: false));
        Assert.Equal(new PickupProperties { ReplicateMovement = false, Item = new PickupItem { Amount = 0 }, PickedUpBy = session.Player.Pawn }, properties);
        StableSubobjectContent interaction = StableSubobjectContent.Read(reader);
        Assert.Equal(pickup.Interaction, interaction.Guid);
        Assert.Equal(new Dictionary<string, object?> { [PropertyNames.CurrentInteractionState] = (ulong)CurrentInteractionState.SuccessDisabled },
            new PropertyCodec(new ValueCodec(GameReplication.Catalog, [DamDoors.InteractionState])).Decode(new PropertyLayout(GameReplication.Catalog, Pickups.InteractionClass), interaction.Content));
        Assert.Equal(0, reader.Remaining);
    }

    private static ContainerItem Ducks(int amount) => new(s_duck, Guid.NewGuid(), amount, 1);

    private static InventoryCommand Drop(int container, int slot, int amount = 0)
    {
        ItemContainerSlot from = new(new ItemContainerHandle(container, 0), checked((ushort)slot));
        return new InventoryCommand(amount == 0 ? InventoryCommandType.DropItem : InventoryCommandType.DropPartialItem, from, from, amount, new ItemContainerHandle(-1, ushort.MaxValue));
    }

    // Puts an item into the player's backpack, as a pickup would.
    private static ContainerItem Give(ScriptedPlayer session, ContainerItem item)
    {
        Assert.Equal(item.Amount, session.Member.Inventory.PickUp(item));
        return item;
    }

    private static int Slot(ScriptedPlayer session, ContainerItem item) =>
        session.Member.Inventory.Containers[session.Member.Inventory.Backpack].Items.ToList().FindIndex(slot => slot?.Id == item.Id);

    // Possession and settled frames at a place, then standing there until map actors may go to the client; returns
    // the next input frame and service time.
    private static (uint Frame, double At) Ready(ScriptedPlayer session, Vector3D at, double from = 100)
    {
        session.Possess();
        (uint frame, double time) = session.Settle(30, from, at);
        return Stand(session, frame, time, at, 16);
    }

    // Standing inputs at a place, each followed by a service 0.3 s later, for some seconds.
    private static (uint Frame, double At) Stand(ScriptedPlayer session, uint frame, double at, Vector3D position, double seconds)
    {
        for (double end = at + seconds; at < end; at += 0.3)
        {
            session.Input(frame, frame + 3, position: position);
            session.Service(at);
            frame += 3;
        }

        return (frame, at);
    }

    private static IReadOnlyList<ulong> ExportRoots(ChannelBunch bunch) => ObjectExports.Read(new BitReader(bunch.Payload, bunch.PayloadBits)).Roots;

    // Whether a bunch opens a channel spawning (or naming again) the pickup.
    private static bool Spawns(ChannelBunch bunch, WorldPickup pickup)
    {
        if (!bunch.Opened || bunch.CloseReason is not null || !bunch.MustBeMapped)
        {
            return false;
        }

        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        MustMapGuids.Read(reader);
        return NetGuid.Read(reader) == pickup.Guid;
    }

    private static ChannelBunch LastOn(ScriptedPlayer session, int channel, Func<ChannelBunch, bool> match) =>
        session.Sent.Last(bunch => bunch.Channel == channel && match(bunch));

    // The pickup's properties in the bunch that opens its channel.
    private static PickupProperties OpenProperties(ScriptedPlayer session, ChannelBunch bunch, WorldPickup pickup)
    {
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        Assert.Equal([session.Raid.Pickups.Archetype, pickup.DataAsset], MustMapGuids.Read(reader).Guids);
        BitWriter spawn = new();
        new ActorSpawn(pickup.Guid, session.Raid.Pickups.Archetype, 0, pickup.Location, pickup.Rotation).Write(spawn);
        Assert.Equal(spawn.ToArray(), BitReaderBytes(reader, spawn.Length));
        ActorContentBlock block = ActorContentBlock.Read(reader);
        Assert.Equal(0, reader.Remaining);
        Dictionary<ulong, string> objects = new() { [pickup.DataAsset] = Pickups.DataAssetClass, [session.Player.Pawn] = PlayerClasses.Pawn.Class };
        foreach (WorldPlayer player in session.Raid.Players.Select(member => session.Raid.State.FindPlayer(member.Id)).OfType<WorldPlayer>())
        {
            objects[player.Pawn] = PlayerClasses.Pawn.Class;
        }

        return PickupProperties.FromValues(new PropertyCodec(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects))
            .Decode(new PropertyLayout(GameReplication.Catalog, Pickups.ActorClass), block, swapActorRoles: false));
    }

    // The pickup interaction's state in a bunch on its open channel.
    private static Dictionary<string, object?> Interaction(ChannelBunch bunch, WorldPickup pickup)
    {
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        StableSubobjectContent component = StableSubobjectContent.Read(reader);
        Assert.Equal(pickup.Interaction, component.Guid);
        Dictionary<ulong, string> components = new() { [pickup.RootCollider] = Pickups.RootColliderClass };
        return new PropertyCodec(new ValueCodec(GameReplication.Catalog, [DamDoors.InteractionState], components))
            .Decode(new PropertyLayout(GameReplication.Catalog, Pickups.InteractionClass), component.Content);
    }

    private static byte[] BitReaderBytes(BitReader reader, long bits)
    {
        BitWriter copy = new();
        copy.WriteBits(reader.ReadBitArray(bits), bits);
        return copy.ToArray();
    }
}
