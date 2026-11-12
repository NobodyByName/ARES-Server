using Ares.Raid.World;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class LootTests
{
    [Fact]
    public void A_loot_container_spawns_at_its_transform_from_a_loadable_class()
    {
        ScriptedPlayer session = new();
        LootCatalog catalog = session.Raid.Loot;
        LootContainer container = Assert.Single(catalog.Containers);
        Assert.Equal(DamLoot.All[0], container.Placement);

        // Socket containers are not part of the map, so the client may load their class.
        ChannelBunch exports = catalog.ExportBunch(container);
        (ObjectExports records, IReadOnlyList<ulong> roots) = ObjectExports.Read(new BitReader(exports.Payload, exports.PayloadBits));
        Assert.Equal([container.Archetype, catalog.SearchAnimation], roots);
        Assert.Equal(DamLoot.SearchAnimation.Split('.')[1], Assert.IsType<StaticObjectExport>(records.Records[catalog.SearchAnimation]).Path);
        StaticObjectExport archetype = Assert.IsType<StaticObjectExport>(records.Records[container.Archetype]);
        Assert.Equal((DamLoot.ServerCabinet.Archetype.Split('.')[1], false), (archetype.Path, archetype.NoLoad));

        ChannelBunch bunch = catalog.OpenBunch(container, 300, 1);
        Assert.True(bunch is { Opened: true, MustBeMapped: true });
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        Assert.Equal([container.Archetype], MustMapGuids.Read(reader).Guids);
        Assert.Equal((container.Guid, container.Archetype, 0UL), (NetGuid.Read(reader), NetGuid.Read(reader), NetGuid.Read(reader)));
        Assert.Equal((true, false), (reader.ReadBit(), reader.ReadBit()));
        Assert.Equal(container.Placement.Location.X, BitConverter.UInt64BitsToDouble(reader.ReadBits(64)));
        Assert.Equal(container.Placement.Location.Y, BitConverter.UInt64BitsToDouble(reader.ReadBits(64)));
        Assert.Equal(container.Placement.Location.Z, BitConverter.UInt64BitsToDouble(reader.ReadBits(64)));
        Assert.True(reader.ReadBit());
        Assert.Equal((false, true), (reader.ReadBit(), reader.ReadBit()));
        Assert.Equal(container.Placement.Rotation.Yaw, reader.ReadBits(16) * 360.0 / 65536, 2);
        Assert.True(reader.ReadBit());
        Assert.Equal(container.Placement.Rotation.Roll, reader.ReadBits(16) * 360.0 / 65536, 2);
        Assert.Equal(0UL, reader.ReadBits(2));

        ValueCodec values = new(GameReplication.Catalog, [KnownEnums.NetRole]);
        Dictionary<string, object?> content = new PropertyCodec(values).Decode(new PropertyLayout(GameReplication.Catalog, DamLoot.ActorClass), ActorContentBlock.Read(reader), swapActorRoles: false);
        Assert.Equal(0, reader.Remaining);
        Assert.Equal((ulong)NetRole.SimulatedProxy, content[PropertyNames.RemoteRole]);
    }

    [Fact]
    public void A_loot_container_names_its_components_and_enables_its_search()
    {
        ScriptedPlayer session = new();
        LootContainer container = session.Raid.Loot.Containers[0];
        Assert.Same(container, session.Raid.Loot.ByInteraction(container.Interaction));
        Assert.Null(session.Raid.Loot.ByInteraction(container.Guid));

        ChannelBunch names = session.Raid.Loot.ComponentExportBunch(container);
        (ObjectExports records, IReadOnlyList<ulong> roots) = ObjectExports.Read(new BitReader(names.Payload, names.PayloadBits));
        Assert.Equal(container.Components, roots);
        Assert.Equal(DamLoot.InteractionComponent, Assert.IsType<StableSubobjectExport>(records.Records[container.Interaction]).Path);
        Assert.Equal(container.Guid, records.Records[container.Interaction].Outer);

        ChannelBunch search = session.Raid.Loot.InteractionBunch(container, 300, CurrentInteractionState.Idle);
        StableSubobjectContent component = StableSubobjectContent.Read(new BitReader(search.Payload, search.PayloadBits));
        Assert.Equal(container.Interaction, component.Guid);
        Dictionary<string, object?> state = new PropertyCodec(new ValueCodec(GameReplication.Catalog, [DamDoors.InteractionState]))
            .Decode(new PropertyLayout(GameReplication.Catalog, DamLoot.InteractionClass), component.Content);
        Assert.Equal((ulong)CurrentInteractionState.Idle, state[PropertyNames.CurrentInteractionState]);
    }

    [Fact]
    public void One_press_searches_a_container_which_opens_with_a_duck_that_can_be_taken()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0, 123.5, 124.0, 124.5])
        {
            session.Service(at);
        }

        LootContainer container = session.Raid.Loot.Containers[0];
        int channel = 64 + session.Raid.Doors.Doors.Count + session.Raid.Ladders.Ladders.Count + container.Index;
        Assert.Equal(1, session.Channels.LootReady);
        Assert.Single(session.Sent, bunch => bunch is { Opened: true } && bunch.Channel == channel);

        // One press with the raider tool holstered starts the search; it is never taken for a door.
        int pressWithTool = PendulumInput.PressRequest | PendulumInput.EquipToolRequestFlag;
        session.Input(30, 33, container.Interaction, container.Interaction, AtPrompt(session, container), request: pressWithTool);
        session.Service(125.0);
        Assert.Equal([$"{pressWithTool}@33"], session.Channels.LootRequests);
        Assert.Equal((1, 0, 0), (session.Channels.LootSearches, session.Channels.DoorPresses, session.Channels.IgnoredDoorRequests));
        Assert.Null(session.Member.Inventory.OpenLoot);

        // Not before the tool is out, the character lined up and the breach held.
        double drawn = 125.0 + FerroDefaults.UnequipSeconds + HandItems.RaiderToolEquipSeconds;
        session.Input(33, 36, focus: container.Interaction, position: AtPrompt(session, container));
        session.Service(drawn + DamLoot.SearchAlignmentSeconds + DamLoot.SearchHoldSeconds - 0.1);
        Assert.Null(session.Member.Inventory.OpenLoot);

        // Then the container opens with its duck among the player's containers.
        session.Sent.Clear();
        session.Service(drawn + DamLoot.SearchAlignmentSeconds + DamLoot.SearchHoldSeconds);
        Assert.Equal(1, session.Channels.LootOpens);
        int loot = Assert.IsType<int>(session.Member.Inventory.OpenLoot);
        ContainerItem duck = Assert.IsType<ContainerItem>(session.Member.Inventory.LootItems[0]);
        Assert.Equal(InventoryBlackboard.RubberDuckIndex, duck.AssetIndex);
        Assert.Contains(session.Sent, bunch => bunch.Channel == channel);
        Assert.Contains(session.Sent, bunch => bunch.Channel == 6);

        // Taking the duck moves it into the backpack; walking away closes the container, which keeps the rest.
        int backpack = session.Member.Inventory.Backpack;
        ushort free = (ushort)Enumerable.Range(0, session.Member.Inventory.Containers[backpack].Items.Count).First(slot => session.Member.Inventory.Containers[backpack].Items[slot] is null);
        double carried = session.Member.Inventory.Weight;
        session.Input(36, 39, focus: container.Interaction, position: AtPrompt(session, container), inventory: new InventoryCommand(InventoryCommandType.SwapItems,
            new ItemContainerSlot(new ItemContainerHandle(loot, 0), 0), new ItemContainerSlot(new ItemContainerHandle(backpack, 0), free), 0, new ItemContainerHandle(-1, ushort.MaxValue)));
        Assert.Equal(duck, session.Member.Inventory.Containers[backpack].Items[free]);
        Assert.True(session.Member.Inventory.Weight > carried);

        // It stays open within the prompt distance and its leeway (2.1 m), and closes beyond.
        Vector3D near = AtPrompt(session, container) with { X = AtPrompt(session, container).X + 205 };
        session.Input(39, 42, position: near);
        session.Service(130.0);
        Assert.NotNull(session.Member.Inventory.OpenLoot);
        Vector3D away = AtPrompt(session, container) with { X = AtPrompt(session, container).X + 215 };
        session.Input(42, 45, position: away);
        session.Service(131.0);
        Assert.Null(session.Member.Inventory.OpenLoot);
        Assert.Equal(duck, session.Member.Inventory.Containers[backpack].Items[free]);
        Assert.All(session.Raid.LootStates.Items(container.Index), Assert.Null);

        // The client sends only channel bookkeeping on a loot channel.
        session.Channels.Receive(session.Io, new ChannelBunch(channel, [], 0) { Name = ChannelName.Actor });
    }

    [Fact]
    public void Moving_during_the_search_cancels_it()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0, 123.5, 124.0, 124.5])
        {
            session.Service(at);
        }

        LootContainer container = session.Raid.Loot.Containers[0];
        session.Input(30, 33, container.Interaction, container.Interaction, AtPrompt(session, container));
        session.Service(125.0);
        session.Input(33, 36, focus: container.Interaction, position: AtPrompt(session, container), forward: 1);
        session.Service(126.0);
        session.Service(140.0);
        Assert.Equal((1, 1, 0), (session.Channels.LootSearches, session.Channels.CancelledSearches, session.Channels.LootOpens));
        Assert.Null(session.Member.Inventory.OpenLoot);
    }

    [Fact]
    public void An_opened_container_shows_open_ends_when_the_looting_menu_closes_and_reopens_without_a_breach()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0, 123.5, 124.0, 124.5])
        {
            session.Service(at);
        }

        LootContainer container = session.Raid.Loot.Containers[0];
        session.Input(30, 33, container.Interaction, container.Interaction, AtPrompt(session, container));
        session.Service(125.0);
        session.Input(33, 36, focus: container.Interaction, position: AtPrompt(session, container));
        session.Service(125.0 + DamLoot.SearchAlignmentSeconds + DamLoot.SearchHoldSeconds);
        Assert.NotNull(session.Member.Inventory.OpenLoot);
        double opened = Assert.IsType<double>(session.Raid.LootStates.OpenedAt(container.Index));

        // The success flag the client carries on once the container is open closes nothing.
        session.Input(36, 39, 0, container.Interaction, AtPrompt(session, container), request: PendulumInput.LastInteractionSuccessfulFlag);
        session.Service(130.0);
        Assert.NotNull(session.Member.Inventory.OpenLoot);

        // Closing the looting menu sends a request that names no target.
        int closed = PendulumInput.LootingMenuClosedRequest | PendulumInput.LastInteractionSuccessfulFlag;
        session.Input(39, 42, 0, container.Interaction, AtPrompt(session, container), request: closed);
        session.Sent.Clear();
        session.Service(131.0);
        Assert.Null(session.Member.Inventory.OpenLoot);
        Assert.Equal([$"{PendulumInput.PressRequest}@33", $"{closed}@42"], session.Channels.LootRequests);

        // From its first opening the search needs no raider tool, hold or breach animation.
        int channel = 64 + session.Raid.Doors.Doors.Count + session.Raid.Ladders.Ladders.Count + container.Index;
        ChannelBunch ended = session.Sent.Last(bunch => bunch.Channel == channel);
        Dictionary<string, object?> search = new PropertyCodec(new ValueCodec(GameReplication.Catalog, [DamDoors.InteractionState]))
            .Decode(new PropertyLayout(GameReplication.Catalog, DamLoot.InteractionClass), StableSubobjectContent.Read(new BitReader(ended.Payload, ended.PayloadBits)).Content);
        Assert.Equal((ulong)CurrentInteractionState.SuccessIdle, search[PropertyNames.CurrentInteractionState]);
        Assert.Equal(false, search[PropertyNames.EquipRaiderToolDuringInitialization]);
        Assert.Equal(DamLoot.OpenedHoldSeconds, search[PropertyNames.HoldDuration]);
        Assert.Equal(false, search[PropertyNames.UseAnimationContainer]);
        Assert.Equal(DamLoot.NoHoldStartedTime, search[PropertyNames.InteractionServerHoldStartedTime]);

        // Opening again takes no breach.
        session.Input(42, 45, container.Interaction, container.Interaction, AtPrompt(session, container));
        session.Service(132.0);
        session.Input(45, 48, focus: container.Interaction, position: AtPrompt(session, container));
        session.Service(132.0 + DamLoot.SearchAlignmentSeconds - 0.1);
        Assert.Equal(1, session.Channels.LootOpens);
        session.Service(132.0 + DamLoot.SearchAlignmentSeconds);
        Assert.Equal(2, session.Channels.LootOpens);
        Assert.Equal(opened, session.Raid.LootStates.OpenedAt(container.Index));

        // A container spawned after its first opening shows open.
        ChannelBunch spawn = session.Raid.Loot.OpenBunch(container, 300, 1, openedAt: opened);
        BitReader reader = new(spawn.Payload, spawn.PayloadBits);
        MustMapGuids.Read(reader);
        ulong[] ids = [NetGuid.Read(reader), NetGuid.Read(reader), NetGuid.Read(reader)];
        Assert.Equal(container.Guid, ids[0]);
        reader.ReadBit();
        reader.ReadBit();
        reader.ReadBitArray(64 * 3);
        reader.ReadBit();
        for (int axis = 0; axis < 3; axis++)
        {
            if (reader.ReadBit())
            {
                reader.ReadBits(16);
            }
        }

        reader.ReadBits(2);
        Dictionary<string, object?> content = new PropertyCodec(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole]))
            .Decode(new PropertyLayout(GameReplication.Catalog, DamLoot.ActorClass), ActorContentBlock.Read(reader), swapActorRoles: false);
        Assert.Equal(opened, content[PropertyNames.CurrentOpenTime]);
    }

    [Fact]
    public void A_server_run_interaction_shows_its_times_place_and_animation()
    {
        PawnInteraction interaction = new(0x380002, LastInteractionEvent.Restaged, 357.566, 357.566, 358.566, 3, DamLoot.SearchLocation, 0x2F0007);
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(PawnBlackboardFrames.FirstTick, new PawnMotion(default), 0x380002, interaction: interaction);
        PendulumSlot slot = state[PawnBlackboard.InteractorSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[slot.DescriptorId!.Value].Fields;
        ulong Field(int index) => PendulumCodec.Integer(fields[index], slot.Value);

        // Times and places are steps of about 0.12 around the middle of the field.
        Assert.Equal(8388608UL + 2999, Field(PawnBlackboard.AlignmentStartedTimeField));
        Assert.Equal(8388608UL + 3008, Field(PawnBlackboard.InteractionStartedTimeField));
        Assert.Equal((ulong)LastInteractionEvent.Restaged, Field(PawnBlackboard.LastInteractionEventField));
        Assert.Equal(0x380002UL, Field(PawnBlackboard.CurrentInteractionField));
        Assert.Equal(0x80000003UL, Field(PawnBlackboard.InteractionSequenceField));
        Assert.Equal(0UL, Field(PawnBlackboard.InteractorFlagsField));
        Assert.Equal([8388608UL + 640, 8388608UL + 52, 8388608UL - 1], Enumerable.Range(PawnBlackboard.InteractionLocationXField, 3).Select(Field));
        Assert.Equal(0x2F0007UL, Field(PawnBlackboard.InteractionAnimationField));
    }

    // Where the test container's prompt is: the Raid places it 3 m along X from the player's spawn.
    private static Vector3D AtPrompt(ScriptedPlayer session, LootContainer container) =>
        container.Prompt(session.Member.Spawn.Location with { X = session.Member.Spawn.Location.X + 300 });
}
