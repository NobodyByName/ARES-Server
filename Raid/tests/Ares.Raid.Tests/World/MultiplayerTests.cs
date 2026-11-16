using Ares.Common.Game.Inventory;
using Ares.Raid.Tests.World.Movement;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Players;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class MultiplayerTests
{
    [Fact]
    public void Two_parties_share_one_raid_with_their_own_actors_inventory_and_start()
    {
        ScriptedPlayer first = new(loadout: InventoryTests.StandardLoadout());
        ScriptedPlayer second = new(first, 43);
        first.Possess();
        second.Possess();

        Assert.Same(first.Raid, second.Raid);
        Assert.Equal([42L, 43L], first.Raid.Players.Select(player => player.Id));
        Assert.Equal((DeliveryPhase.Possessed, DeliveryPhase.Possessed), (first.Channels.Phase, second.Channels.Phase));
        Assert.True(first.Player.PossessionAcknowledged && second.Player.PossessionAcknowledged);
        Assert.Empty(new[] { first.Player.Controller, first.Player.PlayerState, first.Player.Pawn, first.Player.Squad }
            .Intersect([second.Player.Controller, second.Player.PlayerState, second.Player.Pawn, second.Player.Squad]));
        Assert.NotEqual(first.Raid.State.Actor(first.Player.Squad).Properties[PropertyNames.SquadId],
            second.Raid.State.Actor(second.Player.Squad).Properties[PropertyNames.SquadId]);
        Assert.NotSame(first.Member.Inventory, second.Member.Inventory);
        Assert.NotEqual(first.Member.Inventory.Containers.Sum(container => container.Items.Count(item => item is not null)),
            second.Member.Inventory.Containers.Sum(container => container.Items.Count(item => item is not null)));
        Assert.Equal(first.Member.Spawn.Start, second.Member.Spawn.Start);
        Assert.Equal((0, 1), (first.Member.Spawn.Slot, second.Member.Spawn.Slot));
    }

    [Fact]
    public void A_surrendering_player_leaves_while_the_raid_goes_on_until_the_last_one_leaves()
    {
        ScriptedPlayer first = new();
        ScriptedPlayer second = new(first, 43);
        first.Possess();
        second.Possess();
        RaidWorld raid = first.Raid;

        Surrender(first);
        Assert.True(first.Member.Left);
        Assert.True(first.Worlds.HasLeft(raid.MatchId, 42));
        Assert.False(first.Worlds.HasLeft(raid.MatchId, 43));
        Assert.False(raid.Ended);
        Assert.True(first.Worlds.AcceptingPlayers(raid.MatchId));
        Assert.Null(first.Worlds.WelcomeFor(first.Principal));
        Assert.Equal(RaidWorld.Welcome, first.Worlds.WelcomeFor(second.Principal));
        Assert.Throws<WorldPreparationException>(() => first.Worlds.Join(raid.MatchId, [42]));

        Surrender(second);
        Assert.True(raid.Ended);
        Assert.False(first.Worlds.AcceptingPlayers(raid.MatchId));
        Assert.Throws<WorldPreparationException>(() => first.Worlds.Join(raid.MatchId, [44]));
    }

    [Fact]
    public void An_expired_reservation_takes_its_player_out_of_the_raid()
    {
        ScriptedPlayer first = new();
        ScriptedPlayer second = new(first, 43);
        string match = first.Raid.MatchId;
        first.Worlds.Leave("another-match", [43]);
        Assert.False(second.Member.Left);

        first.Worlds.Leave(match, [43, 99]);
        Assert.True(second.Member.Left);
        Assert.Null(first.Worlds.WelcomeFor(second.Principal));
        Assert.Throws<InvalidPacketException>(() => first.Worlds.StartPlayer(second.Principal, new PlayerLogin(7, 0, new UniqueNetId(5, "43", "", false, false)), second.Io));
        Assert.False(first.Raid.Ended);
    }

    [Fact]
    public void Every_party_shares_one_start_and_the_raid_refuses_players_it_cannot_place()
    {
        DamWorlds worlds = new(() => 0, null, () => 7);
        worlds.Prepare("a", [1]);
        RaidWorld raid = worlds.Current!;
        IReadOnlyList<PlayerStart> starts = Spawns.Eligible(DamRounds.All[raid.Seed % DamRounds.All.Count]);
        int points = starts[raid.Seed % starts.Count].SpawnPoints.Count;
        Assert.Throws<WorldPreparationException>(() => worlds.Join("b", [2]));
        Assert.Throws<WorldPreparationException>(() => worlds.Join("a", [1]));
        for (long player = 2; player <= points; player++)
        {
            worlds.Join("a", [player]);
        }

        Assert.Throws<WorldPreparationException>(() => worlds.Join("a", [points + 1]));
        Assert.Single(raid.Players.Select(player => player.Spawn.Start).Distinct());
        Assert.Equal(Enumerable.Range(0, points).Select(slot => (int?)slot), raid.Players.Select(player => player.Spawn.Slot));
    }

    [Fact]
    public void Each_player_sees_the_other_as_a_simulated_character_that_moves_holds_its_gun_and_goes_when_they_leave()
    {
        ScriptedPlayer first = new();
        ScriptedPlayer second = new(first, 43, WeaponTests.WithWeapons() with { UserId = 43 });
        first.Possess();
        second.Possess();
        (_, double at) = first.Settle(30, 101);
        (uint frame, at) = second.Settle(30, at);

        // Nothing of the other player before the map actors' start delay.
        first.Sent.Clear();
        first.Service(at);
        Assert.DoesNotContain(first.Sent, bunch => OpenedActor(bunch) == second.Player.Pawn);

        Vector3D moved = second.Member.Spawn.Location with { X = second.Member.Spawn.Location.X + 50 };
        second.Input(frame, frame + 3, position: moved);
        double publishedAt = first.Raid.WorldSeconds(first.Raid.Clock()) * PawnBlackboard.TimePerSecond;
        at += 20;
        for (int step = 0; step < 5; step++)
        {
            first.Service(at += 0.1);
        }

        ChannelBunch state = Assert.Single(first.Sent, bunch => OpenedActor(bunch) == second.Player.PlayerState);
        ChannelBunch pawn = Assert.Single(first.Sent, bunch => OpenedActor(bunch) == second.Player.Pawn);
        Assert.Equal(state.Channel + 1, pawn.Channel);
        Assert.DoesNotContain(first.Sent, bunch => OpenedActor(bunch) == first.Player.Pawn);
        PlayerMotion shown = second.Member.Motion!;
        IReadOnlyDictionary<int, PendulumSlot> openState = PawnBlackboardFrames.State(shown.Tick, shown.Motion, 0, weapons: shown.Weapons, timeOffset: shown.TimeOffset);
        ActorContentBlock expected = WorldState.Merge(first.Raid.State.ObservedPawnProperties(second.Player, WorldState.CompressedPitch(shown.Pitch)),
            first.Raid.State.BlackboardFrame(0, shown.Tick, openState, new Dictionary<int, PendulumSlot>(), PendulumCodec.Others));
        Assert.Equal(expected, Assert.Single(PawnContent(pawn)), new ContentComparer());
        Assert.True(Distance(moved, shown.Motion.Position) < 1);
        // The frame is on the raid's clock: its simulation time plus its time offset is the world time (the time on
        // the raid's packets) the movement was published at, to within one step of the offset.
        Assert.InRange(((long)shown.Tick * PawnBlackboard.TimePerTick) + shown.TimeOffset - publishedAt, -PawnBlackboard.TimeOffsetStep, PawnBlackboard.TimeOffsetStep);
        Assert.Equal(0, shown.TimeOffset % PawnBlackboard.TimePerTick);
        Assert.Contains(first.Sent, bunch => bunch.Channel == state.Channel && bunch is { Opened: false, PayloadBits: > 0 });
        Dictionary<int, PendulumSlot> opened = new(openState);

        // The loot containers around the start reach the first client first, so its packets then carry only characters.
        while (first.Channels.LootWaiting > 0 || first.Channels.LootChannelsInUse > 0)
        {
            first.Service(at += 0.1);
        }

        // The next movement arrives as a frame on the character's channel.
        Vector3D further = moved with { Y = moved.Y + 30 };
        second.Input(frame + 3, frame + 6, position: further);
        first.Sent.Clear();
        first.Service(at += 0.1);
        Dictionary<int, PendulumSlot> next = first.PawnFrame(opened, pawn.Channel, PendulumCodec.Others);
        PlayerMotion latest = second.Member.Motion!;
        Assert.Equal((shown.Tick, latest.Tick), first.LastFrameNumbers);
        Assert.All(first.Sent.Where(bunch => bunch.Channel == pawn.Channel), bunch => Assert.Null(bunch.Sequence));
        Assert.True(Distance(further, latest.Motion.Position) < 1);
        Assert.Equal(PawnBlackboardFrames.State(latest.Tick, latest.Motion, 0)[PawnBlackboard.MovementStateSlot].Value,
            next[PawnBlackboard.MovementStateSlot].Value);

        // A lost frame is not resent; the next one is coded against the last frame the client confirmed.
        second.Input(frame + 6, frame + 9, position: further with { X = further.X + 30 });
        first.Sent.Clear();
        first.LoseNextPacket = true;
        first.Service(at += 0.1);
        uint lostTick = second.Member.Motion!.Tick;
        second.Input(frame + 9, frame + 12, position: further with { X = further.X + 60 });
        first.Sent.Clear();
        first.Service(at += 0.1);
        first.PawnFrame(next, pawn.Channel, PendulumCodec.Others);
        Assert.Equal((latest.Tick, second.Member.Motion!.Tick), first.LastFrameNumbers);
        Assert.True(lostTick > latest.Tick && second.Member.Motion!.Tick > lostTick);

        // The gun in the second player's hands shows by item type, without an item link the first client lacks.
        PendulumField[] weaponFields = [.. PendulumRegistry.Descriptors[WeaponProtocol.WeaponDescriptor].Fields];
        byte[] gun = next[WeaponProtocol.FirstWeaponSlot].Value;
        PendulumVariable equipped = latest.Weapons.Single(variable => variable.Slot == WeaponProtocol.FirstWeaponSlot);
        ulong asset = equipped.Values.OfType<PendulumIntegerValue>().Single(value => value.Field == WeaponProtocol.WeaponAssetField).Value;
        Assert.NotEqual(0UL, asset);
        Assert.Equal(asset, PendulumCodec.Integer(weaponFields[WeaponProtocol.WeaponAssetField], gun));
        Assert.Equal(0UL, PendulumCodec.Integer(weaponFields[WeaponProtocol.WeaponItemInstanceField], gun));

        // Once the confirmed frame is a client history behind, the client may have overwritten it: a reset follows.
        uint confirmedTick = second.Member.Motion!.Tick;
        uint input = frame + 12;
        while (second.Member.Motion!.Tick - confirmedTick < PendulumFrames.History)
        {
            second.Input(input, input + 3, position: further with { X = further.X + 60 + input - frame });
            input += 3;
            first.LoseNextPacket = true;
            first.Service(at += 0.1);
        }

        second.Input(input, input + 3, position: further with { X = further.X + 60 + input - frame });
        first.Sent.Clear();
        first.HoldAcknowledgements = true;
        first.Service(at += 0.1);
        Dictionary<int, PendulumSlot> reset = first.PawnFrame(new Dictionary<int, PendulumSlot>(), pawn.Channel, PendulumCodec.Others);
        uint resetTick = second.Member.Motion!.Tick;
        Assert.Equal((0U, resetTick), first.LastFrameNumbers);
        Assert.Equal(PawnBlackboardFrames.State(second.Member.Motion!.Tick, second.Member.Motion!.Motion, 0)[PawnBlackboard.MovementStateSlot].Value,
            reset[PawnBlackboard.MovementStateSlot].Value);

        // A second reset could clear this one from the client history before its acknowledgement reaches us.
        second.Input(input + 3, input + 6, position: further with { X = further.X + 63 + input - frame });
        first.Sent.Clear();
        first.Service(at += 0.1);
        Assert.DoesNotContain(first.Sent, bunch => bunch.Channel == pawn.Channel);

        first.HoldAcknowledgements = false;
        first.AcknowledgeSent();
        first.Service(at += 0.1);
        first.PawnFrame(reset, pawn.Channel, PendulumCodec.Others);
        Assert.Equal((resetTick, second.Member.Motion!.Tick), first.LastFrameNumbers);

        // The second player sees the first one too.
        second.Sent.Clear();
        for (int step = 0; step < 5; step++)
        {
            second.Service(at += 0.1);
        }

        Assert.Contains(second.Sent, bunch => OpenedActor(bunch) == first.Player.Pawn);

        // Leaving removes the character and player state from the others.
        Surrender(second);
        first.Sent.Clear();
        first.Service(at += 0.1);
        Assert.Equal([(pawn.Channel, Bunches.Destroyed), (state.Channel, Bunches.Destroyed)],
            first.Sent.Where(bunch => bunch.CloseReason is not null).Select(bunch => (bunch.Channel, bunch.CloseReason!.Value)));
        first.Sent.Clear();
        first.Service(at + 1);
        Assert.DoesNotContain(first.Sent, bunch => bunch.Channel == pawn.Channel || bunch.Channel == state.Channel);
    }

    [Fact]
    public void Other_players_see_a_snap_hook_throw_and_pull_as_it_happens_while_the_throwers_own_frames_are_held()
    {
        // A floor just under the second player's start and a wall ahead of it (the default aim looks along +X at the feet).
        PlayerLoadout hooked = GadgetTests.AddSnapHook(WeaponTests.WithWeapons() with { UserId = 43 }, 0);
        Vector3D start = new ScriptedPlayer(new ScriptedPlayer(), 43, hooked).Member.Spawn.Location;
        ScriptedPlayer first = new(collision: ScriptedClient.Map(
            (start with { Z = start.Z - 11 }, new System.Numerics.Vector3(3000, 3000, 10)),
            (start with { X = start.X + 600 }, new System.Numerics.Vector3(50, 2000, 2000))));
        ScriptedPlayer second = new(first, 43, hooked);
        first.Possess();
        second.Possess();
        (_, double at) = first.Settle(30, 101, first.Member.Spawn.Location);
        (uint frame, at) = second.Settle(30, at, start);
        Assert.Equal(start, second.Member.Spawn.Location);
        at += 20;
        for (int step = 0; step < 5; step++)
        {
            second.Input(frame, frame + 1, position: start);
            frame++;
            first.Service(at += 0.1);
        }

        while (first.Channels.LootWaiting > 0 || first.Channels.LootChannelsInUse > 0)
        {
            first.Service(at += 0.1);
        }

        ChannelBunch pawn = Assert.Single(first.Sent, bunch => OpenedActor(bunch) == second.Player.Pawn);
        first.Sent.Clear();
        second.Input(frame, frame + 1, position: start);
        frame++;
        first.Service(at += 0.1);
        PlayerMotion opened = second.Member.Motion!;
        Dictionary<int, PendulumSlot> seen = new(PawnBlackboardFrames.State(opened.Tick, opened.Motion, 0, weapons: opened.Weapons, timeOffset: opened.TimeOffset));
        seen = first.PawnFrame(seen, pawn.Channel, PendulumCodec.Others);

        // Draw the hook, throw it at the wall, ride the pull towards the wall, then let the client allow the next use.
        const ulong Allowed = 1UL << WeaponProtocol.AllowPrimaryBit;
        const ulong Throw = Allowed | (1UL << WeaponProtocol.FireDownBit) | (1UL << WeaponProtocol.FirePressedBit);
        RaidInventory inventory = second.Member.Inventory;
        second.Equip(frame + 1, second.Player.HeldItems[inventory.Containers[inventory.Belt].Items[0]!.Id], 1);
        uint thrown = frame + 60;
        uint? released = null;
        Vector3D position = start;
        Dictionary<uint, Vector3D> reports = [];
        List<(uint Tick, Dictionary<int, PendulumSlot> State, PlayerMotion Motion)> shown = [];
        for (int step = 0; step < 240 && (released is null || frame < released + 10); step++)
        {
            CharacterState? thrower = second.Member.Motion?.Motion.Character;
            if (thrower?.SnapHook.Pull is not null)
            {
                position = position with { X = position.X + 15 };
            }

            if (released is null && frame > thrown && thrower is { SnapHook: { Busy: false, Target: not null } })
            {
                released = frame + 15;
            }

            ulong flags = frame + 1 == thrown ? Throw : frame + 1 < thrown || frame + 1 >= released ? Allowed : 0;
            second.Input(frame, frame + 1, position: position, weaponFlags: flags);
            frame++;
            reports[frame] = position;
            second.Service(at += 0.06);
            first.Sent.Clear();
            first.Service(at);
            if (!first.Sent.Any(bunch => bunch.Channel == pawn.Channel && (bunch.Payload[0] & 3) == 2))
            {
                continue;
            }

            seen = first.PawnFrame(seen, pawn.Channel, PendulumCodec.Others);
            shown.Add((first.LastFrameNumbers.Latest, seen, second.Member.Motion!));
        }

        // The thrower's own frames were held from the throw until the client allowed the next use.
        Assert.Equal(1, second.Channels.QuickItemHolds);
        Assert.Equal((thrown, released!.Value - 1), second.Channels.LastQuickItemHold);

        // The watching client got a frame for every tick, through the throw and the pull, at the thrower's position.
        Assert.All(shown.Zip(shown.Skip(1)), pair => Assert.Equal(pair.First.Tick + 1, pair.Second.Tick));
        Assert.Contains(shown, entry => entry.Tick < thrown);
        Assert.Contains(shown, entry => entry.Tick > released);
        IReadOnlyList<PendulumField> item = PendulumRegistry.Descriptors[QuickItemProtocol.ItemNodeDescriptor].Fields;
        IReadOnlyList<PendulumField> target = PendulumRegistry.Descriptors[QuickItemProtocol.MoveTargetDescriptor].Fields;
        IReadOnlyList<PendulumField> node = PendulumRegistry.Descriptors[QuickItemProtocol.PullNodeDescriptor].Fields;
        IReadOnlyList<PendulumField> graph = PendulumRegistry.Descriptors[PawnBlackboard.InitialState.Single(variable => variable.Slot == PawnBlackboard.GraphStateSlot).DescriptorId].Fields;
        IReadOnlyList<PendulumField> movement = PendulumRegistry.Descriptors[PawnBlackboard.InitialState.Single(variable => variable.Slot == PawnBlackboard.MovementStateSlot).DescriptorId].Fields;
        int pulled = 0;
        foreach ((uint tick, Dictionary<int, PendulumSlot> state, PlayerMotion motion) in shown)
        {
            Assert.True(Distance(motion.Motion.Position, ToWorld(state[PawnBlackboard.MovementStateSlot].Value)) < 0.2);
            SnapHookUse use = motion.Motion.Character!.Value.SnapHook;
            // Before the draw no item node is laid out.
            ulong bits = state.Values.SingleOrDefault(slot => slot.DescriptorId == QuickItemProtocol.ItemNodeDescriptor) is { } hook
                ? PendulumCodec.Integer(item[QuickItemProtocol.HookStateField], hook.Value)
                : 0;
            Assert.Equal(use.Hook.Thrown, (bits & (1UL << QuickItemProtocol.HookOutBit)) != 0);
            Assert.Equal(use.Hook is { Thrown: true, Attached: true }, (bits & (1UL << QuickItemProtocol.HookAttachedBit)) != 0);
            PendulumSlot? moveTarget = state.Values.SingleOrDefault(slot => slot.DescriptorId == QuickItemProtocol.MoveTargetDescriptor);
            Assert.Equal(use.Target is not null, moveTarget is not null);
            PendulumSlot? pull = state.Values.SingleOrDefault(slot => slot.DescriptorId == QuickItemProtocol.PullNodeDescriptor);
            Assert.Equal(use.Pull is not null, pull is not null);
            bool pulling = (PendulumCodec.Integer(graph[PawnBlackboard.GraphTagsField], state[PawnBlackboard.GraphStateSlot].Value) & SnapHookParameters.PullingFlags)
                == SnapHookParameters.PullingFlags;
            Assert.Equal(use.Pull is not null, pulling);
            if (moveTarget is not null)
            {
                Assert.Equal(use.Target!.Value.IsActive, PendulumCodec.Integer(target[QuickItemProtocol.MoveTargetCreatedField], moveTarget.Value) != PawnSlideState.TimeBias - 1);
            }

            if (pull is null)
            {
                continue;
            }

            // Pulled: the client's reported position, the pull node pointing at the move target.
            pulled++;
            Assert.Equal((ulong)moveTarget!.Slot, PendulumCodec.Integer(node[QuickItemProtocol.PullNodeMoveTargetField], pull.Value));
            // Reported positions are quantized to about 0.1 per axis; the body stands on the floor.
            Assert.True(Math.Min(Distance(motion.Motion.Position, reports[tick]), Distance(motion.Motion.Position, reports[tick + 1])) < 0.5);
        }

        Assert.InRange(pulled, 5, 60);
        Assert.Contains(shown, entry => entry.Motion.Motion.Character!.Value.SnapHook.Hook is { Thrown: true, Attached: false });

        // After the pull its node's slot stays laid out without a variable; the move target keeps its removal time.
        Dictionary<int, PendulumSlot> after = shown[^1].State;
        int pullSlot = shown.Last(entry => entry.State.Values.Any(slot => slot.DescriptorId == QuickItemProtocol.PullNodeDescriptor)).State.Values
            .Single(slot => slot.DescriptorId == QuickItemProtocol.PullNodeDescriptor).Slot;
        Assert.Null(after[pullSlot].DescriptorId);
        PendulumSlot kept = Assert.Single(after.Values, slot => slot.DescriptorId == QuickItemProtocol.MoveTargetDescriptor);
        Assert.NotEqual(PawnSlideState.TimeBias - 1, PendulumCodec.Integer(target[QuickItemProtocol.MoveTargetRemovedField], kept.Value));
        Assert.Equal(0, second.Channels.InputDecodeFailures);
        return;

        Vector3D ToWorld(byte[] value) => new(Coordinate(value, PawnBlackboard.PositionXField), Coordinate(value, PawnBlackboard.PositionYField),
            Coordinate(value, PawnBlackboard.PositionZField));

        double Coordinate(byte[] value, int field) =>
            (PendulumCodec.Integer(movement[field], value) * 2 * PawnBlackboard.PositionRange / (((PendulumScalarField)movement[field]).Maximum - 1.0)) - PawnBlackboard.PositionRange;
    }

    [Fact]
    public void Aim_pitch_is_compressed_like_the_pawn_replicates_it()
    {
        Assert.Equal(0, WorldState.CompressedPitch(0));
        Assert.Equal(8192, WorldState.CompressedPitch(Math.PI / 4));
        Assert.Equal(65536 - 8192, WorldState.CompressedPitch(-Math.PI / 4));
    }

    private static double Distance(Vector3D a, Vector3D b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));

    // The actor a dynamic actor's open spawns, or 0 for other bunches.
    private static ulong OpenedActor(ChannelBunch bunch)
    {
        if (!bunch.Opened || !bunch.MustBeMapped)
        {
            return 0;
        }

        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        MustMapGuids.Read(reader);
        return NetGuid.Read(reader);
    }

    // The content blocks after a character open's spawn (location written, facing if any, no scale or velocity).
    private static List<ActorContentBlock> PawnContent(ChannelBunch bunch)
    {
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        MustMapGuids.Read(reader);
        NetGuid.Read(reader);
        NetGuid.Read(reader);
        NetGuid.Read(reader);
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        for (int axis = 0; axis < 3; axis++)
        {
            reader.ReadBits(64);
        }

        int axes = reader.ReadBit() ? 3 : 0;
        for (int axis = 0; axis < axes; axis++)
        {
            if (reader.ReadBit())
            {
                reader.ReadBits(16);
            }
        }

        reader.ReadBits(2);
        List<ActorContentBlock> blocks = [];
        while (reader.Remaining > 0)
        {
            blocks.Add(ActorContentBlock.Read(reader));
        }

        return blocks;
    }

    private sealed class ContentComparer : IEqualityComparer<ActorContentBlock>
    {
        public bool Equals(ActorContentBlock? x, ActorContentBlock? y) =>
            x is not null && y is not null && x.HasRepLayout == y.HasRepLayout && x.PayloadBits == y.PayloadBits && x.Payload.AsSpan().SequenceEqual(y.Payload);

        public int GetHashCode(ActorContentBlock obj) => obj.PayloadBits.GetHashCode();
    }

    private static void Surrender(ScriptedPlayer session)
    {
        session.Call(FunctionNames.ServerSurrender, []);
        session.Flush("surrender");
        session.Channels.Closed();
    }
}
