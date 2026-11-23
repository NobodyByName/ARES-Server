using Ares.Common.Game.Inventory;
using Ares.Raid.World.Actors;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World;

public sealed class DoorUnlockTests
{
    private const int DoorChannelBase = 64;

    [Fact]
    public void A_locked_door_with_unlock_items_unlocks_once_and_then_opens()
    {
        DoorStates states = new(
        [
            new DoorPlacement("cell", "keyed") { StartAsLocked = true, UnlockItems = [new DoorUnlockItem("/Game/Key.Key_C", 1, true)] },
            new DoorPlacement("cell", "keyless") { StartAsLocked = true },
        ]);
        Assert.Equal(new DoorButtons(CurrentInteractionState.Disabled, CurrentInteractionState.Disabled, CurrentInteractionState.Idle), states.Buttons(0));
        Assert.False(states.Press(0, 1.0));

        Assert.True(states.Unlock(0));
        Assert.False(states.Unlock(0));
        Assert.Equal((DoorLockState.Unlocked, 1), (states.Get(0).State.Lock, states.Get(0).Revision));
        Assert.Equal(new DoorButtons(CurrentInteractionState.Idle, CurrentInteractionState.Disabled, CurrentInteractionState.SuccessDisabled), states.Buttons(0));
        Assert.True(states.Press(0, 2.0));

        Assert.False(states.Unlock(1));
        Assert.Equal(CurrentInteractionState.Disabled, states.Buttons(1).Unlock);
    }

    [Fact]
    public void An_armed_area_fires_while_a_player_is_inside_and_resets_after_the_last_one_left()
    {
        (ExploitAreaStates areas, DoorStates doors) = Trap(resetSeconds: 10);
        Vector3D inside = new(1050, 0, 100);
        Vector3D outside = new(1150, 0, 100);

        areas.Advance(1.0, [outside]);
        Assert.Equal(ExploitPreventionAreaState.Idle, areas.Get(0));
        areas.Advance(2.0, [outside, inside]);
        Assert.Equal(ExploitPreventionAreaState.Active, areas.Get(0));
        areas.Advance(5.0, [outside]);
        Assert.Equal(ExploitPreventionAreaState.Active, areas.Get(0));
        areas.Advance(12.0, []);
        Assert.Equal(ExploitPreventionAreaState.Idle, areas.Get(0));

        areas.Advance(13.0, [inside]);
        Assert.Equal(CurrentInteractionState.Disabled, doors.Buttons(0).Unlock);
        Assert.False(doors.Unlock(0));
        areas.Advance(23.0, []);
        Assert.Equal(CurrentInteractionState.Idle, doors.Buttons(0).Unlock);
        Assert.True(doors.Unlock(0));
        Assert.Equal(ExploitPreventionAreaState.Disabled, areas.Get(0));
        areas.Advance(24.0, [inside]);
        Assert.Equal(ExploitPreventionAreaState.Disabled, areas.Get(0));
    }

    [Fact]
    public void A_reset_time_of_zero_turns_the_trap_off()
    {
        (ExploitAreaStates areas, _) = Trap(resetSeconds: 0);
        areas.Advance(1.0, [new Vector3D(1050, 0, 100)]);
        Assert.Equal(ExploitPreventionAreaState.Idle, areas.Get(0));
    }

    [Fact]
    public void A_firing_room_cannot_be_unlocked_until_its_fire_is_out()
    {
        Door keyed = new ScriptedPlayer().Raid.Doors.Doors.First(door => door.Unlock is not null);
        DoorKey key = Assert.Single(keyed.Keys);
        ItemDefinition item = TestGameData.Data.Items.All.First(candidate => key.AssetIndexes.Contains((ushort)candidate.UsableIndex));
        ScriptedPlayer session = new(loadout: WithBackpackItem(InventoryTests.StandardLoadout(), item.AssetId));
        Door locked = OpenKeyedDoor(session);
        ExploitArea area = session.Raid.Doors.Areas.Single(candidate => candidate.Door == locked);
        Vector3D inside = area.Placement.Triggers[0].Center;
        Vector3D away = inside with { X = inside.X + 100000 };

        session.Input(30, 33, position: inside);
        session.Service(124.0);
        session.Input(33, 36, locked.Unlock!.Value, position: inside);
        Assert.Equal(ExploitPreventionAreaState.Active, session.Raid.ExploitAreas.Get(area.Index));
        Assert.Equal(CurrentInteractionState.Disabled, session.Raid.DoorStates.Buttons(locked.Index).Unlock);
        Assert.Equal((0, DoorLockState.Locked, 1), (session.Channels.DoorUnlocks, session.Raid.DoorStates.Get(locked.Index).State.Lock, session.Member.Inventory.Carrying(key.AssetIndexes)));

        session.Input(36, 39, position: away);
        session.Service(125.0);
        session.Service(125.0 + TestGameData.Raid.Settings.ExploitTrapResetSeconds + 1);
        Assert.Equal(ExploitPreventionAreaState.Idle, session.Raid.ExploitAreas.Get(area.Index));

        session.Input(39, 42, locked.Unlock!.Value, position: away);
        session.Service(137.0);
        Assert.Equal(1, session.Channels.DoorUnlocks);
        Assert.Equal(ExploitPreventionAreaState.Disabled, session.Raid.ExploitAreas.Get(area.Index));
    }

    [Fact]
    public void Dam_trap_boxes_cover_a_secure_room_but_not_its_antechamber()
    {
        IReadOnlyList<ExploitArea> areas = new ScriptedPlayer().Raid.Doors.Areas;
        Assert.Contains(areas, area => area.Contains(new Vector3D(498936, 590147, -41200)));
        Assert.DoesNotContain(areas, area => area.Contains(new Vector3D(497813, 590221, -41200)));
        Assert.DoesNotContain(areas, area => area.Contains(new Vector3D(498086, 589171, -41200)));
    }

    // One locked door whose area has a 4 m by 2 m box turned 90 degrees, centred 10 m along X.
    private static (ExploitAreaStates Areas, DoorStates Doors) Trap(double resetSeconds)
    {
        DoorPlacement placement = new("cell", "keyed") { StartAsLocked = true, UnlockItems = [new DoorUnlockItem("/Game/Key.Key_C", 1, true)], ExploitPreventionArea = "area" };
        DoorStates doors = new([placement]);
        Door door = new(0, "cell", 1, [2, 3], 4, 5, [], 6);
        ExploitPreventionAreaPlacement area = new("cell", "area") { Triggers = [new ExploitTriggerBox(new Vector3D(1000, 0, 100), new Rotator(0, 90, 0), new Vector3D(200, 100, 200))] };
        return (new ExploitAreaStates([new ExploitArea(0, door, 6, area)], doors, resetSeconds), doors);
    }

    [Fact]
    public void Unlocking_without_the_key_keeps_the_door_locked()
    {
        ScriptedPlayer session = new(loadout: InventoryTests.StandardLoadout());
        Door locked = OpenKeyedDoor(session);

        session.Input(30, 33, locked.Unlock!.Value);

        Assert.Equal((0, 1, 1), (session.Channels.DoorUnlocks, session.Channels.MissingDoorKeys, session.Channels.IgnoredDoorRequests));
        Assert.Equal(DoorLockState.Locked, session.Raid.DoorStates.Get(locked.Index).State.Lock);
    }

    [Fact]
    public void Unlocking_with_the_key_takes_it_and_lets_the_door_open()
    {
        Door keyed = new ScriptedPlayer().Raid.Doors.Doors.First(door => door.Unlock is not null);
        DoorKey key = Assert.Single(keyed.Keys);
        ItemDefinition item = TestGameData.Data.Items.All.First(candidate => key.AssetIndexes.Contains((ushort)candidate.UsableIndex));
        ScriptedPlayer session = new(loadout: WithBackpackItem(InventoryTests.StandardLoadout(), item.AssetId));
        Door locked = OpenKeyedDoor(session);
        Assert.Equal(1, session.Member.Inventory.Carrying(key.AssetIndexes));

        int sent = session.Sent.Count;
        session.Input(30, 33, locked.Unlock!.Value);
        session.Service(123.5);

        Assert.Equal((1, 0), (session.Channels.DoorUnlocks, session.Channels.MissingDoorKeys));
        Assert.Equal(DoorLockState.Unlocked, session.Raid.DoorStates.Get(locked.Index).State.Lock);
        Assert.Equal(key.Consume ? 0 : 1, session.Member.Inventory.Carrying(key.AssetIndexes));
        Assert.Equal(4, session.Member.Inventory.Carrying(new HashSet<ushort> { (ushort)TestGameData.RubberDuck.UsableIndex }));
        Assert.Contains(session.Sent.Skip(sent), bunch => bunch.Channel == DoorChannelBase + locked.Index);
        Assert.Contains(session.Sent.Skip(sent), bunch => bunch.Channel == AreaChannel(session, locked));

        session.Input(33, 36, locked.Buttons[0]);
        Assert.Equal(1, session.Channels.DoorPresses);
    }

    // Possesses the character and replicates the first locked door with unlock items.
    private static Door OpenKeyedDoor(ScriptedPlayer session)
    {
        Door locked = session.Raid.Doors.Doors.First(door => door.Unlock is not null);
        session.Possess();
        session.ReportVisible((locked.Cell, true, false));
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0])
        {
            session.Service(at);
        }

        Assert.Contains(session.Sent, bunch => bunch is { Opened: true } && bunch.Channel == DoorChannelBase + locked.Index);
        Assert.Contains(session.Sent, bunch => bunch is { Opened: true } && bunch.Channel == AreaChannel(session, locked));
        return locked;
    }

    private static int AreaChannel(ScriptedPlayer session, Door door) =>
        session.Raid.Channels.ExploitAreas.Channel(session.Raid.Doors.Areas.Single(area => area.Door == door).Index);

    // Places one item of asset assetId in the standard backpack's sixth slot.
    private static PlayerLoadout WithBackpackItem(PlayerLoadout loadout, long assetId)
    {
        InventoryItem branch = loadout.Items.Single(item => TestGameData.Items.Definitions.StandardBranch(item.GameAssetId) == TestGameData.Loadout.Branches.Backpack);
        InventoryItem backpack = loadout.Items.Single(item => item.InstanceId == branch.Slots[0]);
        string target = backpack.Slots[5];
        InventoryItem added = new(Guid.NewGuid().ToString("D"), assetId, 1, [], 0, 0, "", "key", 0);
        return loadout with { Items = [.. loadout.Items.Select(item => item.InstanceId == target ? item with { Slots = [added.InstanceId] } : item), added] };
    }
}
