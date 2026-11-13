using Ares.Raid.Replication;
using Ares.Raid.World;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class LootTests
{
    private const string CabinetActor = "StaticMeshActor_59f2d924aecddadf_19";
    private const string CabinetSocket = "ContainerSocket.Door_4";
    private const string ServerCabinetClass = "/BP_SocketContainer_Gvt_Server_01_x400z250_Door.BP_SocketContainer_Gvt_Server_01_x400z250_Door_C";

    [Fact]
    public void Without_rolls_every_active_socket_with_a_servable_class_gets_one_container()
    {
        ScriptedPlayer session = new();
        LootCatalog catalog = session.Raid.Loot;
        LootSpawnCounts counts = catalog.Counts;

        Assert.Equal(DamLootSockets.All.Count, counts.Sockets);
        Assert.Equal(counts.Sockets, counts.Spawned + counts.InactiveLayer + counts.NoContainerType + counts.Unsupported);
        Assert.Equal((0, 0, 0), (counts.SkippedByRoll, counts.SkippedByGroupCap, counts.SkippedByLimiter));
        Assert.Equal(DamLootSockets.All.Count(socket => socket.DataLayer is { } layer && !session.Raid.ActiveDataLayers.Contains(layer)), counts.InactiveLayer);
        Assert.Equal(counts.Spawned, catalog.Containers.Count);
        Assert.All(catalog.Containers, container => Assert.Contains(container.Class.Container, LootSocketClasses.Supported(container.Socket!)));
        Assert.Equal(catalog.Containers.Count, catalog.Containers.Select(container => container.Guid).Distinct().Count());
        Assert.Equal(catalog.Containers.Count, catalog.Containers.Select(container => container.Interaction).Distinct().Count());
        Assert.All(catalog.Containers, container => Assert.Same(container, catalog.ByInteraction(container.Interaction)));
        Assert.Null(catalog.ByInteraction(catalog.Containers[0].Guid));

        // The reference server cabinet stands on its socket.
        LootContainer cabinet = Cabinet(session);
        Assert.EndsWith(ServerCabinetClass, cabinet.Class.Container.Class, StringComparison.Ordinal);
        Assert.Equal(452930.7, cabinet.Location.X, 0);
        Assert.Equal(559705.2, cabinet.Location.Y, 0);
    }

    [Fact]
    public void Sockets_in_inactive_layers_without_a_type_or_without_a_servable_class_get_no_container()
    {
        LootSocket cabinet = DamLootSockets.All.Single(socket => socket is { Actor: CabinetActor, Socket: CabinetSocket });
        LootSocket unsupported = DamLootSockets.All.First(socket => socket is { DataLayer: null } && LootSocketClasses.Candidates(socket).Count > 0 && LootSocketClasses.Supported(socket).Count == 0);
        LootSocket[] sockets =
        [
            cabinet,
            cabinet with { DataLayer = "DataLayer_Inactive" },
            cabinet with { DataLayer = "DataLayer_Active" },
            cabinet with { ContainerTag = "Socket.LootContainer.Unknown" },
            unsupported,
        ];

        LootCatalog catalog = new(new ScriptedPlayer().Worlds.Current!.State.Replication, sockets, DamLootSockets.Meshes, DamRounds.All[0].Name, ["DataLayer_Active"], 7, LootSpawnRules.EverySocket);
        LootSpawnCounts counts = catalog.Counts;
        Assert.Equal((5, 2, 1, 1, 1, 1, 0, 0, 0), (counts.Sockets, counts.Spawned, counts.InactiveLayer, counts.NoContainerType, counts.Unsupported, counts.Classes, counts.SkippedByRoll, counts.SkippedByGroupCap, counts.SkippedByLimiter));
        Assert.Equal(2, counts.SpawnedByLootValue[PointOfInterestLootValue.Unknown]);
        Assert.Equal(0, counts.SpawnedByLootValue[PointOfInterestLootValue.High]);
        Assert.Equal([sockets[0], sockets[2]], catalog.Containers.Select(container => container.Socket));
    }

    [Fact]
    public void A_container_is_relevant_within_the_net_cull_distance()
    {
        ScriptedPlayer session = new();
        LootContainer cabinet = Cabinet(session);
        Vector3D at = cabinet.Location;

        IReadOnlyList<LootContainer> near = session.Raid.Loot.Relevant(at);
        Assert.Same(cabinet, near[0]);
        Assert.All(near, container => Assert.True(Distance(container.Location, at) <= LootContainerClasses.NetCullDistance));
        Assert.Equal(session.Raid.Loot.Containers.Count(container => Distance(container.Location, at) <= LootContainerClasses.NetCullDistance), near.Count);
        Assert.DoesNotContain(cabinet, session.Raid.Loot.Relevant(at with { X = at.X + LootContainerClasses.NetCullDistance + 1 }));
        Assert.Contains(cabinet, session.Raid.Loot.Relevant(at with { X = at.X + LootContainerClasses.NetCullDistance - 1 }));
    }

    [Fact]
    public void An_open_container_closes_beyond_its_class_prompt_distance_and_the_server_leeway()
    {
        LootContainer cabinet = Cabinet(new ScriptedPlayer());
        Assert.Equal(195, cabinet.Class.OpenedPromptDistance);
        Assert.Equal(270, cabinet.CloseDistance);

        // The prompt sits on the cabinet door, turned with the cabinet.
        Vector3D offset = cabinet.Rotation.Rotate(cabinet.Class.PromptLocation);
        Assert.Equal(cabinet.Location.X + offset.X, cabinet.Prompt.X, 6);
        Assert.Equal(cabinet.Location.Z + offset.Z, cabinet.Prompt.Z, 6);
    }

    [Fact]
    public void A_loot_container_spawns_at_its_socket_from_a_loadable_class()
    {
        ScriptedPlayer session = new();
        LootCatalog catalog = session.Raid.Loot;
        LootContainer container = Cabinet(session);

        // Socket containers are not part of the map, so the client may load their class.
        ChannelBunch exports = catalog.ExportBunch(container);
        (ObjectExports records, IReadOnlyList<ulong> roots) = ObjectExports.Read(new BitReader(exports.Payload, exports.PayloadBits));
        Assert.Equal([container.Archetype, container.SearchAnimation], roots);
        Assert.Equal(container.Class.SearchAnimation!.Split('.')[1], Assert.IsType<StaticObjectExport>(records.Records[container.SearchAnimation]).Path);
        StaticObjectExport archetype = Assert.IsType<StaticObjectExport>(records.Records[container.Archetype]);
        Assert.Equal((container.Class.Container.Archetype.Split('.')[1], false), (archetype.Path, archetype.NoLoad));

        ChannelBunch bunch = catalog.OpenBunch(container, 300, 1);
        Assert.True(bunch is { Opened: true, MustBeMapped: true });
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        Assert.Equal([container.Archetype], MustMapGuids.Read(reader).Guids);
        Assert.Equal((container.Guid, container.Archetype, 0UL), (NetGuid.Read(reader), NetGuid.Read(reader), NetGuid.Read(reader)));
        Assert.Equal((true, false), (reader.ReadBit(), reader.ReadBit()));
        Assert.Equal(container.Location.X, BitConverter.UInt64BitsToDouble(reader.ReadBits(64)));
        Assert.Equal(container.Location.Y, BitConverter.UInt64BitsToDouble(reader.ReadBits(64)));
        Assert.Equal(container.Location.Z, BitConverter.UInt64BitsToDouble(reader.ReadBits(64)));
        Assert.True(reader.ReadBit());
        Assert.Equal((false, true), (reader.ReadBit(), reader.ReadBit()));
        Assert.Equal(container.Rotation.Yaw, reader.ReadBits(16) * 360.0 / 65536, 2);
        Assert.True(reader.ReadBit());
        Assert.Equal(container.Rotation.Roll, reader.ReadBits(16) * 360.0 / 65536, 2);
        Assert.Equal(0UL, reader.ReadBits(2));

        ValueCodec values = new(GameReplication.Catalog, [KnownEnums.NetRole]);
        Dictionary<string, object?> content = new PropertyCodec(values).Decode(new PropertyLayout(GameReplication.Catalog, container.Class.ActorClass), ActorContentBlock.Read(reader), swapActorRoles: false);
        Assert.Equal(0, reader.Remaining);
        Assert.Equal((ulong)NetRole.SimulatedProxy, content[PropertyNames.RemoteRole]);
    }

    [Fact]
    public void A_loot_container_names_its_components_and_sends_its_search_state()
    {
        ScriptedPlayer session = new();
        LootContainer container = Cabinet(session);

        ChannelBunch names = LootCatalog.ComponentExportBunch(container);
        (ObjectExports records, IReadOnlyList<ulong> roots) = ObjectExports.Read(new BitReader(names.Payload, names.PayloadBits));
        Assert.Equal(container.Components, roots);
        Assert.Equal(LootContainerClasses.InteractionComponent, Assert.IsType<StableSubobjectExport>(records.Records[container.Interaction]).Path);
        Assert.Equal(container.Guid, records.Records[container.Interaction].Outer);

        ChannelBunch search = session.Raid.Loot.StateBunch(container, 300, new LootView(CurrentInteractionState.Idle, null, null, null));
        Dictionary<string, object?> state = Search(container, search);
        Assert.Equal((ulong)CurrentInteractionState.Idle, state[PropertyNames.CurrentInteractionState]);
        Assert.Equal(LootInteractions.NoHoldStartedTime, state[PropertyNames.InteractionServerHoldStartedTime]);
    }

    [Fact]
    public void Relevant_containers_reach_the_client_nearest_first_a_few_at_a_time_and_go_dormant()
    {
        ScriptedPlayer session = new();
        (_, double at) = Ready(session);
        int first = FirstLootChannel(session);
        LootContainer cabinet = Cabinet(session);
        Vector3D spawn = session.Member.Spawn.Location;
        int relevant = session.Raid.Loot.Relevant(spawn).Count;
        Assert.True(relevant > 24, $"{relevant} containers near the spawn");

        // The first check starts a few deliveries, then at most 24 run at once, each on a loot channel.
        session.Service(at);
        Assert.Equal(8, session.Channels.LootChannelsInUse);
        for (int step = 0; step < 4; step++)
        {
            session.Service(at += 0.3);
            Assert.InRange(session.Channels.LootChannelsInUse, 1, 24);
        }

        for (int step = 0; step < 400 && session.Channels.LootWaiting > 0; step++)
        {
            session.Service(at += 0.3);
        }

        session.Service(at += 0.3);
        session.Service(at + 0.3);
        Assert.Equal(0, session.Channels.LootWaiting);
        Assert.Equal(relevant, session.Channels.LootReady);
        Assert.Equal(0, session.Channels.LootChannelsInUse);
        Assert.False(session.Channels.HasLoot(cabinet.Index));

        // Every loot channel opened by a spawn and closed dormant, inside the loot channel range.
        List<ChannelBunch> opens = [.. session.Sent.Where(bunch => bunch.Opened && bunch.Channel >= first && bunch.Channel < first + PlayerChannels.LootChannels)];
        Assert.Equal(relevant, opens.Count);
        Assert.Equal(relevant, session.Sent.Count(bunch => bunch.CloseReason == Bunches.Dormancy));
        Assert.DoesNotContain(session.Sent, bunch => bunch.CloseReason is { } reason && reason != Bunches.Dormancy && bunch.Channel >= first && bunch.Channel < first + PlayerChannels.LootChannels);
        List<ulong> spawned = [.. opens.Select(SpawnedGuid)];
        Assert.Equal(spawned.Count, spawned.Distinct().Count());

        // Nearest first.
        double[] distances = [.. spawned.Select(guid => Distance(session.Raid.Loot.Containers.Single(container => container.Guid == guid).Location, spawn))];
        Assert.True(distances.Take(8).Max() <= distances.Skip(8).Min());
    }

    [Fact]
    public void One_press_searches_a_container_which_opens_with_a_duck_that_can_be_taken()
    {
        ScriptedPlayer session = new();
        LootContainer container = Cabinet(session);
        (uint frame, double at) = Reach(session, container);
        int opens = session.Channels.LootChannelOpens;

        // One press with the raider tool holstered starts the search; it is never taken for a door. The dormant
        // container's channel opens again for it.
        const int PressWithTool = PendulumInput.PressRequest | PendulumInput.EquipToolRequestFlag;
        session.Sent.Clear();
        session.Input(frame, frame + 3, container.Interaction, container.Interaction, container.Prompt, request: PressWithTool);
        session.Service(at);
        Assert.Equal([$"{PressWithTool}@{frame + 3}"], session.Channels.LootRequests);
        Assert.Equal((1, 0, 0), (session.Channels.LootSearches, session.Channels.DoorPresses, session.Channels.IgnoredDoorRequests));
        Assert.Null(session.Member.Inventory.OpenLoot);
        Assert.Equal(opens + 1, session.Channels.LootChannelOpens);
        ChannelBunch reopen = Assert.Single(session.Sent, bunch => OpensLoot(session, bunch, container));
        Dictionary<string, object?> searching = Search(container, session.Sent.Last(bunch => bunch.Channel == reopen.Channel));
        Assert.Equal((ulong)CurrentInteractionState.Ongoing, searching[PropertyNames.CurrentInteractionState]);

        // Not before the tool is out, the character lined up and the breach held.
        double drawn = at + FerroDefaults.UnequipSeconds + HandItems.RaiderToolEquipSeconds;
        double opensAt = drawn + container.Class.AlignmentSeconds + container.Class.HoldSeconds;
        session.Input(frame + 3, frame + 6, focus: container.Interaction, position: container.Prompt);
        session.Service(opensAt - 0.1);
        Assert.Null(session.Member.Inventory.OpenLoot);
        Assert.DoesNotContain(session.Sent, bunch => bunch.Channel == reopen.Channel && bunch.CloseReason is not null);

        // Then the container opens with its duck among the player's containers.
        session.Sent.Clear();
        session.Service(opensAt);
        Assert.Equal(1, session.Channels.LootOpens);
        int loot = Assert.IsType<int>(session.Member.Inventory.OpenLoot);
        ContainerItem duck = Assert.IsType<ContainerItem>(session.Member.Inventory.LootItems[0]);
        Assert.Equal(InventoryBlackboard.RubberDuckIndex, duck.AssetIndex);
        Assert.Equal(container.Class.SlotCount, session.Member.Inventory.LootItems.Count);
        Assert.Contains(session.Sent, bunch => bunch.Channel == reopen.Channel);
        Assert.Contains(session.Sent, bunch => bunch.Channel == 6);

        // Taking the duck moves it into the backpack.
        int backpack = session.Member.Inventory.Backpack;
        ushort free = (ushort)Enumerable.Range(0, session.Member.Inventory.Containers[backpack].Items.Count).First(slot => session.Member.Inventory.Containers[backpack].Items[slot] is null);
        double carried = session.Member.Inventory.Weight;
        session.Input(frame + 6, frame + 9, focus: container.Interaction, position: container.Prompt, inventory: new InventoryCommand(InventoryCommandType.SwapItems,
            new ItemContainerSlot(new ItemContainerHandle(loot, 0), 0), new ItemContainerSlot(new ItemContainerHandle(backpack, 0), free), 0, new ItemContainerHandle(-1, ushort.MaxValue)));
        Assert.Equal(duck, session.Member.Inventory.Containers[backpack].Items[free]);
        Assert.True(session.Member.Inventory.Weight > carried);

        // It stays open within the prompt distance and its leeway (2.7 m), and closes beyond, keeping the rest.
        session.Input(frame + 9, frame + 12, position: container.Prompt with { X = container.Prompt.X + 265 });
        session.Service(opensAt + 1);
        Assert.NotNull(session.Member.Inventory.OpenLoot);
        session.Input(frame + 12, frame + 15, position: container.Prompt with { X = container.Prompt.X + 275 });
        session.Sent.Clear();
        session.Service(opensAt + 2);
        Assert.Null(session.Member.Inventory.OpenLoot);
        Assert.Equal(duck, session.Member.Inventory.Containers[backpack].Items[free]);
        Assert.All(session.Raid.LootStates.Items(container.Index), Assert.Null);

        // Its channel then goes dormant; the client sends only channel bookkeeping there.
        session.Service(opensAt + 3);
        Assert.Contains(session.Sent, bunch => bunch.Channel == reopen.Channel && bunch.CloseReason == Bunches.Dormancy);
        Assert.True(session.Channels.HasLoot(container.Index));
        session.Channels.Receive(session.Io, new ChannelBunch(reopen.Channel, [], 0) { Name = ChannelName.Actor });
    }

    [Fact]
    public void A_spawner_container_is_searched_like_a_socket_container()
    {
        ScriptedPlayer session = new(lootRules: LootSpawnRules.EverySocket with { FreeStanding = FreeStandingLoot.Dam });
        LootContainer container = session.Raid.Loot.Containers.First(container => container.Source == LootContainerSource.Spawner);
        Assert.Null(container.Socket);
        (uint frame, double at) = Reach(session, container);

        const int PressWithTool = PendulumInput.PressRequest | PendulumInput.EquipToolRequestFlag;
        session.Input(frame, frame + 3, container.Interaction, container.Interaction, container.Prompt, request: PressWithTool);
        session.Service(at);
        Assert.Equal(1, session.Channels.LootSearches);
        double ready = container.Class.EquipsRaiderTool ? at + FerroDefaults.UnequipSeconds + HandItems.RaiderToolEquipSeconds : at;
        session.Input(frame + 3, frame + 6, focus: container.Interaction, position: container.Prompt);
        session.Service(ready + container.Class.AlignmentSeconds + container.Class.HoldSeconds);
        Assert.Equal(1, session.Channels.LootOpens);
        Assert.NotNull(session.Member.Inventory.OpenLoot);
        ContainerItem duck = Assert.IsType<ContainerItem>(session.Member.Inventory.LootItems[0]);
        Assert.Equal(InventoryBlackboard.RubberDuckIndex, duck.AssetIndex);
        Assert.NotNull(session.Raid.LootStates.OpenedAt(container.Index));
    }

    [Fact]
    public void A_loot_channel_the_client_closes_is_counted_named_and_not_delivered_again()
    {
        ScriptedPlayer session = new(lootRules: LootSpawnRules.EverySocket with { FreeStanding = FreeStandingLoot.Dam });
        LootContainer container = session.Raid.Loot.Containers.First(container => container.Source == LootContainerSource.Spawner);
        (uint frame, double at) = Ready(session);
        ChannelBunch? open = null;
        for (int step = 0; step < 300 && open is null; step++)
        {
            session.Input(frame, frame + 3, position: container.Prompt);
            session.Service(at);
            (frame, at) = (frame + 3, at + 0.3);
            open = session.Sent.FirstOrDefault(bunch => OpensLoot(session, bunch, container));
        }

        Assert.NotNull(open);
        session.Channels.ActorChannelFailed(open.Channel);
        Assert.Equal(1, session.Channels.LootFailed);
        string failure = Assert.Single(session.Channels.LootFailures);
        string name = container.Class.Container.Class.Split('.')[^1]["BP_SocketContainer_".Length..^"_C".Length];
        Assert.StartsWith($"Spawner:{name}@", failure, StringComparison.Ordinal);

        int sent = session.Sent.Count;
        for (int step = 0; step < 20; step++)
        {
            session.Input(frame, frame + 3, position: container.Prompt);
            session.Service(at);
            (frame, at) = (frame + 3, at + 0.3);
        }

        Assert.DoesNotContain(session.Sent.Skip(sent), bunch => OpensLoot(session, bunch, container));
        Assert.False(session.Channels.HasLoot(container.Index));
    }

    [Fact]
    public void Moving_during_the_search_cancels_it()
    {
        ScriptedPlayer session = new();
        LootContainer container = Cabinet(session);
        (uint frame, double at) = Reach(session, container);
        session.Input(frame, frame + 3, container.Interaction, container.Interaction, container.Prompt);
        session.Service(at);
        session.Input(frame + 3, frame + 6, focus: container.Interaction, position: container.Prompt, forward: 1);
        session.Service(at + 1);
        session.Service(at + 15);
        session.Service(at + 16);
        Assert.Equal((1, 1, 0), (session.Channels.LootSearches, session.Channels.CancelledSearches, session.Channels.LootOpens));
        Assert.Null(session.Member.Inventory.OpenLoot);
        Assert.Equal(0, session.Channels.LootChannelsInUse);
    }

    [Fact]
    public void An_opened_container_shows_open_ends_when_the_looting_menu_closes_and_reopens_without_a_breach()
    {
        ScriptedPlayer session = new();
        LootContainer container = Cabinet(session);
        (uint frame, double at) = Reach(session, container);
        session.Sent.Clear();
        session.Input(frame, frame + 3, container.Interaction, container.Interaction, container.Prompt);
        session.Service(at);
        int channel = Assert.Single(session.Sent, bunch => OpensLoot(session, bunch, container)).Channel;
        session.Input(frame + 3, frame + 6, focus: container.Interaction, position: container.Prompt);
        session.Service(at + container.Class.AlignmentSeconds + container.Class.HoldSeconds);
        Assert.NotNull(session.Member.Inventory.OpenLoot);
        double opened = Assert.IsType<double>(session.Raid.LootStates.OpenedAt(container.Index));

        // The success flag the client carries on once the container is open closes nothing.
        session.Input(frame + 6, frame + 9, 0, container.Interaction, container.Prompt, request: PendulumInput.LastInteractionSuccessfulFlag);
        session.Service(at + 6);
        Assert.NotNull(session.Member.Inventory.OpenLoot);

        // Closing the looting menu sends a request that names no target.
        const int Closed = PendulumInput.LootingMenuClosedRequest | PendulumInput.LastInteractionSuccessfulFlag;
        session.Input(frame + 9, frame + 12, 0, container.Interaction, container.Prompt, request: Closed);
        session.Sent.Clear();
        session.Service(at + 7);
        Assert.Null(session.Member.Inventory.OpenLoot);
        Assert.Equal([$"{PendulumInput.PressRequest}@{frame + 3}", $"{Closed}@{frame + 12}"], session.Channels.LootRequests);

        // From its first opening the search needs no raider tool, hold or breach animation.
        Dictionary<string, object?> search = Search(container, session.Sent.Last(bunch => bunch.Channel == channel && bunch.CloseReason is null));
        Assert.Equal((ulong)CurrentInteractionState.SuccessIdle, search[PropertyNames.CurrentInteractionState]);
        Assert.Equal(false, search[PropertyNames.EquipRaiderToolDuringInitialization]);
        Assert.Equal(LootInteractions.OpenedHoldSeconds, search[PropertyNames.HoldDuration]);
        Assert.Equal(false, search[PropertyNames.UseAnimationContainer]);
        Assert.Equal(LootInteractions.NoHoldStartedTime, search[PropertyNames.InteractionServerHoldStartedTime]);

        // Opening again takes no breach.
        session.Service(at + 8);
        session.Input(frame + 12, frame + 15, container.Interaction, container.Interaction, container.Prompt);
        session.Service(at + 9);
        session.Input(frame + 15, frame + 18, focus: container.Interaction, position: container.Prompt);
        session.Service(at + 9 + container.Class.AlignmentSeconds - 0.1);
        Assert.Equal(1, session.Channels.LootOpens);
        session.Service(at + 9 + container.Class.AlignmentSeconds);
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
            .Decode(new PropertyLayout(GameReplication.Catalog, container.Class.ActorClass), ActorContentBlock.Read(reader), swapActorRoles: false);
        Assert.Equal(opened, content[PropertyNames.CurrentOpenTime]);
    }

    [Fact]
    public void A_first_opening_reaches_other_players_who_have_the_container_through_a_short_open_channel()
    {
        ScriptedPlayer host = new();
        ScriptedPlayer other = new(host, 43);
        LootContainer container = Cabinet(host);
        (uint frame, double at) = Reach(host, container);
        (_, double otherAt) = Reach(other, container, at);
        int opens = other.Channels.LootChannelOpens;

        host.Input(frame, frame + 3, container.Interaction, container.Interaction, container.Prompt);
        host.Service(otherAt);
        host.Input(frame + 3, frame + 6, focus: container.Interaction, position: container.Prompt);
        host.Service(otherAt + container.Class.AlignmentSeconds + container.Class.HoldSeconds);
        double opened = Assert.IsType<double>(host.Raid.LootStates.OpenedAt(container.Index));

        // The other client keeps the dormant container: its channel opens again with the opened state, then closes.
        other.Sent.Clear();
        other.Service(otherAt + 5);
        Assert.Equal(opens + 1, other.Channels.LootChannelOpens);
        ChannelBunch reopen = Assert.Single(other.Sent, bunch => OpensLoot(other, bunch, container));
        Dictionary<string, object?> search = Search(container, other.Sent.Last(bunch => bunch.Channel == reopen.Channel && bunch.CloseReason is null));
        Assert.Equal((ulong)CurrentInteractionState.SuccessIdle, search[PropertyNames.CurrentInteractionState]);
        Assert.Equal(opened, search[PropertyNames.FirstOpenedServerTime]);
        other.Service(otherAt + 6);
        other.Service(otherAt + 7);
        Assert.Contains(other.Sent, bunch => bunch.Channel == reopen.Channel && bunch.CloseReason == Bunches.Dormancy);
        Assert.Equal(0, other.Channels.LootChannelsInUse);

        // Nothing more changes: the channel stays closed.
        other.Sent.Clear();
        other.Service(otherAt + 8);
        Assert.DoesNotContain(other.Sent, bunch => bunch.Channel == reopen.Channel);
    }

    [Fact]
    public void A_server_run_interaction_shows_its_times_place_and_animation()
    {
        PawnInteraction interaction = new(0x380002, LastInteractionEvent.Restaged, 357.566, 357.566, 358.566, 3, new Vector3D(76.3, 6.2, -0.1), 0x2F0007);
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(PawnBlackboardFrames.FirstTick, new PawnMotion(default), 0x380002, interaction: interaction);
        PendulumSlot slot = state[PawnBlackboard.InteractorSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[slot.DescriptorId!.Value].Fields;
        // Times and places are steps of about 0.12 around the middle of the field.
        Assert.Equal(8388608UL + 2999, Field(PawnBlackboard.AlignmentStartedTimeField));
        Assert.Equal(8388608UL + 3008, Field(PawnBlackboard.InteractionStartedTimeField));
        Assert.Equal((ulong)LastInteractionEvent.Restaged, Field(PawnBlackboard.LastInteractionEventField));
        Assert.Equal(0x380002UL, Field(PawnBlackboard.CurrentInteractionField));
        Assert.Equal(0x80000003UL, Field(PawnBlackboard.InteractionSequenceField));
        Assert.Equal(0UL, Field(PawnBlackboard.InteractorFlagsField));
        Assert.Equal([8388608UL + 640, 8388608UL + 52, 8388608UL - 1], Enumerable.Range(PawnBlackboard.InteractionLocationXField, 3).Select(Field));
        Assert.Equal(0x2F0007UL, Field(PawnBlackboard.InteractionAnimationField));
        return;

        ulong Field(int index) => PendulumCodec.Integer(fields[index], slot.Value);
    }

    // The reference server cabinet among the raid's containers.
    private static LootContainer Cabinet(ScriptedPlayer session) =>
        session.Raid.Loot.Containers.Single(container => container.Socket is { Actor: CabinetActor, Socket: CabinetSocket });

    // Possession, then services until map actors are allowed; returns the next input frame and service time.
    private static (uint Frame, double At) Ready(ScriptedPlayer session, double from = 100.0)
    {
        session.Possess();
        session.Service(from);
        session.Service(from + 6);
        return (30, from + 21);
    }

    // Walks the character to the container's prompt until its client has the container and no loot channel is open.
    private static (uint Frame, double At) Reach(ScriptedPlayer session, LootContainer container, double from = 100.0)
    {
        (uint frame, double at) = Ready(session, from);
        for (int step = 0; step < 300 && (!session.Channels.HasLoot(container.Index) || session.Channels.LootChannelsInUse > 0); step++)
        {
            session.Input(frame, frame + 3, position: container.Prompt);
            session.Service(at);
            (frame, at) = (frame + 3, at + 0.3);
        }

        Assert.True(session.Channels.HasLoot(container.Index));
        return (frame, at);
    }

    private static int FirstLootChannel(ScriptedPlayer session) => 64 + session.Raid.Doors.Doors.Count + session.Raid.Ladders.Ladders.Count;

    // Whether a bunch opens a loot channel for the container.
    private static bool OpensLoot(ScriptedPlayer session, ChannelBunch bunch, LootContainer container) =>
        bunch.Opened && bunch.Channel >= FirstLootChannel(session) && bunch.Channel < FirstLootChannel(session) + PlayerChannels.LootChannels
        && SpawnedGuid(bunch) == container.Guid;

    // The actor a channel's opening bunch spawns.
    private static ulong SpawnedGuid(ChannelBunch bunch)
    {
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        MustMapGuids.Read(reader);
        return NetGuid.Read(reader);
    }

    // The search interaction's state in a container's state bunch.
    private static Dictionary<string, object?> Search(LootContainer container, ChannelBunch bunch)
    {
        BitReader probe = new(bunch.Payload, bunch.PayloadBits);
        probe.ReadBit();
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        if (probe.ReadBit())
        {
            ActorContentBlock.Read(reader);
        }

        StableSubobjectContent component = StableSubobjectContent.Read(reader);

        Assert.Equal(container.Interaction, component.Guid);
        return new PropertyCodec(new ValueCodec(GameReplication.Catalog, [DamDoors.InteractionState]))
            .Decode(new PropertyLayout(GameReplication.Catalog, container.InteractionClass), component.Content);
    }

    private static double Distance(Vector3D a, Vector3D b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)) + ((a.Z - b.Z) * (a.Z - b.Z)));
}
