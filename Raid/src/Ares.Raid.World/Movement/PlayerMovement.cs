using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Movement;

/// <summary>
/// A player's character simulation, run from the input frames the client sent, in frame order. It starts at the
/// frame of the pawn's reset frame. A frame the client has not delivered repeats the previous input with its buttons
/// held, so it starts no new press. When such a frame, or a reported position a tick depends on, arrives later, the
/// ticks from the earliest affected one are simulated again, so the state does not depend on how the frames were
/// delivered. With the level's collision the simulation moves the character's body itself and follows the client:
/// when the simulated position ends a tick further than the tolerance from the position the client reports for it,
/// the body continues from the reported one, and within the tolerance it closes a share of the horizontal distance. Without collision the position is the reported one and the reported
/// positions tell whether the character is off the ground (<see cref="AirMotion"/>).
/// </summary>
public sealed class PlayerMovement
{
    // Ticks kept, and revisable by late input, for as many frames as the client keeps the server's frames. A frame
    // that arrives after its tick has left this window is ignored.
    private const uint KeptTicks = PendulumFrames.History;

    // The client's movement options: steering above the maximum speed, and braking that would end below the maximum
    // speed while the input still pushes forward stops at the maximum speed.
    private const bool SteerAboveMaximum = true;
    private const bool FixBrakingOvershoot = true;

    // The client's reload-sprint option: whether a running reload blocks sprinting (unless the sprint started after the
    // reload press or is pressed again).
    private const bool ReloadBlocksSprint = false;

    // Buttons whose press is the button down without its previous-frame copy.
    private static readonly (MovementButtons Button, MovementButtons Previous)[] s_pressPairs =
    [
        (MovementButtons.Jump, MovementButtons.PreviousJump),
        (MovementButtons.Crouch, MovementButtons.PreviousCrouch),
        (MovementButtons.DodgeRoll, MovementButtons.PreviousDodgeRoll),
        (MovementButtons.UsingGamepad, MovementButtons.PreviousUsingGamepad),
    ];

    /// <summary>Distance between the simulated and the reported position above which the body follows the report.</summary>
    public const double PositionTolerance = 3;

    // One step of the reported positions' grid.
    private static readonly double s_reportStep = 2 * PawnBlackboard.PositionRange
        / (((PendulumScalarField)PendulumRegistry.Descriptors[PendulumInput.SlotDescriptors[PendulumInput.PositionSlot]].Fields[PendulumInput.PositionXField]).Maximum - 1.0);

    // Share of the remaining horizontal distance to the reported position the body closes each tick while it is within
    // the tolerance, so small differences in speed do not add up.
    private const double FollowShare = 0.1;

    // Ticks a phase followed from the reports (a vault, a ladder animation) may last at most; a client locked for longer
    // is busy with something else.
    private const int MostFollowedTicks = 60;

    // Ticks into a climb at which the character is on the ladder's line, past getting onto it.
    private static readonly int s_attachedLadderTicks = (int)Math.Ceiling(LadderParameters.AlignmentSeconds / PawnBlackboard.TickSeconds) + 1;

    private uint _start;
    private Dictionary<uint, LadderStart> _ladderStarts = [];
    private uint? _revisedFrom;
    private readonly CollisionWorld? _collision;
    private readonly double _tolerance;
    private CharacterSimulation _simulation;
    private Dictionary<uint, Tick> _ticks = [];
    private MovementInput _movement;
    private CharacterInput _claims;
    private AirMotion _air;
    private double _yaw;

    /// <summary>Starts the simulation at rest.</summary>
    /// <param name="frame">Input frame of the reset frame; simulation starts with the next one.</param>
    /// <param name="position">Spawn position.</param>
    /// <param name="yaw">Spawn facing in radians.</param>
    /// <param name="collision">The level's collision, or null to keep the reported positions.</param>
    /// <param name="tolerance">See <see cref="PositionTolerance"/>; infinite never follows the reports.</param>
    public PlayerMovement(uint frame, Vector3D position, double yaw, CollisionWorld? collision = null, double tolerance = PositionTolerance)
        : this(frame, new CharacterState(0, position, false), yaw, collision, tolerance)
    {
    }

    private PlayerMovement(uint frame, CharacterState state, double yaw, CollisionWorld? collision, double tolerance)
    {
        _collision = collision;
        _tolerance = tolerance;
        _simulation = CharacterSimulation.From(state, collision);
        (_start, Frame, _yaw) = (frame, frame, yaw);
        _ticks[frame] = new Tick(_simulation.Copy(), default, default, default, yaw, false, false, false, Inputs.None);
    }

    /// <summary>
    /// Starts the simulation again at rest at a position, as the character's frames put it there (a teleport): the next
    /// frame places the body where the client then reports it, without counting it as a re-seed. The counters keep
    /// their totals apart from the ticks after the frame, which run again.
    /// </summary>
    /// <param name="frame">Input frame the character is placed at; simulation goes on with the next one.</param>
    /// <param name="position">Where the character is placed.</param>
    /// <param name="yaw">Its facing in radians.</param>
    public void Restart(uint frame, Vector3D position, double yaw)
    {
        // Ticks after the restart frame run again from it; what they counted no longer holds.
        MissingFrames -= _ticks.Count(entry => entry.Key > frame && entry.Value.Guessed);
        Reseeds -= _ticks.Count(entry => entry.Key > frame && entry.Value.Reseeded);
        if (_ticks.TryGetValue(frame, out Tick kept))
        {
            (LastReseed, WorstReseed) = (kept.LastReseed, kept.WorstReseed);
        }

        _simulation = CharacterSimulation.From(new CharacterState(0, position, _simulation.State.Flashlight) { Yaw = yaw }, _collision);
        (_start, Frame, _yaw, _air, _revisedFrom) = (frame, frame, yaw, default, null);
        _ladderStarts.Clear();
        _ticks = new Dictionary<uint, Tick>
        {
            [frame] = new(_simulation.Copy(), default, default, default, yaw, false, false, false, Inputs.None)
            {
                LastReseed = LastReseed,
                WorstReseed = WorstReseed,
            },
        };
    }

    /// <summary>Starts the simulation in a given character state.</summary>
    /// <param name="frame">Input frame of the state; simulation starts with the next one.</param>
    /// <param name="state">The character's state at that frame.</param>
    /// <param name="yaw">Facing in radians.</param>
    /// <param name="collision">The level's collision, or null to keep the reported positions.</param>
    /// <param name="tolerance">See <see cref="PositionTolerance"/>; infinite never follows the reports.</param>
    /// <returns>The simulation.</returns>
    public static PlayerMovement From(uint frame, CharacterState state, double yaw, CollisionWorld? collision = null, double tolerance = PositionTolerance) =>
        new(frame, state, yaw, collision, tolerance);

    // Input frames a tick reads besides its own: the reported positions around it and the next frame's weapon controls.
    [Flags]
    private enum Inputs : byte
    {
        None = 0,
        Previous = 1 << 0,
        Own = 1 << 1,
        Next = 1 << 2,
    }

    /// <summary>Latest simulated frame.</summary>
    public uint Frame { get; private set; }

    /// <summary>Simulated frames whose input the client has not delivered.</summary>
    public int MissingFrames { get; private set; }

    /// <summary>Simulated frames after which the body continued from the reported position.</summary>
    public int Reseeds { get; private set; }

    /// <summary>The latest such frame: its number, how far the simulation was off and whether it had the body in the air.</summary>
    public (uint Frame, double Distance, bool Airborne)? LastReseed { get; private set; }

    /// <summary>Simulated minus reported position after the latest simulated frame, before any following.</summary>
    public Vector3D LastError { get; private set; }

    /// <summary>Largest distance the simulation was off when the body continued from a reported position.</summary>
    public double WorstReseed { get; private set; }

    /// <summary>State after the latest simulated frame.</summary>
    public CharacterState Latest => _simulation.State;

    /// <summary>An independent copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerMovement Copy()
    {
        PlayerMovement copy = (PlayerMovement)MemberwiseClone();
        copy._simulation = _simulation.Copy();
        copy._ticks = new Dictionary<uint, Tick>(_ticks);
        copy._ladderStarts = new Dictionary<uint, LadderStart>(_ladderStarts);
        return copy;
    }

    /// <summary>
    /// The client pressed a ladder's climb interaction with the input frame: from that tick the character climbs the
    /// ladder (<see cref="CharacterSimulation.StepOnLadder"/>) until it leaves it.
    /// </summary>
    /// <param name="frame">Input frame of the climb request.</param>
    /// <param name="ladder">Index of the ladder in <see cref="DamLadders.All"/>.</param>
    /// <param name="topEntry">Whether the press was on the ladder's top entry.</param>
    /// <param name="attached">Whether the character is already on the ladder's line, past getting onto it.</param>
    public void Climb(uint frame, int ladder, bool topEntry, bool attached = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ladder);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(ladder, DamLadders.All.Count);
        if (!_ladderStarts.TryAdd(frame, new LadderStart(ladder, topEntry, attached)) || frame > Frame || frame <= _start)
        {
            return;
        }

        // The tick has run already; it runs again on the next revision.
        _revisedFrom = Math.Min(_revisedFrom ?? frame, frame);
    }

    /// <summary>Simulates the next frame, after revising the ticks late input frames have reached.</summary>
    /// <param name="input">The client's input history.</param>
    /// <param name="armed">Whether a weapon is in the character's hands; a revision keeps each tick's value.</param>
    /// <param name="heldItem">
    /// The held item and its weapon state after the frame's weapon input, or null to leave the held item out; a revision
    /// keeps each tick's value. The frame's weapon controls are read from the input history.
    /// </param>
    /// <returns>The frame's state.</returns>
    public CharacterState Advance(InputHistory input, bool armed, HeldItemInput? heldItem = null)
    {
        Revise(input);
        return Simulate(input, armed, heldItem);
    }

    /// <summary>
    /// Simulates again, in order, the kept ticks that ran before one of the input frames they read had arrived,
    /// from the earliest such tick up to <see cref="Frame"/>, each with the weapon state it first ran with.
    /// </summary>
    /// <param name="input">The client's input history.</param>
    /// <returns>The earliest tick simulated again, or null when no late frame reached a kept tick.</returns>
    public uint? Revise(InputHistory input)
    {
        ArgumentNullException.ThrowIfNull(input);
        uint? earliest = _ticks
            .Where(entry => entry.Value.Missing != Inputs.None && _ticks.ContainsKey(entry.Key - 1) && Arrived(input, entry.Key, entry.Value.Missing))
            .Select(entry => (uint?)entry.Key)
            .Append(_revisedFrom is { } climbed && _ticks.ContainsKey(climbed - 1) ? climbed : null)
            .Min();
        _revisedFrom = null;
        if (earliest is not { } first)
        {
            return null;
        }

        uint last = Frame;
        Tick before = _ticks[first - 1];
        MissingFrames -= _ticks.Count(entry => entry.Key >= first && entry.Value.Guessed);
        Reseeds -= _ticks.Count(entry => entry.Key >= first && entry.Value.Reseeded);
        (LastReseed, WorstReseed) = (before.LastReseed, before.WorstReseed);
        (_simulation, _movement, _claims, _air, _yaw) = (before.Simulation.Copy(), before.Movement, before.Claims, before.Air, before.Yaw);
        Frame = first - 1;
        while (Frame < last)
        {
            Tick kept = _ticks[Frame + 1];
            Simulate(input, kept.Armed, kept.HeldItem);
        }

        return first;
    }

    /// <summary>The state after a simulated frame.</summary>
    /// <param name="frame">Input frame.</param>
    /// <returns>The state, or null when the frame was not simulated or is too old.</returns>
    public CharacterState? State(uint frame) => frame != _start && _ticks.TryGetValue(frame, out Tick tick) ? tick.Simulation.State : null;

    private static bool Arrived(InputHistory input, uint frame, Inputs missing) =>
        (missing.HasFlag(Inputs.Previous) && input.Received(frame - 1))
        || (missing.HasFlag(Inputs.Own) && input.Received(frame))
        || (missing.HasFlag(Inputs.Next) && input.Received(frame + 1));

    private static MovementInput Held(MovementInput input)
    {
        MovementButtons buttons = input.Buttons;
        foreach ((MovementButtons button, MovementButtons previous) in s_pressPairs)
        {
            buttons = buttons.HasFlag(button) ? buttons | previous : buttons & ~previous;
        }

        return input with { Buttons = buttons };
    }

    // The held item with the weapon controls of its frame; without a weapon input the controls stay released.
    private static HeldItemInput? HeldControls(HeldItemInput? heldItem, WeaponInput? weapon) =>
        heldItem is { } item
            ? item with
            {
                FireDown = weapon?.FireDown ?? false,
                FireBlocked = weapon?.FireBlocked ?? false,
                AllowPrimary = weapon?.AllowPrimary ?? false,
                ReloadDown = weapon?.ReloadDown ?? false,
                ReloadDownBefore = weapon?.ReloadDownBefore ?? false,
                ReloadBlocksSprint = ReloadBlocksSprint,
            }
            : null;

    private static CharacterInput Held(CharacterInput input) =>
        input with
        {
            Flags = (input.Flags & (1UL << PendulumInput.FlashlightDownBit)) != 0
                ? input.Flags | (1UL << PendulumInput.FlashlightPreviousDownBit)
                : input.Flags & ~(1UL << PendulumInput.FlashlightPreviousDownBit),
        };

    private CharacterState Simulate(InputHistory input, bool armed, HeldItemInput? heldItem)
    {
        ArgumentNullException.ThrowIfNull(input);
        uint frame = Frame + 1;
        bool guessed = false;
        if (input.Movement(frame) is { } movement && input.Claims(frame) is { } claims)
        {
            (_movement, _claims) = (movement, claims);
            _yaw = input.Yaw(frame) ?? _yaw;
        }
        else
        {
            (_movement, _claims) = (Held(_movement) with { Frame = frame }, Held(_claims) with { Frame = frame });
            guessed = true;
            MissingFrames++;
        }

        _air = _air.Next(_movement.JumpPressed, input.Weapon(frame + 1)?.FireBlocked ?? false, input.Position(frame - 1), input.Position(frame), input.Position(frame + 1));

        // A ladder climb starts with the client's press on the ladder's interaction; after its animated phases and a
        // followed vault the body follows the reports while the client is in the air or locked in the action.
        CharacterState previous = _simulation.State;
        bool locked = _air.Unsettled || (input.Weapon(frame + 1)?.FireBlocked ?? false);
        LadderState? start = _ladderStarts.TryGetValue(frame, out LadderStart begun)
            ? LadderMovement.Begin(LadderLine.From(DamLadders.All[begun.Ladder]), begun.TopEntry) with { Ticks = begun.Attached ? s_attachedLadderTicks : 0 }
            : null;
        bool followingLadder = start is null && previous.Ladder is null && previous.LadderFollowedTicks is > 0 and < MostFollowedTicks && locked;
        bool followingVault = start is null && previous.Ladder is null && !followingLadder && previous.VaultFollowedTicks is > 0 and < MostFollowedTicks
            && (VaultDetection.Requested(_claims) || (input.Weapon(frame + 1)?.FireBlocked ?? false));
        CharacterState state;
        if (start is not null || previous.Ladder is not null || (followingLadder && input.Position(frame + 1) is not null))
        {
            state = _simulation.StepOnLadder(_claims, _movement, _yaw, PawnBlackboard.TickSeconds, input.Position(frame + 1), input.Position(frame), start,
                _air.Unsettled);
        }
        else if (followingVault && input.Position(frame + 1) is { } vaulting)
        {
            state = _simulation.StepFollowingVault(_claims, PawnBlackboard.TickSeconds, vaulting, input.Position(frame), _yaw, _air.Unsettled);
        }
        else
        {
            state = _simulation.StepOnGround(_claims, _movement, _yaw, PawnBlackboard.TickSeconds,
                armed ? GroundMovementParameters.Armed : GroundMovementParameters.Unarmed, SteerAboveMaximum, FixBrakingOvershoot,
                input.Position(frame), sprint: armed ? SprintMovementParameters.Armed : SprintMovementParameters.Unarmed,
                slide: SlideMovementParameters.ForSurface(_simulation.State.Body?.Ground.Surface ?? default), unsettled: _air.Unsettled,
                vaults: VaultParameters.Definitions(armed, exhausted: false), pitch: input.Pitch(frame) ?? 0,
                heldItem: HeldControls(heldItem, input.Weapon(frame)));
        }

        bool reseeded = false;
        if (_collision is not null && state.Body is { } body && input.Position(frame + 1) is { } reported)
        {
            double distance = Math.Sqrt(Square(body.Position.X - reported.X) + Square(body.Position.Y - reported.Y) + Square(body.Position.Z - reported.Z));
            LastError = new Vector3D(body.Position.X - reported.X, body.Position.Y - reported.Y, body.Position.Z - reported.Z);
            bool first = frame == _start + 1;
            double vertical = input.Position(frame) is { } before ? (reported.Z - before.Z) / PawnBlackboard.TickSeconds : body.Velocity.Z;
            if (first && distance > s_reportStep)
            {
                // The client starts from the reset frame's position; within one step of its coarser reports the body keeps its own.
                BodyState placed = BodyMotion.Start(_collision, reported, body.Velocity with { Z = 0 });
                state = _simulation.Reseat(placed with { Jumping = placed.Airborne && (body.Jumping || placed.Jumping), JumpRequestedAt = body.JumpRequestedAt });
            }
            else if (start is { TopEntry: true } || (state.LadderFollowedTicks == 1 && previous.LadderFollowedTicks == 0))
            {
                // A ladder phase the game animates starts: it follows the reports, which counts once.
                reseeded = Count(frame, distance, body.Airborne);
            }
            else if (state.VaultMissed || (state.Vault is not null && distance > _tolerance))
            {
                // The client vaults where the simulation found no vault, or off the simulated path: the body follows its
                // reports for the rest of that vault, which counts once.
                state = _simulation.FollowVault(PawnBlackboard.TickSeconds, reported, input.Position(frame), _air.Unsettled);
                reseeded = previous.VaultFollowedTicks == 0 && Count(frame, distance, body.Airborne);
            }
            else if ((state.Ladder is not null && distance > _tolerance) || (state.Landing is not null && (state.LandingFollowed || distance > _tolerance)))
            {
                // A climb or a heavy landing whose path left the client's continues from the reports; that counts once.
                bool counted = state.Ladder is not null ? state.LadderDiverged : state.LandingFollowed;
                state = _simulation.FollowReport(BodyMotion.Start(_collision, reported, body.Velocity with { Z = vertical }) with { Jumping = false });
                reseeded = !counted && Count(frame, distance, body.Airborne);
            }
            else
            {
                switch (state)
                {
                    case { Vault: null, Ladder: null, LadderFollowedTicks: 0 } when distance > _tolerance:
                        // In the air the vertical speed the reports show; on the ground the body's own, which follows the ground.
                        // The horizontal speed stays the movement's own.
                        BodyState placed = BodyMotion.Start(_collision, reported, body.Velocity with { Z = vertical });
                        placed = placed.Airborne ? placed : placed with { Velocity = body.Velocity };
                        state = _simulation.Reseat(placed with { Jumping = placed.Airborne && (body.Jumping || placed.Jumping), JumpRequestedAt = body.JumpRequestedAt });
                        reseeded = Count(frame, distance, body.Airborne);
                        break;
                    case { Vault: null, Ladder: null } when double.IsFinite(_tolerance):
                        // Paths of their own (a vault, a climb) keep to them; ordinary movement closes a share of the distance.
                        state = _simulation.Reseat(body with
                        {
                            Position = body.Position with
                            {
                                X = body.Position.X + ((reported.X - body.Position.X) * FollowShare),
                                Y = body.Position.Y + ((reported.Y - body.Position.Y) * FollowShare),
                            },
                        });
                        break;
                }
            }
        }

        Frame = frame;
        Inputs missing = (input.Received(frame - 1) ? Inputs.None : Inputs.Previous)
            | (input.Received(frame) ? Inputs.None : Inputs.Own)
            | (input.Received(frame + 1) ? Inputs.None : Inputs.Next);
        _ticks[frame] = new Tick(_simulation.Copy(), _movement, _claims, _air, _yaw, armed, guessed, reseeded, missing)
        {
            HeldItem = heldItem,
            LastReseed = LastReseed,
            WorstReseed = WorstReseed,
        };
        _ticks.Remove(frame - KeptTicks);
        _ladderStarts.Remove(frame - KeptTicks);
        return state;
    }

    private static double Square(double value) => value * value;

    // Counts a tick after which the body continued from the reported position.
    private bool Count(uint frame, double distance, bool airborne)
    {
        Reseeds++;
        LastReseed = (frame, distance, airborne);
        WorstReseed = Math.Max(WorstReseed, distance);
        return true;
    }

    // A simulated tick: the simulation after it, the input and motion the next tick continues from, the weapon state
    // it ran with, whether its own input was guessed, whether its body continued from the reported position, which
    // input frames it read had not arrived, and the re-seed records as they stood after it.
    private readonly record struct Tick(CharacterSimulation Simulation, MovementInput Movement, CharacterInput Claims, AirMotion Air,
        double Yaw, bool Armed, bool Guessed, bool Reseeded, Inputs Missing)
    {
        public HeldItemInput? HeldItem { get; init; }

        public (uint Frame, double Distance, bool Airborne)? LastReseed { get; init; }

        public double WorstReseed { get; init; }
    }

    // A climb the client asked for: the ladder, whether from its top, and whether already on its line.
    private readonly record struct LadderStart(int Ladder, bool TopEntry, bool Attached);
}
