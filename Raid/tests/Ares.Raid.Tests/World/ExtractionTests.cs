using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class ExtractionTests
{
    [Fact]
    public void Elevators_are_named_by_their_path_in_the_persistent_level()
    {
        ScriptedPlayer session = new();
        ExtractionCatalog catalog = session.Raid.Extraction;
        Assert.Equal(DamExtraction.All.Select(placement => placement.Actor), catalog.Points.Select(point => point.Name));
        ExtractionPoint point = catalog.Points[1];
        Assert.Equal(DamExtraction.Buttons.Count, point.Buttons.Count);
        Assert.Equal((point, DamExtraction.Buttons[0]), catalog.ByButton(point.Buttons[0]));
        Assert.Null(catalog.ByButton(point.Guid));

        // The buttons, the elevator, the persistent level, the world and the map package, then the descend switches'
        // animation set; nothing is loaded.
        IReadOnlyList<ChannelBunch> bunches = catalog.ExportBunches(point);
        Assert.All(bunches, bunch => Assert.True(bunch.PackageExports));
        int descend = IndexOf(ExtractionButtonRole.Descend);
        ulong animation = catalog.Animation(descend);
        Assert.Equal([.. point.Buttons, animation], bunches.Select(bunch => Assert.Single(ObjectExports.Read(new BitReader(bunch.Payload, bunch.PayloadBits)).Roots)));
        Assert.Equal(0UL, catalog.Animation(s_summon));
        (ObjectExports animations, _) = ObjectExports.Read(new BitReader(bunches[^1].Payload, bunches[^1].PayloadBits));
        StaticObjectExport set = Assert.IsType<StaticObjectExport>(animations.Records[animation]);
        StaticObjectExport setPackage = Assert.IsType<StaticObjectExport>(animations.Records[set.OuterGuid]);
        Assert.Equal(DamExtraction.Buttons[descend].AnimationContainer, $"{setPackage.Path}/{set.Path}"[..setPackage.Path.Length]);
        Assert.EndsWith($"/{set.Path}", setPackage.Path, StringComparison.Ordinal);
        Assert.True(set.NoLoad && setPackage.NoLoad);
        ChannelBunch first = bunches[0];
        (ObjectExports exports, _) = ObjectExports.Read(new BitReader(first.Payload, first.PayloadBits));
        StaticObjectExport button = Assert.IsType<StaticObjectExport>(exports.Records[point.Buttons[0]]);
        Assert.Equal((DamExtraction.Buttons[0].Component, point.Guid), (button.Path, button.OuterGuid));
        StaticObjectExport actor = Assert.IsType<StaticObjectExport>(exports.Records[point.Guid]);
        StaticObjectExport level = Assert.IsType<StaticObjectExport>(exports.Records[actor.OuterGuid]);
        StaticObjectExport world = Assert.IsType<StaticObjectExport>(exports.Records[level.OuterGuid]);
        StaticObjectExport package = Assert.IsType<StaticObjectExport>(exports.Records[world.OuterGuid]);
        Assert.Equal(
            (DamExtraction.All[1].Actor, DamMap.PersistentLevelName, DamMap.WorldName, DamMap.MapPackage, 0UL),
            (actor.Path, level.Path, world.Path, package.Path, package.OuterGuid));
        Assert.All(exports.Records.Values.OfType<StaticObjectExport>(), record => Assert.True(record.NoLoad, record.Path));
    }

    [Fact]
    public void An_elevator_opens_closed_and_enabled_with_only_its_call_button_usable()
    {
        ScriptedPlayer session = new();
        ExtractionPoint point = session.Raid.Extraction.Points[0];
        ExtractionPointState start = session.Raid.ExtractionStates.Get(point.Index);
        Assert.Equal(new ExtractionPointState(ExtractionState.Closed, null, null), start);
        IReadOnlyList<CurrentInteractionState> buttons = session.Raid.ExtractionStates.Buttons(point.Index);
        Assert.Equal(DamExtraction.Buttons.Select(button => button.StartEnabled ? CurrentInteractionState.Idle : CurrentInteractionState.Disabled), buttons);
        Assert.Equal(CurrentInteractionState.Idle, buttons[DamExtraction.Buttons.ToList().FindIndex(button => button.Role == ExtractionButtonRole.Summon)]);

        ChannelBunch bunch = session.Raid.Extraction.OpenBunch(point, 300, 1, start, 42.5);
        Assert.True(bunch is { Opened: true, MustBeMapped: false, Channel: 300 });
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        Assert.Equal(point.Guid, NetGuid.Read(reader));

        ValueCodec values = new(GameReplication.Catalog, [KnownEnums.NetRole, DamDoors.InteractionState, DamExtraction.StateEnum]);
        PropertyCodec codec = new(values);
        Dictionary<string, object?> content = codec.Decode(new PropertyLayout(GameReplication.Catalog, DamExtraction.ActorClass), ActorContentBlock.Read(reader), swapActorRoles: false);
        Assert.Equal((ulong)ExtractionState.Closed, content[PropertyNames.State]);
        Assert.Equal(42.5, content[PropertyNames.StateChangeTimestamp]);
        Assert.Equal(true, content[PropertyNames.IsEnabled]);
        Assert.Equal((ulong)NetRole.SimulatedProxy, content[PropertyNames.RemoteRole]);
        Assert.False(content.ContainsKey(PropertyNames.CanExtract));
        Assert.False(content.ContainsKey(StartedAtPath));

        for (int index = 0; index < point.Buttons.Count; index++)
        {
            bool layout = reader.ReadBit();
            Assert.False(reader.ReadBit());
            Assert.Equal(point.Buttons[index], NetGuid.Read(reader));
            Assert.True(reader.ReadBit());
            uint size = reader.ReadPacked();
            ActorContentBlock block = new(reader.ReadBitArray(size), size, layout);
            Dictionary<string, object?> button = codec.Decode(new PropertyLayout(GameReplication.Catalog, DamExtraction.Buttons[index].Class), block, swapActorRoles: false);
            Assert.Equal((ulong)buttons[index], button[PropertyNames.CurrentInteractionState]);
        }

        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Every_elevator_is_sent_once_after_the_map_actor_delay_and_its_channel_stays_open()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0, 123.5])
        {
            session.Service(at);
        }

        int first = 64 + session.Raid.Doors.Doors.Count + session.Raid.Ladders.Ladders.Count + session.Raid.Loot.Containers.Count;
        int count = session.Raid.Extraction.Points.Count;
        Assert.Equal(DamExtraction.All.Count, session.Channels.ElevatorsReady);
        Assert.Equal(Enumerable.Range(first, count), session.Sent.Where(bunch => bunch is { Opened: true } && bunch.Channel >= first && bunch.Channel < first + count)
            .Select(bunch => bunch.Channel).Order());
        Assert.DoesNotContain(session.Sent, bunch => bunch.CloseReason is not null && bunch.Channel >= first && bunch.Channel < first + count);

        // The client sends only channel bookkeeping there; nothing more is sent while the state stays.
        session.Channels.Receive(session.Io, new ChannelBunch(first, [], 0) { Name = ChannelName.Actor });
        session.Sent.Clear();
        session.Service(124.0);
        Assert.DoesNotContain(session.Sent, bunch => bunch.Channel >= first && bunch.Channel < first + count);
    }

    [Fact]
    public void Presses_on_elevator_buttons_and_on_unknown_targets_are_logged()
    {
        ScriptedPlayer session = new();
        session.Possess();
        ExtractionPoint point = session.Raid.Extraction.Points[2];
        session.Input(30, 33, 0);
        session.Input(33, 36, point.Buttons[0]);

        // A press on an elevator this client does not have yet is only logged: it is not a door request and leaves
        // the elevator closed.
        Assert.Equal(0, session.Channels.IgnoredDoorRequests);
        Assert.Equal(ExtractionState.Closed, session.Raid.ExtractionStates.Get(point.Index).State);
        session.Input(36, 39, 0x7fff0001);
        session.Input(39, 42, session.Door.Buttons[0]);

        Assert.Equal([$"{point.Name}/{DamExtraction.Buttons[0].Component} {PendulumInput.PressRequest}@36"], session.Channels.ElevatorRequests);
        Assert.Equal([$"0x0 {PendulumInput.PressRequest}@33", $"0x7fff0001 {PendulumInput.PressRequest}@39"], session.Channels.UnknownInteractionRequests);
    }

    private const string StartedAtPath = $"{PropertyNames.ExtractionInfo}.{PropertyNames.ExtractionStartedTs}";
    private const string ExtractionTimePath = $"{PropertyNames.ExtractionInfo}.{PropertyNames.ExtractionTime}";

    private static readonly int s_summon = IndexOf(ExtractionButtonRole.Summon);

    [Fact]
    public void A_call_runs_the_press_then_the_elevator_arrives_and_is_ready_after_the_settings_times()
    {
        ExtractionStates states = new(2);
        const double Pressed = 512.666;
        Assert.True(states.Call(0, 42, Pressed));
        Assert.False(states.Call(0, 43, Pressed + 0.05));
        ExtractionPointState called = states.Get(0);
        Assert.Equal(new ExtractionPointState(ExtractionState.Closed, null, 42, Pressed), called);
        Assert.Equal(ByRole(CurrentInteractionState.Ongoing, CurrentInteractionState.Disabled), called.Buttons);
        Assert.False(called.CanExtract);

        // Arriving once the press has run: the call button's alignment plus the settings' summon hold.
        double arriving = Pressed + DamExtraction.Buttons[s_summon].AlignmentSeconds + DamExtraction.Settings.SummonHoldSeconds;
        Assert.Equal(arriving, Pressed + ExtractionStates.CallSeconds);
        states.Advance(arriving - 0.01);
        Assert.Equal(called, states.Get(0));
        states.Advance(arriving + 0.02);
        ExtractionPointState arrived = states.Get(0);
        Assert.Equal(new ExtractionPointState(ExtractionState.Arriving, arriving, 42), arrived);
        Assert.Equal(ByRole(CurrentInteractionState.SuccessDisabled, CurrentInteractionState.Disabled), arrived.Buttons);
        Assert.Equal((false, (ExtractionTimer?)null), (arrived.CanExtract, arrived.Timer));
        Assert.False(states.Call(0, 43, arriving + 1));

        // Ready exactly the time to extract ready later, whenever the server looks.
        double ready = arriving + DamExtraction.Settings.TimeToExtractReadySeconds;
        states.Advance(ready - 0.01);
        Assert.Equal(arrived, states.Get(0));
        states.Advance(ready + 3);
        ExtractionPointState open = states.Get(0);
        Assert.Equal(new ExtractionPointState(ExtractionState.Ready, ready, 42), open);
        Assert.Equal(ByRole(CurrentInteractionState.SuccessDisabled, CurrentInteractionState.Idle), open.Buttons);
        Assert.Equal((true, new ExtractionTimer(ready, DamExtraction.Settings.DefaultExtractionSeconds + DamExtraction.DoorsSeconds)), (open.CanExtract, open.Timer));
        Assert.False(states.Call(0, 43, ready + 4));
        Assert.Equal(new ExtractionPointState(ExtractionState.Closed, null, null), states.Get(1));

        // A late look passes both changes at once with their exact times.
        ExtractionStates late = new(1);
        Assert.True(late.Call(0, 42, 10.0));
        late.Advance(100.0);
        Assert.Equal(new ExtractionPointState(ExtractionState.Ready, 10.0 + ExtractionStates.CallSeconds + DamExtraction.Settings.TimeToExtractReadySeconds, 42), late.Get(0));
    }

    [Fact]
    public void State_changes_are_sent_as_deltas_and_an_open_carries_the_ready_state()
    {
        ScriptedPlayer session = new();
        ExtractionCatalog catalog = session.Raid.Extraction;
        ExtractionPoint point = catalog.Points[0];
        ExtractionPointState closed = new(ExtractionState.Closed, null, null);
        ExtractionPointState called = closed with { CalledBy = 42, CalledAt = 10.0 };
        ExtractionPointState arriving = new(ExtractionState.Arriving, 10.2, 42);
        ExtractionPointState ready = new(ExtractionState.Ready, 55.2, 42);
        Assert.Null(catalog.UpdateBunch(point, 300, closed, closed));
        Assert.Null(catalog.UpdateBunch(point, 300, closed, closed with { CalledBy = 42 }));

        // The press changes only the call button.
        (Dictionary<string, object?>? actor, Dictionary<ulong, ulong> buttons) = Read(catalog.UpdateBunch(point, 300, closed, called)!);
        Assert.Null(actor);
        Assert.Equal(new Dictionary<ulong, ulong> { [point.Buttons[s_summon]] = (ulong)CurrentInteractionState.Ongoing }, buttons);

        (actor, buttons) = Read(catalog.UpdateBunch(point, 300, called, arriving)!);
        Assert.Equal(new Dictionary<string, object?> { [PropertyNames.State] = (ulong)ExtractionState.Arriving, [PropertyNames.StateChangeTimestamp] = 10.2 }, actor);
        Assert.Equal(new Dictionary<ulong, ulong> { [point.Buttons[s_summon]] = (ulong)CurrentInteractionState.SuccessDisabled }, buttons);

        Dictionary<string, object?> readyValues = new()
        {
            [PropertyNames.CanExtract] = true,
            [StartedAtPath] = 55.2,
            [ExtractionTimePath] = DamExtraction.Settings.DefaultExtractionSeconds + DamExtraction.DoorsSeconds,
            [PropertyNames.State] = (ulong)ExtractionState.Ready,
            [PropertyNames.StateChangeTimestamp] = 55.2,
        };
        (actor, buttons) = Read(catalog.UpdateBunch(point, 300, arriving, ready)!);
        Assert.Equal(readyValues, actor);
        int[] descends = [.. Enumerable.Range(0, DamExtraction.Buttons.Count).Where(index => DamExtraction.Buttons[index].Role == ExtractionButtonRole.Descend)];
        Assert.Equal(descends.ToDictionary(index => point.Buttons[index], _ => (ulong)CurrentInteractionState.Idle), buttons);

        // A client that gets the elevator later opens it ready, with the time it became ready.
        ChannelBunch open = catalog.OpenBunch(point, 300, 1, ready, 99.0);
        BitReader reader = new(open.Payload, open.PayloadBits);
        Assert.Equal(point.Guid, NetGuid.Read(reader));
        Dictionary<string, object?> content = Codec.Decode(ActorLayout, ActorContentBlock.Read(reader), swapActorRoles: false);
        Assert.All(readyValues, entry => Assert.Equal(entry.Value, content[entry.Key]));

        // The members sit at the handles the client expects.
        Assert.Equal([25, 27, 28, 29, 30, 31], new[] { PropertyNames.CanExtract, StartedAtPath, ExtractionTimePath, PropertyNames.IsEnabled, PropertyNames.State, PropertyNames.StateChangeTimestamp }
            .Select(path => ActorLayout.Command(path).Handle));
    }

    [Fact]
    public void A_call_press_reaches_every_client_and_a_later_client_opens_the_elevator_ready()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0, 123.5])
        {
            session.Service(at);
        }

        ExtractionPoint point = session.Raid.Extraction.Points[0];
        ulong summon = point.Buttons[s_summon];
        int channel = 64 + session.Raid.Doors.Doors.Count + session.Raid.Ladders.Ladders.Count + session.Raid.Loot.Containers.Count + point.Index;
        double pressed = session.Raid.WorldSeconds(123.5);
        session.Sent.Clear();
        session.Input(30, 33, summon, focus: summon);
        session.Input(33, 36, summon, focus: summon);
        Assert.Equal(1, session.Channels.ElevatorCalls);
        Assert.Equal(new ExtractionPointState(ExtractionState.Closed, null, session.Principal.UserId, pressed), session.Raid.ExtractionStates.Get(point.Index));

        session.Service(123.6);
        (Dictionary<string, object?>? actor, Dictionary<ulong, ulong> buttons) = Read(Assert.Single(session.Sent, bunch => bunch.Channel == channel));
        Assert.Null(actor);
        Assert.Equal(new Dictionary<ulong, ulong> { [summon] = (ulong)CurrentInteractionState.Ongoing }, buttons);

        session.Sent.Clear();
        session.Service(123.5 + ExtractionStates.CallSeconds + 0.05);
        (actor, buttons) = Read(Assert.Single(session.Sent, bunch => bunch.Channel == channel));
        Assert.Equal((ulong)ExtractionState.Arriving, actor![PropertyNames.State]);
        Assert.Equal(pressed + ExtractionStates.CallSeconds, actor[PropertyNames.StateChangeTimestamp]);
        Assert.Equal(new Dictionary<ulong, ulong> { [summon] = (ulong)CurrentInteractionState.SuccessDisabled }, buttons);
        Assert.StartsWith("0:Arriving@", session.Channels.ElevatorStates, StringComparison.Ordinal);

        // Presses on a called elevator and on a descend switch before it is ready change nothing.
        session.Input(36, 39, summon, focus: summon);
        session.Input(39, 42, point.Buttons[IndexOf(ExtractionButtonRole.Descend)]);
        Assert.Equal(1, session.Channels.ElevatorCalls);
        Assert.Equal(ExtractionState.Arriving, session.Raid.ExtractionStates.Get(point.Index).State);

        // A second raider gets the elevator ready, with the time it became ready.
        double readyAt = 123.5 + ExtractionStates.CallSeconds + DamExtraction.Settings.TimeToExtractReadySeconds;
        ScriptedPlayer second = new(session, 43);
        second.Possess();
        foreach (double at in (double[])[readyAt, readyAt + 6, readyAt + 21, readyAt + 21.5, readyAt + 22, readyAt + 22.5, readyAt + 23])
        {
            second.Service(at);
        }

        ChannelBunch open = Assert.Single(second.Sent, bunch => bunch is { Opened: true } && bunch.Channel == channel);
        BitReader reader = new(open.Payload, open.PayloadBits);
        Assert.Equal(point.Guid, NetGuid.Read(reader));
        Dictionary<string, object?> content = Codec.Decode(ActorLayout, ActorContentBlock.Read(reader), swapActorRoles: false);
        Assert.Equal((ulong)ExtractionState.Ready, content[PropertyNames.State]);
        Assert.Equal(pressed + ExtractionStates.CallSeconds + DamExtraction.Settings.TimeToExtractReadySeconds, content[PropertyNames.StateChangeTimestamp]);

        // The first raider gets the change on its open channel.
        session.Sent.Clear();
        session.Service(readyAt + 23.5);
        (actor, buttons) = Read(Assert.Single(session.Sent, bunch => bunch.Channel == channel));
        Assert.Equal((ulong)ExtractionState.Ready, actor![PropertyNames.State]);
        Assert.Equal(true, actor[PropertyNames.CanExtract]);
        Assert.All(buttons.Values, state => Assert.Equal((ulong)CurrentInteractionState.Idle, state));
    }

    [Fact]
    public void No_pawn_frame_resets_the_clients_call_press_until_its_ending_played()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0])
        {
            session.Service(at);
        }

        ulong summon = session.Raid.Extraction.Points[0].Buttons[s_summon];
        Vector3D reported = new(1.0, 2.0, 3.0);
        int frames = session.Channels.BlackboardFrames;
        session.Input(30, 33, focus: summon, position: reported);
        session.Service(123.5);
        Assert.Equal(frames + 1, session.Channels.BlackboardFrames);

        // The call button stops being usable once the elevator arrives; the frame that says so waits.
        session.Input(33, 36, summon, focus: summon, position: reported);
        session.Service(124.0);
        session.Input(36, 39, focus: summon, position: reported);
        session.Service(123.5 + ExtractionStates.CallSeconds + 1);
        Assert.Equal(frames + 1, session.Channels.BlackboardFrames);

        session.Input(39, 42, focus: summon, position: reported);
        session.Service(123.5 + ExtractionStates.CallSeconds + 3);
        Assert.Equal(frames + 2, session.Channels.BlackboardFrames);
    }

    [Fact]
    public void A_held_descend_switch_closes_the_doors_and_the_elevator_leaves_once_they_are_closed()
    {
        const double Ready = 557.9;
        ExtractionStates states = ReadyStates(Ready);
        int a = IndexOf(ExtractionButtonRole.Descend);
        int b = DamExtraction.Buttons.ToList().FindIndex(a + 1, button => button.Role == ExtractionButtonRole.Descend);
        ExtractionButton descend = DamExtraction.Buttons[a];
        Assert.Null(states.Get(0).HoldStarts[a]);

        // The press: the switch runs while the character lines up; nobody else can start a hold.
        const double Pressed = 562.666;
        Assert.False(states.Descend(0, s_summon, 42, Pressed));
        Assert.True(states.Descend(0, a, 42, Pressed));
        Assert.False(states.Descend(0, b, 43, Pressed + 0.1));
        ExtractionPointState aligning = states.Get(0);
        Assert.Equal((CurrentInteractionState.Ongoing, CurrentInteractionState.Idle), (aligning.Buttons[a], aligning.Buttons[b]));
        Assert.Equal((DamExtraction.NoHoldStartedTime, DamExtraction.NoHoldStartedTime), (aligning.HoldStarts[a], aligning.HoldStarts[b]));
        Assert.All(DamExtraction.Buttons.Select((button, index) => (button, index)).Where(entry => entry.button.Role != ExtractionButtonRole.Descend),
            entry => Assert.Null(aligning.HoldStarts[entry.index]));

        // The hold starts after the switch's alignment.
        double holdStart = Pressed + descend.AlignmentSeconds;
        states.Advance(holdStart - 0.01);
        Assert.Equal(aligning, states.Get(0));
        states.Advance(holdStart);
        Assert.Equal(holdStart, states.Get(0).HoldStarts[a]);

        // The doors start closing when the settings' extraction hold is over.
        double closing = holdStart + DamExtraction.Settings.ExtractionHoldSeconds;
        states.Advance(closing - 0.01, _ => [7]);
        Assert.Equal(ExtractionState.Ready, states.Get(0).State);
        Assert.Null(states.Departure(0));
        states.Advance(closing + 0.02, index => index == 0 ? [42, 43] : []);
        ExtractionPointState doors = states.Get(0);
        Assert.Equal((ExtractionState.Closing, (double?)closing, true), (doors.State, doors.ChangedAt, doors.CanExtract));
        Assert.Equal(new ExtractionTimer(Ready, closing - Ready + DamExtraction.DoorsSeconds), doors.Timer);
        Assert.Equal((CurrentInteractionState.SuccessDisabled, CurrentInteractionState.SuccessDisabled, CurrentInteractionState.Disabled),
            (doors.Buttons[s_summon], doors.Buttons[a], doors.Buttons[b]));
        Assert.Equal((DamExtraction.NoHoldStartedTime, DamExtraction.NoHoldStartedTime), (doors.HoldStarts[a], doors.HoldStarts[b]));
        Assert.Equal(new ExtractionDescent(a, 42, Pressed, Holding: true), doors.Departed);
        Assert.Null(states.Departure(0));

        // It has left once the doors are closed, with the players inside then (one stepped out while they closed):
        // callable again, the used switch stays succeeded until the next arrival.
        states.Advance(closing + DamExtraction.DoorsSeconds - 0.01, index => index == 0 ? [42, 43] : []);
        Assert.Equal(ExtractionState.Closing, states.Get(0).State);
        Assert.Null(states.Departure(0));
        states.Advance(closing + DamExtraction.DoorsSeconds, index => index == 0 ? [43] : []);
        Assert.Equal(new ExtractionDeparture(closing, [43]), states.Departure(0), new DepartureComparer());
        ExtractionPointState left = states.Get(0);
        Assert.Equal((ExtractionState.Closed, (double?)(closing + DamExtraction.DoorsSeconds), (long?)null, false), (left.State, left.ChangedAt, left.CalledBy, left.CanExtract));
        Assert.Equal(new ExtractionTimer(DamExtraction.NoExtractionStartedTime, doors.Timer!.Value.Seconds), left.Timer);
        Assert.Equal((CurrentInteractionState.Idle, CurrentInteractionState.SuccessDisabled, CurrentInteractionState.Disabled),
            (left.Buttons[s_summon], left.Buttons[a], left.Buttons[b]));

        // The next cycle starts over: both switches usable again, no hold running.
        Assert.True(states.Call(0, 43, 600.0));
        states.Advance(600.0 + ExtractionStates.CallSeconds + DamExtraction.Settings.TimeToExtractReadySeconds);
        ExtractionPointState again = states.Get(0);
        Assert.Equal((ExtractionState.Ready, CurrentInteractionState.Idle, CurrentInteractionState.Idle), (again.State, again.Buttons[a], again.Buttons[b]));
        Assert.Equal((DamExtraction.NoHoldStartedTime, DamExtraction.NoHoldStartedTime), (again.HoldStarts[a], again.HoldStarts[b]));
        Assert.Equal(new ExtractionTimer(again.ChangedAt!.Value, DamExtraction.Settings.DefaultExtractionSeconds + DamExtraction.DoorsSeconds), again.Timer);
    }

    [Fact]
    public void A_ready_elevator_nobody_descends_with_closes_after_the_extraction_time()
    {
        const double Ready = 175.716;
        ExtractionStates states = ReadyStates(Ready);
        int a = IndexOf(ExtractionButtonRole.Descend);
        double timeout = Ready + DamExtraction.Settings.DefaultExtractionSeconds;

        // A hold that would end after the extraction time does not keep the elevator: the doors close on time.
        Assert.True(states.Descend(0, a, 42, timeout - 2));
        states.Advance(timeout - 0.01);
        Assert.Equal(ExtractionState.Ready, states.Get(0).State);
        states.Advance(timeout + 1, _ => []);
        ExtractionPointState doors = states.Get(0);
        Assert.Equal((ExtractionState.Closing, (double?)timeout, (ExtractionDescent?)null, (ExtractionDescent?)null), (doors.State, doors.ChangedAt, doors.Descent, doors.Departed));
        Assert.Equal(new ExtractionTimer(Ready, timeout - Ready + DamExtraction.DoorsSeconds), doors.Timer);
        Assert.Equal(ByRole(CurrentInteractionState.SuccessDisabled, CurrentInteractionState.Disabled), doors.Buttons);
        Assert.Null(states.Departure(0));

        states.Advance(timeout + DamExtraction.DoorsSeconds, _ => []);
        Assert.Equal(new ExtractionDeparture(timeout, []), states.Departure(0), new DepartureComparer());
        ExtractionPointState left = states.Get(0);
        Assert.Equal((ExtractionState.Closed, (double?)(timeout + DamExtraction.DoorsSeconds)), (left.State, left.ChangedAt));
        Assert.Equal(ByRole(CurrentInteractionState.Idle, CurrentInteractionState.Disabled), left.Buttons);
        Assert.Equal(new ExtractionTimer(DamExtraction.NoExtractionStartedTime, doors.Timer!.Value.Seconds), left.Timer);

        // A late look passes Closing and Closed at once with their exact times.
        ExtractionStates late = ReadyStates(Ready);
        late.Advance(timeout + 100);
        Assert.Equal((ExtractionState.Closed, (double?)(timeout + DamExtraction.DoorsSeconds)), (late.Get(0).State, late.Get(0).ChangedAt));
        Assert.Equal(timeout, late.Departure(0)!.At);
    }

    [Fact]
    public void A_cancelled_hold_leaves_the_switch_usable()
    {
        ExtractionStates states = ReadyStates(10.0);
        int a = IndexOf(ExtractionButtonRole.Descend);
        Assert.True(states.Descend(0, a, 42, 20.0));
        Assert.False(states.CancelDescent(0, 43));
        Assert.True(states.CancelDescent(0, 42));
        ExtractionPointState state = states.Get(0);
        Assert.Equal((ExtractionState.Ready, (ExtractionDescent?)null, true), (state.State, state.Descent, state.HoldRan));
        Assert.Equal(ByRole(CurrentInteractionState.SuccessDisabled, CurrentInteractionState.Idle), state.Buttons);
        states.Advance(30.0);
        Assert.Equal(ExtractionState.Ready, states.Get(0).State);
    }

    [Fact]
    public void Hold_and_closing_changes_are_sent_as_deltas_with_the_hold_start_time()
    {
        ScriptedPlayer session = new();
        ExtractionCatalog catalog = session.Raid.Extraction;
        ExtractionPoint point = catalog.Points[0];
        int a = IndexOf(ExtractionButtonRole.Descend);
        int b = DamExtraction.Buttons.ToList().FindIndex(a + 1, button => button.Role == ExtractionButtonRole.Descend);
        ExtractionPointState ready = new(ExtractionState.Ready, 55.2, 42);
        ExtractionDescent pressed = new(a, 42, 60.0);
        ExtractionPointState aligning = ready with { Descent = pressed, HoldRan = true };
        ExtractionPointState holding = aligning with { Descent = pressed with { Holding = true } };
        double closing = pressed.EndsAt;
        ExtractionPointState doors = new(ExtractionState.Closing, closing, 42) { ReadyAt = 55.2, ClosingAt = closing, Departed = holding.Descent, HoldRan = true };

        (Dictionary<string, object?>? actor, Dictionary<ulong, (ulong State, double? Hold)> buttons) = ReadHolds(catalog.UpdateBunch(point, 300, ready, aligning)!);
        Assert.Null(actor);
        Assert.Equal(new Dictionary<ulong, (ulong, double?)>
        {
            [point.Buttons[a]] = ((ulong)CurrentInteractionState.Ongoing, DamExtraction.NoHoldStartedTime),
            [point.Buttons[b]] = ((ulong)CurrentInteractionState.Idle, DamExtraction.NoHoldStartedTime),
        }, buttons);

        (actor, buttons) = ReadHolds(catalog.UpdateBunch(point, 300, aligning, holding)!);
        Assert.Null(actor);
        Assert.Equal(new Dictionary<ulong, (ulong, double?)> { [point.Buttons[a]] = ((ulong)CurrentInteractionState.Ongoing, pressed.HoldStartsAt) }, buttons);

        (actor, buttons) = ReadHolds(catalog.UpdateBunch(point, 300, holding, doors)!);
        Assert.Equal(new Dictionary<string, object?>
        {
            [PropertyNames.State] = (ulong)ExtractionState.Closing,
            [PropertyNames.StateChangeTimestamp] = closing,
            [ExtractionTimePath] = closing - 55.2 + DamExtraction.DoorsSeconds,
        }, actor);
        Assert.Equal(new Dictionary<ulong, (ulong, double?)>
        {
            [point.Buttons[a]] = ((ulong)CurrentInteractionState.SuccessDisabled, DamExtraction.NoHoldStartedTime),
            [point.Buttons[b]] = ((ulong)CurrentInteractionState.Disabled, DamExtraction.NoHoldStartedTime),
        }, buttons);

        ExtractionPointState left = doors with { State = ExtractionState.Closed, ChangedAt = closing + DamExtraction.DoorsSeconds, CalledBy = null };
        (actor, buttons) = ReadHolds(catalog.UpdateBunch(point, 300, doors, left)!);
        Assert.Equal(new Dictionary<string, object?>
        {
            [PropertyNames.CanExtract] = false,
            [StartedAtPath] = DamExtraction.NoExtractionStartedTime,
            [PropertyNames.State] = (ulong)ExtractionState.Closed,
            [PropertyNames.StateChangeTimestamp] = closing + DamExtraction.DoorsSeconds,
        }, actor);
        Assert.Equal(new Dictionary<ulong, (ulong, double?)> { [point.Buttons[s_summon]] = ((ulong)CurrentInteractionState.Idle, null) }, buttons);
    }

    [Fact]
    public void The_extraction_zone_is_a_box_turned_with_the_elevator()
    {
        ScriptedPlayer session = new();
        foreach (ExtractionPoint point in session.Raid.Extraction.Points)
        {
            ExtractionPointPlacement placement = DamExtraction.All[point.Index];
            Vector3D half = DamExtraction.ZoneHalfExtent;
            Vector3D centre = Add(placement.Location, placement.Rotation.Rotate(DamExtraction.ZoneLocation));
            Assert.True(point.Contains(centre));
            Assert.True(point.Contains(Add(centre, placement.Rotation.Rotate(new Vector3D(half.X - 1, half.Y - 1, half.Z - 1)))));
            Assert.False(point.Contains(Add(centre, placement.Rotation.Rotate(new Vector3D(half.X + 1, 0, 0)))));
            Assert.False(point.Contains(Add(centre, placement.Rotation.Rotate(new Vector3D(0, -half.Y - 1, 0)))));
            Assert.False(point.Contains(Add(centre, new Vector3D(0, 0, half.Z + 1))));
            Assert.All(session.Raid.Extraction.Points.Where(other => other != point), other => Assert.False(other.Contains(centre)));
        }
    }

    [Fact]
    public void A_descend_hold_runs_on_the_client_then_the_server_ends_it_and_records_who_is_inside()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0, 123.5])
        {
            session.Service(at);
        }

        ExtractionPoint point = session.Raid.Extraction.Points[0];
        int a = IndexOf(ExtractionButtonRole.Descend);
        ulong descend = point.Buttons[a];
        int channel = 64 + session.Raid.Doors.Doors.Count + session.Raid.Ladders.Ladders.Count + session.Raid.Loot.Containers.Count + point.Index;
        double ready = 123.5 + ExtractionStates.CallSeconds + DamExtraction.Settings.TimeToExtractReadySeconds;
        Assert.True(session.Raid.ExtractionStates.Call(point.Index, 43, session.Raid.WorldSeconds(123.5)));
        session.Service(ready + 0.1);
        Assert.Equal(ExtractionState.Ready, session.Raid.ExtractionStates.Get(point.Index).State);

        // Standing inside the zone, one press starts the hold; no pawn frame is sent while the client runs it.
        ExtractionPointPlacement placement = DamExtraction.All[point.Index];
        Vector3D inside = Add(placement.Location, placement.Rotation.Rotate(DamExtraction.ZoneLocation));
        session.Input(30, 33, focus: descend, position: inside);
        session.Service(ready + 0.5);
        double pressedAt = ready + 1;
        session.Service(pressedAt);
        session.Input(33, 36, descend, focus: descend, position: inside);
        Assert.Equal(1, session.Channels.ElevatorDescents);
        Assert.Equal(new ExtractionDescent(a, session.Principal.UserId, session.Raid.WorldSeconds(pressedAt)), session.Raid.ExtractionStates.Get(point.Index).Descent);
        int frames = session.Channels.BlackboardFrames;
        session.Sent.Clear();
        session.Service(pressedAt + 0.2);
        (_, Dictionary<ulong, (ulong State, double? Hold)> buttons) = ReadHolds(Assert.Single(session.Sent, bunch => bunch.Channel == channel));
        Assert.Equal(((ulong)CurrentInteractionState.Ongoing, (double?)DamExtraction.NoHoldStartedTime), buttons[descend]);

        double holdAt = pressedAt + DamExtraction.Buttons[a].AlignmentSeconds;
        double endAt = holdAt + DamExtraction.Settings.ExtractionHoldSeconds;
        session.Input(36, 39, focus: descend, position: inside);
        session.Service(holdAt + 0.1);
        Assert.Equal(session.Raid.WorldSeconds(holdAt), session.Raid.ExtractionStates.Get(point.Index).HoldStarts[a]!.Value, 6);
        Assert.Contains("0:Ready@", session.Channels.ElevatorStates, StringComparison.Ordinal);
        Assert.Contains($"+{DamExtraction.Buttons[a].Component}@", session.Channels.ElevatorStates, StringComparison.Ordinal);
        Assert.Contains("~hold@", session.Channels.ElevatorStates, StringComparison.Ordinal);

        // The hold succeeds: the doors close, and the character's interactor ends the
        // interaction.
        session.Input(39, 42, focus: descend, position: inside);
        session.Sent.Clear();
        session.Service(endAt + 0.05);
        Assert.Equal(ExtractionState.Closing, session.Raid.ExtractionStates.Get(point.Index).State);
        Assert.Null(session.Raid.ExtractionStates.Departure(point.Index));
        Assert.Equal(1, session.Channels.CompletedDescents);
        Assert.Contains(session.Sent, bunch => bunch.Channel == 6);
        Assert.Equal(frames, session.Channels.BlackboardFrames);
        Assert.Empty(session.Channels.ElevatorDepartures);

        // The elevator leaves with this player still inside once the doors have closed.
        session.Input(42, 45, focus: descend, position: inside);
        session.Service(endAt + DamExtraction.DoorsSeconds + 0.05);
        Assert.Equal([session.Principal.UserId], session.Raid.ExtractionStates.Departure(point.Index)!.Players);
        Assert.Matches(@"^0@\d+\.\d{3}:42$", session.Channels.ElevatorDepartures);
        Assert.Contains(session.Channels.ElevatorChanges, change => change.StartsWith("0:Closing@", StringComparison.Ordinal) && change.EndsWith($"^{DamExtraction.Buttons[a].Component}", StringComparison.Ordinal));
        Assert.Contains(session.Channels.ElevatorChanges, change => change.StartsWith("0:Closed@", StringComparison.Ordinal));

        // Frames resume after the ending; the switch is no longer focused.
        session.Input(45, 48, focus: descend, position: inside);
        session.Service(endAt + DamExtraction.DoorsSeconds + 1);
        Assert.Equal(frames + 1, session.Channels.BlackboardFrames);
    }

    private static ExtractionStates ReadyStates(double ready)
    {
        ExtractionStates states = new(2);
        double called = ready - DamExtraction.Settings.TimeToExtractReadySeconds - ExtractionStates.CallSeconds;
        Assert.True(states.Call(0, 42, called));
        states.Advance(ready);
        Assert.Equal(new ExtractionPointState(ExtractionState.Ready, ready, 42), states.Get(0));
        return states;
    }

    private static Vector3D Add(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    private static ValueCodec Values { get; } = new(GameReplication.Catalog, [KnownEnums.NetRole, DamDoors.InteractionState, DamExtraction.StateEnum]);

    private static PropertyCodec Codec { get; } = new(Values);

    private static PropertyLayout ActorLayout { get; } = new(GameReplication.Catalog, DamExtraction.ActorClass);

    private static int IndexOf(ExtractionButtonRole role) => DamExtraction.Buttons.ToList().FindIndex(button => button.Role == role);

    private static CurrentInteractionState[] ByRole(CurrentInteractionState summon, CurrentInteractionState descend) =>
        [.. DamExtraction.Buttons.Select(button => button.Role switch
        {
            ExtractionButtonRole.Summon => summon,
            ExtractionButtonRole.Descend => descend,
            _ => CurrentInteractionState.Disabled,
        })];

    // The elevator's own changes and its buttons' states with their hold start times (null when not sent).
    private static (Dictionary<string, object?>? Actor, Dictionary<ulong, (ulong State, double? Hold)> Buttons) ReadHolds(ChannelBunch bunch)
    {
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        Dictionary<string, object?>? actor = null;
        Dictionary<ulong, (ulong, double?)> buttons = [];
        PropertyLayout button = new(GameReplication.Catalog, DamExtraction.Buttons[0].Class);
        while (reader.Remaining > 0)
        {
            bool layout = reader.ReadBit();
            if (reader.ReadBit())
            {
                uint size = reader.ReadPacked();
                actor = Codec.Decode(ActorLayout, new ActorContentBlock(reader.ReadBitArray(size), size, layout), swapActorRoles: false);
                continue;
            }

            ulong guid = NetGuid.Read(reader);
            Assert.True(reader.ReadBit());
            uint length = reader.ReadPacked();
            Dictionary<string, object?> values = Codec.Decode(button, new ActorContentBlock(reader.ReadBitArray(length), length, layout), swapActorRoles: false);
            buttons[guid] = ((ulong)values[PropertyNames.CurrentInteractionState]!, values.TryGetValue(PropertyNames.InteractionServerHoldStartedTime, out object? hold) ? (double?)hold : null);
        }

        return (actor, buttons);
    }

    private sealed class DepartureComparer : IEqualityComparer<ExtractionDeparture?>
    {
        public bool Equals(ExtractionDeparture? x, ExtractionDeparture? y) =>
            x is null ? y is null : y is not null && x.At.Equals(y.At) && x.Players.SequenceEqual(y.Players);

        public int GetHashCode(ExtractionDeparture? obj) => obj?.At.GetHashCode() ?? 0;
    }

    // The elevator's own changes and its buttons' states, by button id, from an update on its open channel.
    private static (Dictionary<string, object?>? Actor, Dictionary<ulong, ulong> Buttons) Read(ChannelBunch bunch)
    {
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        Dictionary<string, object?>? actor = null;
        Dictionary<ulong, ulong> buttons = [];
        PropertyLayout button = new(GameReplication.Catalog, DamExtraction.Buttons[0].Class);
        while (reader.Remaining > 0)
        {
            bool layout = reader.ReadBit();
            if (reader.ReadBit())
            {
                uint size = reader.ReadPacked();
                actor = Codec.Decode(ActorLayout, new ActorContentBlock(reader.ReadBitArray(size), size, layout), swapActorRoles: false);
                continue;
            }

            ulong guid = NetGuid.Read(reader);
            Assert.True(reader.ReadBit());
            uint length = reader.ReadPacked();
            Dictionary<string, object?> values = Codec.Decode(button, new ActorContentBlock(reader.ReadBitArray(length), length, layout), swapActorRoles: false);
            buttons[guid] = (ulong)values[PropertyNames.CurrentInteractionState]!;
        }

        return (actor, buttons);
    }
}

