using Ares.Common.Game.Loot;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Loot;
using Ares.Raid.World.Players;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class LootRevealTests
{
    private const int MenuClosed = PendulumInput.LootingMenuClosedRequest | PendulumInput.LastInteractionSuccessfulFlag;

    // Leeway around a due reveal: just before it nothing changes, just after it the reveal is done.
    private const double Early = 0.01;
    private const double Late = 1e-6;

    [Fact]
    public void Opening_hides_the_items_and_reveals_one_a_second_after_the_containers_delay()
    {
        ScriptedPlayer session = new();
        LootContainer container = LootTests.Cabinet(session);
        (uint frame, double at) = LootTests.Reach(session, container);
        double delay = container.RevealDelaySeconds(session.Raid.GameData.LootRules);
        Assert.Equal(container.Class.OpenDelaySeconds, delay);
        Assert.True(delay > 0);
        (_, double opened) = Open(session, container, frame, at);

        // Every occupied slot hides its item: the first is being revealed, the others wait; hidden slots send no item.
        int[] occupied = Occupied(session);
        Assert.True(occupied.Length >= 2);
        Assert.Equal(Hidden(session, occupied, 0), Statuses(session));
        PendulumArrayValue slots = Assert.IsType<PendulumArrayValue>(session.Member.Inventory.Blackboard.Values.Single(value => value.Field == InventoryBlackboard.SlotsField));
        ulong[] hidden = [.. slots.Elements.Where(element => element.Values.Any(value => value.Field == InventoryBlackboard.SearchStatusField))
            .Select(element => Assert.IsType<PendulumIntegerValue>(Assert.Single(element.Values)).Value)];
        Assert.Equal([(ulong)ItemSearchStatus.HiddenSearching, .. Enumerable.Repeat((ulong)ItemSearchStatus.Hidden, occupied.Length - 1)], hidden);

        // The first reveal comes after the delay and one reveal time, then one every reveal time, lowest slot first.
        for (int count = 1; count <= occupied.Length; count++)
        {
            double due = opened + delay + (count * ItemRevealTime.Default.Seconds);
            uint version = session.Member.Inventory.Version;
            session.Service(due - Early);
            Assert.Equal(Hidden(session, occupied, count - 1), Statuses(session));
            session.Service(due + Late);
            Assert.Equal(Hidden(session, occupied, count), Statuses(session));
            Assert.Equal(version + 1, session.Member.Inventory.Version);
        }
    }

    [Fact]
    public void Closing_mid_search_and_reopening_reveals_only_the_rest_after_the_delay_again()
    {
        ScriptedPlayer session = new();
        LootContainer container = LootTests.Cabinet(session);
        (uint frame, double at) = LootTests.Reach(session, container);
        double delay = container.RevealDelaySeconds(session.Raid.GameData.LootRules);
        (frame, double opened) = Open(session, container, frame, at);
        int[] occupied = Occupied(session);
        double first = opened + delay + ItemRevealTime.Default.Seconds;
        session.Service(first + Late);
        Assert.Equal(Hidden(session, occupied, 1), Statuses(session));

        frame = Close(session, container, frame, first + 0.2);
        session.Service(first + 1);
        (_, double reopened) = Open(session, container, frame, first + 2);
        Assert.Equal(Hidden(session, occupied[1..], 0), Statuses(session));
        double next = reopened + delay + ItemRevealTime.Default.Seconds;
        session.Service(next - Early);
        Assert.Equal(Hidden(session, occupied[1..], 0), Statuses(session));
        session.Service(next + Late);
        Assert.Equal(Hidden(session, occupied[1..], 1), Statuses(session));
    }

    [Fact]
    public void An_item_the_player_puts_in_is_never_hidden()
    {
        ScriptedPlayer session = new();
        LootContainer container = LootTests.Cabinet(session);
        (uint frame, double at) = LootTests.Reach(session, container);
        (frame, double opened) = Open(session, container, frame, at);
        int[] occupied = Occupied(session);
        RaidInventory inventory = session.Member.Inventory;
        int loot = Assert.IsType<int>(inventory.OpenLoot);
        Assert.Equal(1, inventory.PickUp(new ContainerItem(session.Raid.GameData.Items.RubberDuckIndex, Guid.NewGuid(), 1, 1)));
        (int from, int slot) = ((int[])[inventory.Backpack, inventory.Belt, inventory.SafePouch])
            .SelectMany(handle => Enumerable.Range(0, inventory.Containers[handle].Items.Count), (handle, index) => (Handle: handle, Index: index))
            .First(position => inventory.Containers[position.Handle].Items[position.Index] is not null);
        ContainerItem carried = inventory.Containers[from].Items[slot]!;
        int empty = Enumerable.Range(0, inventory.LootItems.Count).First(index => inventory.LootItems[index] is null);

        session.Input(frame, frame + 3, focus: container.Interaction, position: container.Prompt, inventory: new InventoryCommand(InventoryCommandType.SwapItems,
            new ItemContainerSlot(new ItemContainerHandle(from, 0), (ushort)slot), new ItemContainerSlot(new ItemContainerHandle(loot, 0), (ushort)empty), 0,
            new ItemContainerHandle(-1, ushort.MaxValue)));
        Assert.Equal(carried, inventory.LootItems[empty]);
        Assert.Equal(Hidden(session, occupied, 0), Statuses(session));

        // Closed before any reveal and opened again: the item put in shows, the others hide again.
        frame = Close(session, container, frame + 3, opened + 0.1);
        session.Service(opened + 1);
        Open(session, container, frame, opened + 2);
        Assert.Equal(carried.Id, session.Member.Inventory.LootItems[empty]?.Id);
        Assert.Equal(Hidden(session, occupied, 0), Statuses(session));
    }

    [Fact]
    public void A_faster_search_shortens_the_delay_and_each_reveal()
    {
        ScriptedPlayer session = new();
        LootContainer container = LootTests.Cabinet(session);
        (uint frame, double at) = LootTests.Reach(session, container);
        session.Member.LootReveal = new LootRevealModifiers(DurationMultiplier: 0.5);
        double delay = container.RevealDelaySeconds(session.Raid.GameData.LootRules);
        (_, double opened) = Open(session, container, frame, at);
        int[] occupied = Occupied(session);
        for (int count = 1; count <= 2; count++)
        {
            double due = opened + ((delay + (count * ItemRevealTime.Default.Seconds)) * 0.5);
            session.Service(due - Early);
            Assert.Equal(Hidden(session, occupied, count - 1), Statuses(session));
            session.Service(due + Late);
            Assert.Equal(Hidden(session, occupied, count), Statuses(session));
        }
    }

    [Fact]
    public void A_double_reveal_shows_two_items_at_once()
    {
        ScriptedPlayer session = new();
        LootContainer container = LootTests.Cabinet(session);
        (uint frame, double at) = LootTests.Reach(session, container);
        session.Member.LootReveal = new LootRevealModifiers(DoubleRevealChance: 1);
        double delay = container.RevealDelaySeconds(session.Raid.GameData.LootRules);
        (_, double opened) = Open(session, container, frame, at);
        int[] occupied = Occupied(session);
        uint version = session.Member.Inventory.Version;
        session.Service(opened + delay + ItemRevealTime.Default.Seconds + Late);
        Assert.Equal(Hidden(session, occupied, 2), Statuses(session));
        Assert.Equal(version + 1, session.Member.Inventory.Version);
    }

    [Fact]
    public void A_reveal_frame_advances_the_dirty_count_and_sends_no_container()
    {
        ScriptedPlayer session = new();
        LootContainer container = LootTests.Cabinet(session);
        (uint frame, double at) = LootTests.Reach(session, container);
        double delay = container.RevealDelaySeconds(session.Raid.GameData.LootRules);
        (frame, double opened) = Open(session, container, frame, at);
        session.Input(frame, frame + 3, focus: container.Interaction, position: container.Prompt);
        session.Service(opened + 0.05);
        frame += 3;
        PendulumField[] fields = [.. PendulumRegistry.Descriptors[InventoryBlackboard.Descriptor].Fields];
        PendulumArrayField containers = (PendulumArrayField)fields[InventoryBlackboard.ContainersField];
        Range containerBytes = containers.Offset..(containers.Offset + (containers.Capacity * containers.Stride));
        int[] occupied = Occupied(session);
        for (int count = 1; count <= occupied.Length; count++)
        {
            int sent = session.Sent.Count;
            double due = opened + delay + (count * ItemRevealTime.Default.Seconds);
            session.Service(due + Late);
            session.Input(frame, frame + 3, focus: container.Interaction, position: container.Prompt);
            session.Service(due + 0.05);
            frame += 3;

            // Only the slots and the dirty count change, so the delta carries no container element.
            (byte[] before, byte[] after) = Assert.Single(InventoryFrames(session, sent), change => !change.Before.AsSpan().SequenceEqual(change.After));
            Assert.Equal(before[containerBytes], after[containerBytes]);
            Assert.Equal(PendulumCodec.Integer(fields[InventoryBlackboard.DirtyCountField], before) + 1, PendulumCodec.Integer(fields[InventoryBlackboard.DirtyCountField], after));
            Assert.Equal((ushort)session.Member.Inventory.Version, PendulumCodec.Integer(fields[InventoryBlackboard.DirtyCountField], after));
        }
    }

    [Fact]
    public void An_operator_rule_replaces_the_containers_delay()
    {
        LootContainer container = LootTests.Cabinet(new ScriptedPlayer());
        Assert.Equal(container.Class.OpenDelaySeconds, container.RevealDelaySeconds(LootRules.None));
        LootRule byTag = new("by tag", [container.Tag!], new HashSet<string>(StringComparer.Ordinal), null, null, RevealDelaySeconds: 2.5);
        LootRule byId = new("by id", [], new HashSet<string>(StringComparer.Ordinal) { container.Id! }, null, null, RevealDelaySeconds: 0);
        Assert.Equal(2.5, container.RevealDelaySeconds(new LootRules([byTag])));
        Assert.Equal(0, container.RevealDelaySeconds(new LootRules([byTag, byId])));
    }

    [Fact]
    public void A_held_container_shows_ongoing_to_the_others_and_ignores_their_press_until_it_closes()
    {
        ScriptedPlayer host = new();
        ScriptedPlayer other = new(host, 43);
        LootContainer container = LootTests.Cabinet(host);
        (uint frame, double at) = LootTests.Reach(host, container);
        (uint otherFrame, double otherAt) = LootTests.Reach(other, container, at);

        host.Input(frame, frame + 3, container.Interaction, container.Interaction, container.Prompt);
        host.Service(otherAt);
        LootHold hold = Assert.IsType<LootHold>(host.Raid.LootStates.HoldOf(container.Index));
        Assert.Equal(host.Member.Id, hold.Player);

        // The other client sees the host's search.
        other.Sent.Clear();
        Dictionary<string, object?> search = OtherSearch(other, container, otherAt + 0.1);
        Assert.Equal((ulong)CurrentInteractionState.Ongoing, search[PropertyNames.CurrentInteractionState]);
        Assert.Equal(hold.HoldStartsAt, search[PropertyNames.InteractionServerHoldStartedTime]);

        // Its presses do nothing, while the host searches and while the host has the container open. Another request
        // between presses ends each one, so the next is a new press rather than a held one.
        other.Input(otherFrame, otherFrame + 3, container.Interaction, container.Interaction, container.Prompt);
        other.Service(otherAt + 0.2);
        other.Input(otherFrame + 3, otherFrame + 6, 0, container.Interaction, container.Prompt, request: PendulumInput.LastInteractionSuccessfulFlag);
        host.Input(frame + 3, frame + 6, focus: container.Interaction, position: container.Prompt);
        double opensAt = otherAt + container.Class.AlignmentSeconds + container.Class.HoldSeconds;
        host.Service(opensAt);
        Assert.NotNull(host.Member.Inventory.OpenLoot);
        other.Input(otherFrame + 6, otherFrame + 9, container.Interaction, container.Interaction, container.Prompt);
        other.Service(opensAt + 0.1);
        other.Input(otherFrame + 9, otherFrame + 12, 0, container.Interaction, container.Prompt, request: PendulumInput.LastInteractionSuccessfulFlag);
        Assert.Equal([$"{PendulumInput.PressRequest}@{otherFrame + 3}", $"{PendulumInput.PressRequest}@{otherFrame + 9}"],
            other.Channels.LootRequests.Where(line => line.StartsWith($"{PendulumInput.PressRequest}@", StringComparison.Ordinal)));
        Assert.Equal(0, other.Channels.LootSearches);
        Assert.Null(other.Member.Inventory.OpenLoot);

        // Once the host closes it, the others see it opened and may search it.
        Close(host, container, frame + 6, opensAt + 1);
        Assert.Null(host.Raid.LootStates.HoldOf(container.Index));
        search = OtherSearch(other, container, opensAt + 1.1);
        Assert.Equal((ulong)CurrentInteractionState.SuccessIdle, search[PropertyNames.CurrentInteractionState]);
        Assert.Equal(LootInteractions.NoHoldStartedTime, search[PropertyNames.InteractionServerHoldStartedTime]);
        other.Service(opensAt + 2);
        other.Input(otherFrame + 12, otherFrame + 15, container.Interaction, container.Interaction, container.Prompt);
        other.Service(opensAt + 3);
        Assert.Equal(1, other.Channels.LootSearches);
    }

    [Fact]
    public void The_containers_sound_follows_the_search_for_every_player_who_has_it()
    {
        ScriptedPlayer host = new();
        ScriptedPlayer other = new(host, 43);
        LootContainer container = LootTests.Cabinet(host);
        (uint frame, double at) = LootTests.Reach(host, container);
        (_, double start) = LootTests.Reach(other, container, at);
        double delay = container.RevealDelaySeconds(host.Raid.GameData.LootRules);
        List<ItemContainerSoundState> own = [];
        List<ItemContainerSoundState> heard = [];
        int ownChannel = -1;
        int otherChannel = -1;

        host.Input(frame, frame + 3, container.Interaction, container.Interaction, container.Prompt);
        Step(start);
        host.Input(frame + 3, frame + 6, focus: container.Interaction, position: container.Prompt);
        Step(start + container.Class.AlignmentSeconds + Early);
        double opened = start + container.Class.AlignmentSeconds + container.Class.HoldSeconds;
        Step(opened);
        int occupied = host.Member.Inventory.LootItems.Count(item => item is not null);
        Step(opened + delay + Early);
        for (int count = 1; count <= occupied; count++)
        {
            Step(opened + delay + (count * ItemRevealTime.Default.Seconds) + Early);
        }

        host.Input(frame + 6, frame + 9, 0, container.Interaction, container.Prompt, request: MenuClosed);
        Step(opened + delay + occupied + 1);
        ItemContainerSoundState[] expected =
        [
            ItemContainerSoundState.Opening, ItemContainerSoundState.Opened, ItemContainerSoundState.OpenedSearching,
            ItemContainerSoundState.Opened, ItemContainerSoundState.None,
        ];
        Assert.Equal(expected, own);
        Assert.Equal(expected, heard);
        return;

        void Step(double now)
        {
            host.Sent.Clear();
            host.Service(now);
            ownChannel = Collect(host, ownChannel, own);
            for (int service = 1; service <= 3; service++)
            {
                other.Sent.Clear();
                other.Service(now + (service * Early));
                otherChannel = Collect(other, otherChannel, heard);
            }
        }

        // Adds each new sound the container's state bunches carry; returns the container's channel on that connection.
        int Collect(ScriptedPlayer session, int channel, List<ItemContainerSoundState> sounds)
        {
            foreach (ChannelBunch bunch in session.Sent)
            {
                if (LootTests.OpensLoot(session, bunch, container))
                {
                    channel = bunch.Channel;
                }
                else if (bunch.Channel == channel && bunch.CloseReason is not null)
                {
                    channel = -1;
                }
                else if (bunch.Channel == channel && Sound(container, bunch) is { } sound && (sounds.Count == 0 || sounds[^1] != sound))
                {
                    sounds.Add(sound);
                }
            }

            return channel;
        }
    }

    [Fact]
    public void A_cancelled_hold_falls_silent_and_frees_the_container()
    {
        ScriptedPlayer session = new();
        LootContainer container = LootTests.Cabinet(session);
        (uint frame, double at) = LootTests.Reach(session, container);
        session.Sent.Clear();
        session.Input(frame, frame + 3, container.Interaction, container.Interaction, container.Prompt);
        session.Service(at);
        int channel = Assert.Single(session.Sent, bunch => LootTests.OpensLoot(session, bunch, container)).Channel;
        session.Service(at + container.Class.AlignmentSeconds + Early);
        Assert.Equal(ItemContainerSoundState.Opening, session.Raid.LootStates.HoldOf(container.Index)?.Sound);

        session.Sent.Clear();
        session.Input(frame + 3, frame + 6, focus: container.Interaction, position: container.Prompt, forward: 1);
        session.Service(at + container.Class.AlignmentSeconds + 0.5);
        Assert.Equal(1, session.Channels.CancelledSearches);
        Assert.Null(session.Raid.LootStates.HoldOf(container.Index));
        ChannelBunch state = session.Sent.Last(bunch => bunch.Channel == channel && bunch.CloseReason is null);
        Assert.Equal(ItemContainerSoundState.None, Sound(container, state));
    }

    // Presses the container's search (without drawing the raider tool) and services until it opens; returns the next
    // input frame and the opening time.
    private static (uint Frame, double OpenedAt) Open(ScriptedPlayer session, LootContainer container, uint frame, double at)
    {
        double hold = session.Raid.LootStates.OpenedAt(container.Index) is null ? container.Class.HoldSeconds : LootInteractions.OpenedHoldSeconds;
        session.Input(frame, frame + 3, container.Interaction, container.Interaction, container.Prompt);
        session.Service(at);
        session.Input(frame + 3, frame + 6, focus: container.Interaction, position: container.Prompt);
        double opensAt = at + container.Class.AlignmentSeconds + hold;
        session.Service(opensAt);
        Assert.NotNull(session.Member.Inventory.OpenLoot);
        return (frame + 6, opensAt);
    }

    // Closes the looting menu; returns the next input frame.
    private static uint Close(ScriptedPlayer session, LootContainer container, uint frame, double at)
    {
        session.Input(frame, frame + 3, 0, container.Interaction, container.Prompt, request: MenuClosed);
        session.Service(at);
        Assert.Null(session.Member.Inventory.OpenLoot);
        return frame + 3;
    }

    // The other player's latest view of the container's search after a service: the last state on the channel the
    // container last opened on (the other containers around have reached the client, so none takes that channel).
    private static Dictionary<string, object?> OtherSearch(ScriptedPlayer other, LootContainer container, double at)
    {
        other.Service(at);
        ChannelBunch open = other.Sent.Last(bunch => LootTests.OpensLoot(other, bunch, container));
        return LootTests.Search(container, other.Sent.Last(bunch => bunch.Channel == open.Channel && bunch.CloseReason is null));
    }

    // Occupied slots of the open container, lowest first.
    private static int[] Occupied(ScriptedPlayer session)
    {
        IReadOnlyList<ContainerItem?> items = session.Member.Inventory.LootItems;
        return [.. Enumerable.Range(0, items.Count).Where(slot => items[slot] is not null)];
    }

    // The container set value before and after each of the player's own pawn blackboard frames sent from the given
    // bunch on, decoded like the client: every frame since the spawn, each against the frame its header names as baseline.
    private static List<(byte[] Before, byte[] After)> InventoryFrames(ScriptedPlayer session, int from)
    {
        NetFieldTable fields = session.Raid.State.Replication.Calls().Fields(PlayerClasses.Pawn.Class);
        Dictionary<uint, Dictionary<int, PendulumSlot>> states = [];
        Dictionary<int, PendulumSlot> spawn = session.SpawnState;
        List<(byte[] Before, byte[] After)> frames = [];
        for (int index = 0; index < session.Sent.Count; index++)
        {
            ChannelBunch bunch = session.Sent[index];
            if (bunch.Channel != 6 || bunch.Opened || (bunch.Payload[0] & 3) != 2)
            {
                continue;
            }

            ActorContentBlock block = ActorContentBlock.Read(new BitReader(bunch.Payload, bunch.PayloadBits));
            ActorFieldPayload payload = ActorFieldPayload.Read(new BitReader(block.Payload, block.PayloadBits), fields.Count);
            BitReader reader = new(payload.Payload, payload.PayloadBits);
            if (fields.FieldAt(payload.Index).Name != PropertyNames.BlackBoardReplicationPtr || !reader.ReadBit())
            {
                continue;
            }

            BlackboardUpdateHeader header = BlackboardUpdate.ReadHeader(reader);
            Dictionary<int, PendulumSlot> previous = header.Reset ? [] : states.GetValueOrDefault(header.Baseline) ?? spawn;
            Dictionary<int, PendulumSlot> result = PendulumCodec.Default.ReadFrame(reader, previous);
            Assert.Equal(0, reader.Remaining);
            states[header.Latest] = result;
            if (index >= from)
            {
                frames.Add((previous[InventoryBlackboard.Slot].Value, result[InventoryBlackboard.Slot].Value));
            }
        }

        return frames;
    }

    // The open container's statuses, every slot shown when it has none.
    private static ItemSearchStatus[] Statuses(ScriptedPlayer session) =>
        session.Member.Inventory.LootSearchStatuses is { } statuses ? [.. statuses] : new ItemSearchStatus[session.Member.Inventory.LootItems.Count];

    // The statuses of a search of the given slots once some of them are revealed.
    private static ItemSearchStatus[] Hidden(ScriptedPlayer session, int[] queue, int revealed)
    {
        ItemSearchStatus[] statuses = new ItemSearchStatus[session.Member.Inventory.LootItems.Count];
        for (int position = revealed; position < queue.Length; position++)
        {
            statuses[queue[position]] = position == revealed ? ItemSearchStatus.HiddenSearching : ItemSearchStatus.Hidden;
        }

        return statuses;
    }

    // The item container's sound in a container's state bunch, or null when the bunch does not carry it.
    private static ItemContainerSoundState? Sound(LootContainer container, ChannelBunch bunch)
    {
        BitReader probe = new(bunch.Payload, bunch.PayloadBits);
        probe.ReadBit();
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        if (probe.ReadBit())
        {
            ActorContentBlock.Read(reader);
        }

        Assert.Equal(container.Interaction, StableSubobjectContent.Read(reader).Guid);
        if (reader.Remaining == 0)
        {
            return null;
        }

        StableSubobjectContent items = StableSubobjectContent.Read(reader);
        Assert.Equal(container.ItemContainer, items.Guid);
        Dictionary<string, object?> values = new PropertyCodec(new ValueCodec(GameReplication.Catalog, [KnownEnums.ItemContainerSoundState]))
            .Decode(new PropertyLayout(GameReplication.Catalog, container.ItemContainerClass), items.Content);
        return values.TryGetValue($"{PropertyNames.SoundData}.{PropertyNames.SoundState}", out object? sound) && sound is ulong value ? (ItemContainerSoundState)value : null;
    }
}
