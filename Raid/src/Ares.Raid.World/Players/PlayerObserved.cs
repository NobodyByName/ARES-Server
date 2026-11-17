using Ares.Raid.Transport;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World.Players;

/// <summary>
/// The other players of the raid as one player sees them: each one's player state and character on their own pair of
/// channels, then the character's movement as it changes. A player who leaves the raid, or leaves it with an elevator,
/// is removed.
/// </summary>
internal sealed class PlayerObserved : IPlayerFamily
{
    // Other players' movement is sent at most this often.
    private const double ObservedFrameInterval = 0.05;
    private const int MaxObservedFramesInFlight = 32;

    private Dictionary<long, ObservedPlayer> _observed = [];

    private enum ObservedStage
    {
        Hidden,
        StateOpening,
        PawnOpening,
        Linking,
        Visible,
        Gone,
    }

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerObserved Copy()
    {
        PlayerObserved copy = (PlayerObserved)MemberwiseClone();
        copy._observed = new Dictionary<long, ObservedPlayer>(_observed);
        return copy;
    }

    /// <inheritdoc />
    public bool ChannelFailed(IPlayerChannelsContext player, int channel)
    {
        RaidWorld raid = player.Raid;
        if (raid.Channels.ObservedPlayer(channel) is not { } observed || observed >= raid.Players.Count)
        {
            return false;
        }

        _observed[raid.Players[observed].Id] = new ObservedPlayer(ObservedStage.Gone);
        return true;
    }

    /// <inheritdoc />
    public bool TryHandle(IPlayerChannelsContext player, InteractionRequest request, bool repeated, double now) => false;

    /// <inheritdoc />
    public bool Usable(IPlayerChannelsContext player, ulong target) => false;

    // Shows the client the other players of the raid once their characters move: opens each one's player state
    // as a simulated actor, then its character at its latest movement, links them and gives the character its
    // appearance; then sends that character's movement as it changes, unreliably, each frame coded
    // against the newest frame the client confirmed. A player who leaves the raid, or leaves it with an elevator, is
    // removed.
    // Everyone is shown everywhere; far-away players are not filtered out yet.
    /// <inheritdoc />
    public void Service(IPlayerChannelsContext player, ReliableSender sender, double now)
    {
        if (!player.MapActorsAllowed(now))
        {
            return;
        }

        RaidWorld raid = player.Raid;
        IReadOnlyList<RaidPlayer> players = raid.Players;
        for (int index = 0; index < players.Count; index++)
        {
            RaidPlayer member = players[index];
            ObservedPlayer observed = _observed.GetValueOrDefault(member.Id, new ObservedPlayer(ObservedStage.Hidden));
            if (member.Id == player.Member.Id || observed.Stage == ObservedStage.Gone || observed.Keys.Any(sender.Pending)
                || raid.State.FindPlayer(member.Id) is not { } other)
            {
                continue;
            }

            int stateChannel = raid.Channels.ObservedState(index);
            int pawnChannel = stateChannel + 1;
            if (member.Left || member.Departed)
            {
                if (observed.Stage >= ObservedStage.PawnOpening)
                {
                    sender.Queue(Bunches.Close(pawnChannel, Bunches.Destroyed));
                }

                if (observed.Stage >= ObservedStage.StateOpening)
                {
                    sender.Queue(Bunches.Close(stateChannel, Bunches.Destroyed));
                }

                _observed[member.Id] = new ObservedPlayer(ObservedStage.Gone);
                continue;
            }

            if (member.Motion is not { } motion)
            {
                continue;
            }

            _observed[member.Id] = observed.Stage switch
            {
                ObservedStage.Hidden => OpenObservedState(raid, sender, other, stateChannel),
                ObservedStage.StateOpening => OpenObservedPawn(raid, sender, other, pawnChannel, motion, now),
                ObservedStage.PawnOpening => LinkObservedPawn(player, sender, other, stateChannel, observed),
                ObservedStage.Linking => observed with
                {
                    Stage = ObservedStage.Visible,
                    Keys = [sender.Queue(Bunches.Actor(pawnChannel, raid.State.AppearanceContent(other).Write))],
                },
                _ when motion.Tick > observed.SentTick && now - observed.SentAt >= ObservedFrameInterval =>
                    SendObservedFrame(raid, sender, pawnChannel, observed, motion, now),
                _ => observed,
            };
        }
    }

    private static ObservedPlayer OpenObservedState(RaidWorld raid, ReliableSender sender, WorldPlayer other, int channel)
    {
        ActorSpawn spawn = new(other.PlayerState, raid.Imports.Archetypes[raid.State.Actor(other.PlayerState).ClassPath], 0, default);
        ActorInitialization open = new(spawn, null, [raid.State.ObservedPlayerStateProperties(other)]);
        return new ObservedPlayer(ObservedStage.StateOpening) { Keys = [sender.Queue(open.ToBunch(channel, sender.NextSequence(channel), []))] };
    }

    // The character opens as a simulated proxy with its movement's first (reset) frame.
    // Other players' frames carry only the fields the game sends to other clients (PendulumCodec.Others).
    // Frames are numbered by the character's simulation tick (about 30 per second).
    private static ObservedPlayer OpenObservedPawn(RaidWorld raid, ReliableSender sender, WorldPlayer other, int channel, PlayerMotion motion, double now)
    {
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(motion.Tick, motion.Motion, 0, weapons: motion.Weapons, timeOffset: motion.TimeOffset);
        Rotator facing = new(0, (motion.Motion.Yaw ?? 0) * 180 / Math.PI, 0);
        ActorSpawn spawn = new(other.Pawn, raid.Imports.Archetypes[raid.State.Actor(other.Pawn).ClassPath], 0, motion.Motion.Position, facing);
        ushort pitch = WorldState.CompressedPitch(motion.Pitch);
        ActorInitialization open = new(spawn, null,
            [WorldState.Merge(raid.State.ObservedPawnProperties(other, pitch), raid.State.BlackboardFrame(0, motion.Tick, state, new Dictionary<int, PendulumSlot>(), PendulumCodec.Others))]);
        return new ObservedPlayer(ObservedStage.PawnOpening, motion.Tick, state, now, pitch, motion.Tick)
        {
            Keys = [sender.Queue(open.ToBunch(channel, sender.NextSequence(channel), []))],
        };
    }

    private static ObservedPlayer LinkObservedPawn(IPlayerChannelsContext player, ReliableSender sender, WorldPlayer other, int stateChannel, ObservedPlayer observed)
    {
        WorldState state = player.Raid.State;
        player.MapObjects([other.PlayerState, other.Pawn]);
        (ObjectExports exports, ulong appearance) = state.AppearanceExports(other);
        return observed with
        {
            Stage = ObservedStage.Linking,
            Keys =
            [
                sender.Queue(Bunches.Actor(stateChannel, state.ObservedPawnLinks(other))),
                sender.Queue(Bunches.Exports(exports, [appearance])),
            ],
        };
    }

    private static ObservedPlayer SendObservedFrame(RaidWorld raid, ReliableSender sender, int channel, ObservedPlayer observed, PlayerMotion motion, double now)
    {
        observed = Confirmed(sender, observed);
        // A reset clears the receiver's history. Wait for its outcome before sending another frame.
        if (observed.State is null && observed.InFlight.Length != 0)
        {
            return observed;
        }

        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(motion.Tick, motion.Motion, 0, weapons: motion.Weapons, timeOffset: motion.TimeOffset);
        // The client may already have overwritten a baseline this far back; a reset frame starts from the defaults.
        bool reset = observed.State is null || motion.Tick - observed.Tick >= PendulumFrames.History;
        if (!reset && WithEndedSlots(state, observed.State!) is { } kept)
        {
            state = kept;
        }
        else
        {
            reset = true;
        }

        ActorContentBlock frame = reset
            ? raid.State.BlackboardFrame(0, motion.Tick, state, new Dictionary<int, PendulumSlot>(), PendulumCodec.Others)
            : raid.State.BlackboardFrame(observed.Tick, motion.Tick, state, observed.State!, PendulumCodec.Others);
        // Aim pitch rides with the frame in one block when it changed.
        ushort pitch = WorldState.CompressedPitch(motion.Pitch);
        if (pitch != observed.Pitch)
        {
            frame = WorldState.Merge(raid.State.ObservedPitch(pitch), frame);
        }

        ObservedFrame sent = new(sender.QueueUnreliable(Bunches.Actor(channel, frame)), motion.Tick, state, pitch);
        return observed with
        {
            Stage = ObservedStage.Visible,
            State = reset ? null : observed.State,
            SentAt = now,
            SentTick = motion.Tick,
            InFlight = reset ? [sent] : [.. observed.InFlight.TakeLast(MaxObservedFramesInFlight - 1), sent],
            Keys = [],
        };
    }

    // A frame cannot take a slot out of the client's layout: the client keeps what its baseline has in a slot the frame
    // does not name. A slot whose variable ended (the Snap Hook's pull node) stays laid out without a variable, as the
    // game keeps it. Null when such a slot shares pages with the frame's own slots, which only a reset frame can clear.
    private static IReadOnlyDictionary<int, PendulumSlot>? WithEndedSlots(IReadOnlyDictionary<int, PendulumSlot> state,
        IReadOnlyDictionary<int, PendulumSlot> baseline)
    {
        PendulumSlot[] ended = [.. baseline.Values.Where(slot => !state.ContainsKey(slot.Slot))];
        if (ended.Length == 0)
        {
            return state;
        }

        HashSet<int> pages = [.. state.Values.SelectMany(slot => Enumerable.Range(slot.Position, slot.Size))];
        if (ended.Any(slot => Enumerable.Range(slot.Position, slot.Size).Any(pages.Contains)))
        {
            return null;
        }

        Dictionary<int, PendulumSlot> kept = new(state);
        foreach (PendulumSlot slot in ended)
        {
            kept[slot.Slot] = slot with { DescriptorId = null, Value = [] };
        }

        return kept;
    }

    // The observed player with its baseline moved to the newest frame the client confirmed; lost or forgotten
    // frames are dropped.
    private static ObservedPlayer Confirmed(ReliableSender sender, ObservedPlayer observed)
    {
        ObservedPlayer result = observed;
        List<ObservedFrame> waiting = [];
        foreach (ObservedFrame frame in observed.InFlight)
        {
            if (sender.Arrived(frame.Key) is true && frame.Tick > result.Tick)
            {
                result = result with { Tick = frame.Tick, State = frame.State, Pitch = frame.Pitch };
            }
            else if (sender.Pending(frame.Key))
            {
                waiting.Add(frame);
            }
        }

        return result with { InFlight = [.. waiting.Where(frame => frame.Tick > result.Tick)] };
    }

    // What this client has of another player: how far its actors' delivery got, the reliable bunches it waits
    // for, the newest movement frame it confirmed (tick, state, aim pitch), when and which tick was last sent, and
    // the unreliable frames whose delivery is not known yet.
    private readonly record struct ObservedPlayer(ObservedStage Stage, uint Tick = 0,
        IReadOnlyDictionary<int, PendulumSlot>? State = null, double SentAt = 0, ushort Pitch = 0, uint SentTick = 0)
    {
        public ReliableKey[] Keys { get; init; } = [];

        public ObservedFrame[] InFlight { get; init; } = [];
    }

    // An unreliable movement frame sent to this client.
    private readonly record struct ObservedFrame(ReliableKey Key, uint Tick, IReadOnlyDictionary<int, PendulumSlot> State, ushort Pitch);
}
