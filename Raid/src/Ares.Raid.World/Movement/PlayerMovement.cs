using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// A player's character simulation, run from the input frames the client sent, once each and in order.
/// It starts at the frame of the pawn's reset frame. A frame the client never delivered repeats the
/// previous input with its buttons held, so it starts no new press.
/// </summary>
public sealed class PlayerMovement
{
    private const uint KeptStates = 64;

    // The client's movement options: steering above the maximum speed, no braking-overshoot correction.
    private const bool SteerAboveMaximum = true;
    private const bool FixBrakingOvershoot = false;

    // Buttons whose press is the button down without its previous-frame copy.
    private static readonly (MovementButtons Button, MovementButtons Previous)[] s_pressPairs =
    [
        (MovementButtons.Jump, MovementButtons.PreviousJump),
        (MovementButtons.Crouch, MovementButtons.PreviousCrouch),
        (MovementButtons.DodgeRoll, MovementButtons.PreviousDodgeRoll),
        (MovementButtons.UsingGamepad, MovementButtons.PreviousUsingGamepad),
    ];

    private CharacterSimulation _simulation;
    private Dictionary<uint, CharacterState> _states = [];
    private MovementInput _movement;
    private CharacterInput _claims;
    private double _yaw;

    /// <summary>Starts the simulation at rest.</summary>
    /// <param name="frame">Input frame of the reset frame; simulation starts with the next one.</param>
    /// <param name="position">Spawn position.</param>
    /// <param name="yaw">Spawn facing in radians.</param>
    public PlayerMovement(uint frame, Vector3D position, double yaw)
    {
        _simulation = new CharacterSimulation(position);
        Frame = frame;
        _yaw = yaw;
    }

    /// <summary>Latest simulated frame.</summary>
    public uint Frame { get; private set; }

    /// <summary>Frames simulated without the client's input.</summary>
    public int MissingFrames { get; private set; }

    /// <summary>State after the latest simulated frame.</summary>
    public CharacterState Latest => _simulation.State;

    /// <summary>An independent copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerMovement Copy()
    {
        PlayerMovement copy = (PlayerMovement)MemberwiseClone();
        copy._simulation = _simulation.Copy();
        copy._states = new Dictionary<uint, CharacterState>(_states);
        return copy;
    }

    /// <summary>Simulates the next frame.</summary>
    /// <param name="input">The client's input history.</param>
    /// <param name="armed">Whether a weapon is in the character's hands.</param>
    /// <returns>The frame's state.</returns>
    public CharacterState Advance(InputHistory input, bool armed)
    {
        ArgumentNullException.ThrowIfNull(input);
        uint frame = Frame + 1;
        if (input.Movement(frame) is { } movement && input.Claims(frame) is { } claims)
        {
            (_movement, _claims) = (movement, claims);
            _yaw = input.Yaw(frame) ?? _yaw;
        }
        else
        {
            (_movement, _claims) = (Held(_movement) with { Frame = frame }, Held(_claims) with { Frame = frame });
            MissingFrames++;
        }

        CharacterState state = _simulation.StepOnGround(_claims, _movement, _yaw, PawnBlackboard.TickSeconds,
            armed ? GroundMovementParameters.Armed : GroundMovementParameters.Unarmed, SteerAboveMaximum, FixBrakingOvershoot,
            input.Position(frame), sprint: armed ? SprintMovementParameters.Armed : SprintMovementParameters.Unarmed,
            slide: SlideMovementParameters.Default);
        Frame = frame;
        _states[frame] = state;
        _states.Remove(frame - KeptStates);
        return state;
    }

    /// <summary>The state after a simulated frame.</summary>
    /// <param name="frame">Input frame.</param>
    /// <returns>The state, or null when the frame was not simulated or is too old.</returns>
    public CharacterState? State(uint frame) => _states.TryGetValue(frame, out CharacterState state) ? state : null;

    private static MovementInput Held(MovementInput input)
    {
        MovementButtons buttons = input.Buttons;
        foreach ((MovementButtons button, MovementButtons previous) in s_pressPairs)
        {
            buttons = buttons.HasFlag(button) ? buttons | previous : buttons & ~previous;
        }

        return input with { Buttons = buttons };
    }

    private static CharacterInput Held(CharacterInput input) =>
        (input.Flags & (1UL << PendulumInput.FlashlightDownBit)) != 0
            ? input with { Flags = input.Flags | (1UL << PendulumInput.FlashlightPreviousDownBit) }
            : input;
}
