using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Character state after an input frame.</summary>
/// <param name="Frame">Input frame number.</param>
/// <param name="Position">Reported world position.</param>
/// <param name="Flashlight">Whether the flashlight is enabled.</param>
public readonly record struct CharacterState(uint Frame, Vector3D Position, bool Flashlight)
{
    public Vector3D Velocity { get; init; }
    public double Yaw { get; init; }
    public double SimulationSeconds { get; init; }
    public double? MovementStartedAt { get; init; }
    public double? SprintStartedAt { get; init; }
    public CharacterStance Stance { get; init; }
    public LocoMode Locomotion { get; init; }
    public bool Shuffling { get; init; }
    public double? SlideStartedAt { get; init; }
    public SlideState? Slide { get; init; }
    public int SlideExitTicks { get; init; }
}

/// <summary>Per-character input simulation for reported position, flashlight and level-ground motion.</summary>
public sealed class CharacterSimulation
{
    /// <summary>Creates a character at its initial position and flashlight state.</summary>
    /// <param name="position">Initial position.</param>
    /// <param name="flashlight">Initial flashlight state.</param>
    public CharacterSimulation(Vector3D position, bool flashlight = false)
    {
        State = new CharacterState(0, position, flashlight);
    }

    /// <summary>State after the most recently applied input.</summary>
    public CharacterState State { get; private set; }

    /// <summary>An independent simulation starting at the current state.</summary>
    /// <returns>The copy.</returns>
    public CharacterSimulation Copy() => (CharacterSimulation)MemberwiseClone();

    /// <summary>Applies one input; the caller supplies frames in simulation order, once each.</summary>
    /// <param name="input">Character button claims.</param>
    /// <param name="position">Reported position, or null to retain the current position.</param>
    /// <param name="electrified">Whether the character is electrified, which disables the flashlight.</param>
    /// <returns>The resulting character state.</returns>
    public CharacterState Step(CharacterInput input, Vector3D? position = null, bool electrified = false)
    {
        bool flashlight = !electrified && (input.FlashlightPressed ? !State.Flashlight : State.Flashlight);
        State = State with { Frame = input.Frame, Position = position ?? State.Position, Flashlight = flashlight };
        return State;
    }

    /// <summary>Advances level-ground motion with the supplied posture's movement parameters.</summary>
    /// <param name="input">Character button claims.</param>
    /// <param name="movement">Movement axes and buttons from the same frame.</param>
    /// <param name="yaw">Control heading in radians.</param>
    /// <param name="seconds">Simulation step duration in seconds.</param>
    /// <param name="parameters">Standing movement parameters for the current equipment.</param>
    /// <param name="steerAboveMaximum">Whether input steering is enabled above maximum speed.</param>
    /// <param name="fixBrakingOvershoot">Whether braking overshoot is corrected.</param>
    /// <param name="position">Reported position.</param>
    /// <param name="electrified">Whether electrification disables the flashlight.</param>
    /// <param name="sprint">Sprint parameters when sprinting is allowed by the character's abilities.</param>
    /// <param name="slide">Slide parameters for the ground surface when sliding is allowed; requires sprint.</param>
    /// <param name="crouched">Crouch movement parameters; defaults to <see cref="GroundMovementParameters.Crouched"/>.</param>
    /// <returns>The next character state.</returns>
    public CharacterState StepOnGround(CharacterInput input, MovementInput movement, double yaw, double seconds,
        GroundMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot,
        Vector3D? position = null, bool electrified = false, SprintMovementParameters? sprint = null,
        SlideMovementParameters? slide = null, GroundMovementParameters? crouched = null)
    {
        if (input.Frame != movement.Frame)
        {
            throw new ArgumentException("Movement and character claims must belong to the same frame", nameof(movement));
        }

        double strength = Math.Min(1, Math.Sqrt((movement.Forward * movement.Forward) + (movement.Right * movement.Right)) / PendulumInput.MovementAxisMaximum);
        double time = State.SimulationSeconds + seconds;
        double? started = strength == 0 ? null : State.MovementStartedAt ?? time;
        double current = Math.Sqrt((State.Velocity.X * State.Velocity.X) + (State.Velocity.Y * State.Velocity.Y));
        double? slideStarted = State.SlideStartedAt;
        if (slideStarted is null && slide is not null && sprint is not null && SlideMovement.CanStart(movement, current))
        {
            slideStarted = time;
        }

        // The press tick and the first slide tick still move with the sprint they leave.
        bool leavingSprint = slideStarted is not null && State.Slide is not { Ticks: > 0 };
        bool crouching = movement.Buttons.HasFlag(MovementButtons.Crouch) && !leavingSprint;
        if (crouching)
        {
            parameters = crouched ?? GroundMovementParameters.Crouched;
        }

        bool shuffling = crouching && started is { } start && (input.WalkRequested
            || strength < GroundMovementParameters.WalkInputStrengthThreshold || time - start < GroundMovementParameters.CrouchShuffleSeconds);
        GroundMovementParameters actual = shuffling
            ? parameters with { MaximumSpeed = parameters.MaximumSpeed * GroundMovementParameters.CrouchShuffleSpeedFactor }
            : parameters;
        bool sprinting = sprint is not null && (leavingSprint
            || (movement.Buttons.HasFlag(MovementButtons.Sprint) && strength > 0 && !crouching && !input.WalkRequested));
        double? sprintStarted = State.SprintStartedAt;
        if (sprinting && sprint is { } sprintParameters)
        {
            if (State.Locomotion != LocoMode.Sprinting || sprintStarted is null)
            {
                sprintStarted = time;
            }

            actual = actual with { MaximumSpeed = SprintMovement.MaximumSpeed(parameters.MaximumSpeed, sprintParameters, time - sprintStarted.Value) };
        }

        double expectedStrength = Math.Abs(1 - strength) < GroundMovementParameters.InputStrengthSnapTolerance ? 1 : strength;
        double expected = actual.MaximumSpeed * expectedStrength;
        LocoMode locomotion = Locomotion(expected, current, sprinting, shuffling);
        Vector3D velocity = FlatGroundMovement.StepVelocity(State.Velocity, movement, yaw, seconds, actual, steerAboveMaximum, fixBrakingOvershoot);
        SlideState? slideState = null;
        int exitTicks = 0;
        CharacterStance stance = crouching ? CharacterStance.Crouch : CharacterStance.Stand;
        if (slideStarted is { } slideStart && slide is { } slideParameters && time > slideStart)
        {
            SlideStep step = FlatSlideMovement.Step(State.Slide ?? default, State.Velocity, movement, yaw, time - slideStart, (float)seconds,
                slideParameters, steerAboveMaximum, fixBrakingOvershoot);
            velocity = step.State.Ticks == 1 ? velocity : step.Velocity;
            slideState = step.Finished ? null : step.State;
            slideStarted = step.Finished ? null : slideStarted;
            exitTicks = step.Finished ? 1 : 0;
            stance = CharacterStance.Slide;
        }
        else
        {
            switch (slide)
            {
                case { } exitParameters when State.SlideExitTicks is > 0 and < SlideMovementParameters.ExitBlendTicks:
                {
                    // For a few ticks after a slide, the new movement only gradually replaces slide braking.
                    Vector3D braking = FlatGroundMovement.CalculateVelocity(State.Velocity, default, seconds,
                        SlideMovementParameters.Ground with { MaximumSpeed = exitParameters.MaximumSpeed }, steerAboveMaximum, fixBrakingOvershoot);
                    double weight = State.SlideExitTicks / (double)SlideMovementParameters.ExitBlendTicks;
                    velocity = new Vector3D(braking.X + ((velocity.X - braking.X) * weight), braking.Y + ((velocity.Y - braking.Y) * weight), 0);
                    exitTicks = State.SlideExitTicks + 1;
                    break;
                }
                case null:
                    slideStarted = null;
                    break;
            }
        }

        Step(input, position, electrified);
        State = State with
        {
            Velocity = velocity,
            Yaw = yaw,
            SimulationSeconds = time,
            MovementStartedAt = started,
            SprintStartedAt = sprintStarted,
            Stance = stance,
            Locomotion = locomotion,
            Shuffling = shuffling,
            SlideStartedAt = slideStarted,
            Slide = slideState,
            SlideExitTicks = exitTicks,
        };
        return State;
    }

    private LocoMode Locomotion(double expected, double current, bool sprinting, bool shuffling)
    {
        bool expectingMovement = Math.Abs(expected) > GroundMovementParameters.NearlyZeroSpeed;
        if (!expectingMovement && Math.Abs(current) <= GroundMovementParameters.NearlyZeroSpeed)
        {
            return LocoMode.Still;
        }

        if (sprinting && expected > GroundMovementParameters.Walking.MaximumSpeed)
        {
            return LocoMode.Sprinting;
        }

        if (shuffling || (expectingMovement && expected < GroundMovementParameters.WalkingModeSpeedThreshold))
        {
            return LocoMode.Walking;
        }

        return expectingMovement ? LocoMode.Running : State.Locomotion;
    }
}
