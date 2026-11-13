using Ares.Common.Game.Inventory;
using Ares.Raid.World;
using Ares.Raid.World.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Replication;

namespace Ares.Raid.Tests.World;

public sealed class RaidEndTests
{
    [Fact]
    public void An_extracting_player_carries_home_the_loadout_frozen_at_the_departure_once_they_leave()
    {
        PlayerLoadout loadout = InventoryTests.StandardLoadout();
        (ScriptedPlayer session, double leaves) = DepartureTests.Departing(loadout);
        string matchId = session.Raid.MatchId;
        Assert.Null(session.Worlds.Result(matchId, 42));
        session.Service(leaves + 0.05);
        Assert.True(session.Member.Departed);
        Assert.Null(session.Worlds.Result(matchId, 42));

        // Nothing that changes the inventory after the departure changes what the player carries home.
        RaidInventory inventory = session.Member.Inventory;
        Assert.True(inventory.Apply(Move(inventory.Backpack, 3, inventory.SafePouch, 0)));
        session.Channels.Closed();
        session.Worlds.Leave(matchId, [42]);

        PlayerRaidEnd result = Assert.IsType<PlayerRaidEnd>(session.Worlds.Result(matchId, 42));
        Assert.Equal(RaidOutcomes.Extracted, result.Outcome);
        InventoryItem duck = loadout.Items.Single(item => item.GameAssetId == StorageItems.RubberDuckAssetId);
        CarriedItem carried = Assert.Single(result.Items);
        Assert.Equal((CarriedPlaces.Backpack, 3, duck.InstanceId, 4L), (carried.Place, carried.Slot, carried.Item.InstanceId, carried.Item.Amount));
    }

    [Fact]
    public void A_surrendering_player_carries_home_only_the_safe_pocket_items()
    {
        ScriptedPlayer session = new(loadout: InventoryTests.StandardLoadout());
        session.Possess();
        RaidInventory inventory = session.Member.Inventory;
        Assert.True(inventory.Apply(Move(inventory.Backpack, 3, inventory.SafePouch, 0, 1)));
        ContainerItem pocket = inventory.Containers[inventory.SafePouch].Items[0]!;

        session.Call(FunctionNames.ServerSurrender, []);
        session.Flush("surrender");
        Assert.Null(session.Worlds.Result(session.Raid.MatchId, 42));
        session.Channels.Closed();

        PlayerRaidEnd result = Assert.IsType<PlayerRaidEnd>(session.Worlds.Result(session.Raid.MatchId, 42));
        Assert.Equal(RaidOutcomes.Lost, result.Outcome);
        CarriedItem carried = Assert.Single(result.Items);
        Assert.Equal((CarriedPlaces.SafePockets, 0, pocket.Id.ToString("D"), 1L), (carried.Place, carried.Slot, carried.Item.InstanceId, carried.Item.Amount));
    }

    [Fact]
    public void A_player_whose_connection_closed_without_surrender_has_lost_once_their_reservation_ends()
    {
        ScriptedPlayer session = new(loadout: InventoryTests.StandardLoadout());
        session.Possess();
        RaidInventory inventory = session.Member.Inventory;
        Assert.True(inventory.Apply(Move(inventory.Backpack, 3, inventory.SafePouch, 0)));
        string matchId = session.Raid.MatchId;

        session.Channels.Closed();
        Assert.False(session.Worlds.HasLeft(matchId, 42));
        Assert.Null(session.Worlds.Result(matchId, 42));
        session.Worlds.Leave(matchId, [42]);

        PlayerRaidEnd result = Assert.IsType<PlayerRaidEnd>(session.Worlds.Result(matchId, 42));
        Assert.Equal(RaidOutcomes.Lost, result.Outcome);
        Assert.Equal([(CarriedPlaces.SafePockets, 0, 4L)], result.Items.Select(item => (item.Place, item.Slot, item.Item.Amount)));
        Assert.Null(session.Worlds.Result("another-match", 42));
    }

    [Fact]
    public void A_player_whose_reservation_ends_before_they_connect_has_no_result()
    {
        ScriptedPlayer first = new();
        first.Possess();
        string matchId = first.Raid.MatchId;
        first.Worlds.Join(matchId, [43]);

        first.Worlds.Leave(matchId, [43]);

        Assert.True(first.Worlds.HasLeft(matchId, 43));
        Assert.False(first.Raid.Player(43)!.Connected);
        Assert.Null(first.Worlds.Result(matchId, 43));
        Assert.True(first.Member.Connected);
        Assert.Null(first.Worlds.Result(matchId, 42));
    }

    // Moves a whole stack, or splits off an amount, from one slot to another.
    private static InventoryCommand Move(int fromContainer, ushort fromSlot, int toContainer, ushort toSlot, int amount = 0) =>
        new(amount == 0 ? InventoryCommandType.SwapItems : InventoryCommandType.SplitItems,
            new ItemContainerSlot(new ItemContainerHandle(fromContainer, 0), fromSlot),
            new ItemContainerSlot(new ItemContainerHandle(toContainer, 0), toSlot), amount, new ItemContainerHandle(-1, ushort.MaxValue));
}
