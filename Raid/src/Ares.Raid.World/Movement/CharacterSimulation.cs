using System.Numerics;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Movement;

/// <summary>Character state after an input frame.</summary>
/// <param name="Frame">Input frame number.</param>
/// <param name="Position">World position of the feet: simulated with <see cref="Body"/>, otherwise the reported one.</param>
/// <param name="Flashlight">Whether the flashlight is enabled.</param>
public readonly record struct CharacterState(uint Frame, Vector3D Position, bool Flashlight)
{
    public Vector3D Velocity { get; init; }
    public double Yaw { get; init; }
    public double SimulationSeconds { get; init; }
    public double? MovementStartedAt { get; init; }
    public double? SprintStartedAt { get; init; }
    /// <summary>Stance of the action layer: what the character does, such as a slide.</summary>
    public CharacterStance Stance { get; init; }

    /// <summary>Locomotion of the action layer.</summary>
    public LocoMode Locomotion { get; init; }

    /// <summary>Stance of the movement state: the posture under the action, crouched during a slide.</summary>
    public CharacterStance MovementStance { get; init; }

    /// <summary>Locomotion of the movement state: the ground movement's, kept unchanged while a slide runs.</summary>
    public LocoMode MovementLocomotion { get; init; }

    public bool Shuffling { get; init; }
    public double? SlideStartedAt { get; init; }
    public SlideState? Slide { get; init; }
    public int SlideExitTicks { get; init; }

    /// <summary>The dodge roll under way, from the tick that starts it to the tick after its last step.</summary>
    public DodgeRollState? Roll { get; init; }

    /// <summary>Whether this tick took the roll's last step; the next tick still moves with the roll's animation.</summary>
    public bool RollFinished { get; init; }

    /// <summary>Ticks of the roll's exit blend taken so far (<see cref="DodgeRollParameters.ExitBlendTicks"/>); 0 while it rolls.</summary>
    public int RollExitTicks { get; init; }

    /// <summary>Whether this tick took a roll step: the character is rolling.</summary>
    public bool Rolling => Roll is { Ticks: > 0 } && RollExitTicks == 0;

    /// <summary>Ticks of the recovery from a heavy landing taken so far, this one included; 0 when none runs.</summary>
    public int HeavyLandingTicks { get; init; }

    /// <summary>The heavy landing under way, from its first step to the end of its hand-over blend.</summary>
    public HeavyLandingState? Landing { get; init; }

    /// <summary>Whether this tick took the landing's last step; the hand-over blend follows.</summary>
    public bool LandingFinished { get; init; }

    /// <summary>Ticks of the landing's hand-over blend taken so far, and how many it has.</summary>
    public (int Taken, int Total) LandingBlend { get; init; }

    /// <summary>Whether the landing's position follows the client's reports because the simulated path left them.</summary>
    public bool LandingFollowed { get; init; }

    /// <summary>When a press that can turn a heavy landing into a recovery roll was last counted, in simulation seconds.</summary>
    public double? RecoveryPressedAt { get; init; }

    /// <summary>Whether the landing-recovery tag is set; heavy landings write it and it keeps its value in between.</summary>
    public bool LandingRecoveryTag { get; init; }

    /// <summary>Air ticks since the take-off of a jump; 0 on the ground or in a fall.</summary>
    public int JumpAirTicks { get; init; }

    /// <summary>Whether the character still crouches from a slide rather than by its crouch input.</summary>
    public bool CrouchLatched { get; init; }

    /// <summary>The ladder climb under way, while the simulation moves the character along the ladder's line.</summary>
    public LadderState? Ladder { get; init; }

    /// <summary>The ladder tags of this tick's ladder step.</summary>
    public ulong LadderFlags { get; init; }

    /// <summary>
    /// Ticks of a ladder phase played by an animation the simulation does not model (top entry, climbing off the top,
    /// jumping off), the position then the one the client reports; 0 when none runs.
    /// </summary>
    public int LadderFollowedTicks { get; init; }

    /// <summary>Whether the climb's position left the client's reports, after which the body continues from them.</summary>
    public bool LadderDiverged { get; init; }

    /// <summary>Ticks the ladder's crouch-input block still lasts after the climb.</summary>
    public int CrouchBlockTicks { get; init; }

    /// <summary>Whether the character is on a ladder or in one of its animated phases.</summary>
    public bool OnLadder => Ladder is not null || LadderFollowedTicks > 0;

    /// <summary>The vault this tick stepped, from the tick that found it to its last step.</summary>
    public VaultState? Vault { get; init; }

    /// <summary>Whether this tick took the vault's last step; the next tick returns to ground or air movement.</summary>
    public bool VaultFinished { get; init; }

    /// <summary>Whether the client asked for a vault this tick and the search found none to play.</summary>
    public bool VaultMissed { get; init; }

    /// <summary>
    /// Ticks the character has vaulted along a path Raid does not know, its position the one its client reports; 0 when
    /// it does not.
    /// </summary>
    public int VaultFollowedTicks { get; init; }

    /// <summary>Whether the character vaults this tick, along a simulated path or a followed one.</summary>
    public bool Vaulting => Vault is not null || VaultFollowedTicks > 0;

    /// <summary>The graph-state flags the simulation decides (<see cref="GraphStateFlags.Simulated"/>).</summary>
    public ulong GraphFlags
    {
        get
        {
            ulong flags = Flashlight ? GraphStateTags.AbilityFlashlightActive : 0;
            flags |= Body is { Jumping: true, Touching: false } ? GraphStateTags.AnimationAbilityJumpStarted : 0;
            flags |= Body is { JumpRequestedAt: not null } ? GraphStateTags.AbilityJumpPending : 0;
            flags |= JumpAirTicks is > 0 and <= AirMovementParameters.JumpTagTicks ? GraphStateTags.AbilityJump : 0;
            flags |= MovementLocomotion == LocoMode.Sprinting && !OnLadder ? GraphStateTags.AbilitySprintActive : 0;
            flags |= MovementStance == CharacterStance.Crouch && Stance == CharacterStance.Crouch && !CrouchLatched ? GraphStateTags.AbilityCrouch : 0;
            if (Stance == CharacterStance.Slide && Slide is { Ticks: > 0 and <= GraphStateFlags.SlideStaminaRechargeBlockedTicks } sliding)
            {
                flags |= GraphStateTags.AttributeStaminaBlockRechargePendulumMove
                    | (sliding.Ticks >= GraphStateFlags.SlideSprintInputBlockedFirstTick ? GraphStateTags.AbilityBlockInputSprintPendulumMove : 0);
            }

            flags |= Rolling ? DodgeRollParameters.RollingFlags : 0;
            flags |= HeavyLandingTicks > 0 ? GraphStateFlags.HeavyLandingRecovery : 0;
            flags |= Vaulting ? VaultParameters.VaultingFlags : 0;
            flags |= LandingRecoveryTag ? GraphStateTags.AbilityLandingRecovery : 0;
            flags |= CrouchBlockTicks > 0 ? GraphStateTags.AbilityBlockInputCrouchPendulumMove : 0;
            flags |= HeldItem is { } held
                ? (held.Zoom ? GraphStateTags.AbilityZooming : 0) | (held.PrimaryUseTag ? GraphStateTags.AbilityFirePrimaryPendulumWep : 0)
                    | (held.ReloadBlocked ? GraphStateTags.AbilityBlockReloadPendulumMove : 0)
                : 0;
            return flags | (Ladder is not null ? LadderFlags : LadderFollowedTicks > 0 ? LadderParameters.ClimbingFlags : 0);
        }
    }

    /// <summary>The graph-state tags from 64 the simulation decides (<see cref="GraphStateFlags.SimulatedUpper"/>).</summary>
    public ulong UpperGraphFlags => (GroundNode.SprintBoosted ? GraphStateUpperTags.UiSkillStateActiveSprintBoost : 0)
        | (Exhausted ? GraphStateUpperTags.StatusExhausted : 0);

    /// <summary>
    /// Whether the persistent action layer marks the character exhausted; its tag copies it every tick. Stamina, which the
    /// server owns, decides it; the movement simulation only reads it.
    /// </summary>
    public bool Exhausted { get; init; }

    /// <summary>
    /// What the held item leaves for the movement (aim, forced walk, sprint blocks), or null while the held item is not
    /// simulated; its tags (<see cref="GraphStateFlags.HeldItem"/>) then keep their initial values.
    /// </summary>
    public HeldItemState? HeldItem { get; init; }

    /// <summary>Whether the character is jumping, falling, landing or recovering from a heavy landing.</summary>
    public bool Unsettled { get; init; }

    /// <summary>The character's body in the level, when the simulation moves it through the level's collision.</summary>
    public BodyState? Body { get; init; }

    /// <summary>The ground-movement node's own variable.</summary>
    public GroundNodeState GroundNode { get; init; } = GroundNodeState.Initial;

    /// <summary>The pawn slots holding the movement nodes' variables.</summary>
    public NodeLayout Nodes { get; init; } = NodeLayout.Initial;

    /// <summary>The stored components of the movement state's unknown velocity (<see cref="PawnBlackboard.UnknownMovementVectorXField"/>), or null for the initial ones.</summary>
    public (ulong X, ulong Y, ulong Z)? UnknownMovementVector { get; init; }
}

/// <summary>
/// Per-character input simulation: flashlight, ground movement and, with the level's collision, the position of the
/// body (walking on the ground, jumping, falling and landing). Without collision the position is the reported one.
/// </summary>
public sealed class CharacterSimulation
{
    // The capsule the vault search traces with.
    private static readonly VaultCapsule s_vaultCapsule = new(DamPlayerStarts.Capsule.Radius, DamPlayerStarts.Capsule.HalfHeight,
        DamPlayerStarts.Capsule.HalfHeight, GroundContactParameters.CrouchedHalfHeight);

    private readonly CollisionWorld? _world;

    /// <summary>Creates a character at its initial position and flashlight state.</summary>
    /// <param name="position">Initial position.</param>
    /// <param name="flashlight">Initial flashlight state.</param>
    /// <param name="world">The level's collision, or null to keep the reported position.</param>
    public CharacterSimulation(Vector3D position, bool flashlight = false, CollisionWorld? world = null)
        : this(new CharacterState(0, position, flashlight), world)
    {
    }

    private CharacterSimulation(CharacterState state, CollisionWorld? world)
    {
        _world = world;
        State = world is null || state.Body is not null ? state : state with { Body = BodyMotion.Start(world, state.Position, state.Velocity) };
    }

    /// <summary>Creates a character in a given state.</summary>
    /// <param name="state">Initial state; its body is placed in the level when it has none.</param>
    /// <param name="world">The level's collision, or null to keep the reported position.</param>
    /// <returns>The simulation.</returns>
    public static CharacterSimulation From(CharacterState state, CollisionWorld? world = null) => new(state, world);

    /// <summary>State after the most recently applied input.</summary>
    public CharacterState State { get; private set; }

    /// <summary>An independent simulation starting at the current state.</summary>
    /// <returns>The copy.</returns>
    public CharacterSimulation Copy() => (CharacterSimulation)MemberwiseClone();

    /// <summary>
    /// Continues the running ladder climb or heavy landing from a body the client reported, for the rest of that climb or
    /// landing: its path has left the client's.
    /// </summary>
    /// <param name="body">The body at the reported position.</param>
    /// <returns>The resulting character state.</returns>
    public CharacterState FollowReport(BodyState body)
    {
        State = State with
        {
            Body = State.Ladder is not null ? body with { Airborne = false } : body,
            Position = body.Position,
            Velocity = body.Velocity,
            LadderDiverged = State.Ladder is not null || State.LadderDiverged,
            LandingFollowed = State.Landing is not null,
        };
        return State;
    }

    /// <summary>Replaces the body, keeping the rest of the state.</summary>
    /// <param name="body">The body to continue from.</param>
    /// <returns>The resulting character state.</returns>
    public CharacterState Reseat(BodyState body)
    {
        // A body in the air put down on the ground has landed, heavily when it fell far enough: this tick counts as the
        // landing's first, and its animation moves the character from the next one.
        HeavyLandingState? landing = State.Landing;
        int ticks = State.HeavyLandingTicks;
        if (landing is null && State.Body is { Airborne: true } falling && !body.Airborne && LandsHeavily(falling))
        {
            bool pressed = State.RecoveryPressedAt is { } at && State.SimulationSeconds - at <= HeavyLandingParameters.RecoveryRollPressSeconds;
            landing = BeginLanding(falling, State.Yaw, pressed, blocked: false) with { Ticks = 1 };
            ticks = 1;
        }

        State = State with
        {
            Body = body,
            Position = body.Position,
            Velocity = body.Velocity,
            Landing = landing,
            HeavyLandingTicks = ticks,
            LandingRecoveryTag = ticks == 1 && landing is { } started ? started.Recovery : State.LandingRecoveryTag,
            // The re-seated body decides as well: in the air, landing or about to jump, frames wait.
            Unsettled = State.Unsettled || landing is not null || body.Airborne || body.Landing || body.JumpRequestedAt is not null,
        };
        return State;
    }

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
    /// <param name="position">Reported position, used when the body is not simulated.</param>
    /// <param name="electrified">Whether electrification disables the flashlight.</param>
    /// <param name="sprint">Sprint parameters when sprinting is allowed by the character's abilities.</param>
    /// <param name="slide">Slide parameters for the ground surface when sliding is allowed; requires sprint.</param>
    /// <param name="crouched">Crouch movement parameters; defaults to <see cref="GroundMovementParameters.Crouched"/>.</param>
    /// <param name="unsettled">Whether the character is jumping, falling or recovering from a heavy landing.</param>
    /// <param name="vaults">Vault definitions for the character's equipment when it can vault; vaults need the level's collision.</param>
    /// <param name="pitch">Control pitch in radians (positive looks up), which the vault search reads.</param>
    /// <param name="heldItem">The held item and weapon controls of this tick, or null to leave the held item out.</param>
    /// <returns>The next character state.</returns>
    public CharacterState StepOnGround(CharacterInput input, MovementInput movement, double yaw, double seconds,
        GroundMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot,
        Vector3D? position = null, bool electrified = false, SprintMovementParameters? sprint = null,
        SlideMovementParameters? slide = null, GroundMovementParameters? crouched = null, bool unsettled = false,
        VaultDefinitionSet? vaults = null, double pitch = 0, HeldItemInput? heldItem = null)
    {
        if (input.Frame != movement.Frame)
        {
            throw new ArgumentException("Movement and character claims must belong to the same frame", nameof(movement));
        }

        // A vault the client asks for is searched before the jump rule; a vault found or under way replaces this tick's movement.
        if (StepVault(input, movement, yaw, pitch, seconds, vaults, electrified, out bool vaultMissed) is { } vaulted)
        {
            return vaulted;
        }

        double strength = Math.Min(1, Math.Sqrt((movement.Forward * movement.Forward) + (movement.Right * movement.Right)) / PendulumInput.MovementAxisMaximum);
        double time = State.SimulationSeconds + seconds;
        double? started = strength == 0 ? null : State.MovementStartedAt ?? time;
        double current = Math.Sqrt((State.Velocity.X * State.Velocity.X) + (State.Velocity.Y * State.Velocity.Y));
        // A running roll, and the tick after its last step, take the place of ground movement.
        bool rollRunning = State.Roll is not null && State.RollExitTicks == 0;
        bool recovering = State is { Landing: not null, LandingFinished: false, LandingBlend.Total: 0 };

        // A crouched character only jumps where it can stand up.
        bool jump = movement.JumpPressed && !rollRunning && !recovering && (State.MovementStance != CharacterStance.Crouch || CanStand());
        BodyState? body = _world is not null && State.Body is { } previousBody ? BodyMotion.Begin(previousBody, input.Frame, jump) : null;
        bool airborne = body is { Airborne: true };

        // A landing from high enough is heavy: the character recovers before it moves on. A roll or crouch press shortly
        // before it can turn it into a recovery roll.
        double? recoveryPressedAt = (movement.DodgeRollPressed || movement.CrouchPressed)
            && (State.RecoveryPressedAt is not { } counted || time - counted > HeavyLandingParameters.RecoveryRollPressRepeatSeconds)
                ? time
                : State.RecoveryPressedAt;
        HeavyLandingState? landing = recovering ? State.Landing : null;
        if (!recovering && State.Body is { Airborne: true } falling && body is { Airborne: false } && LandsHeavily(falling))
        {
            landing = BeginLanding(falling, yaw, recoveryPressedAt is { } pressed && time - pressed <= HeavyLandingParameters.RecoveryRollPressSeconds,
                blocked: State.Landing is not null);
        }

        int heavyLanding = landing is not null ? 1 : 0;

        double? slideStarted = State.SlideStartedAt;
        SlideState? slideState = null;
        if (slideStarted is null && slide is not null && sprint is not null && !airborne && heavyLanding == 0 && !rollRunning
            && SlideMovement.CanStart(movement, current))
        {
            slideStarted = time;
            slideState = new SlideState(default, 0) { StartFrame = input.Frame };
        }

        // The press tick still sprints, and the first slide tick's ground movement is still that sprint.
        bool leavingSprint = slideStarted is not null && State.Slide is not { Ticks: > 0 };
        // Releasing crouch stands the character up only where there is room to stand.
        bool crouchHeld = movement.Buttons.HasFlag(MovementButtons.Crouch);
        bool crouching = !leavingSprint && (crouchHeld || (State.MovementStance == CharacterStance.Crouch && !airborne && !CanStand()));
        if (crouching)
        {
            parameters = crouched ?? GroundMovementParameters.Crouched;
        }

        bool shuffling = crouching && started is { } start && (input.WalkRequested
            || strength < GroundMovementParameters.WalkInputStrengthThreshold || time - start < GroundMovementParameters.CrouchShuffleSeconds);
        GroundMovementParameters actual = shuffling
            ? parameters with { MaximumSpeed = parameters.MaximumSpeed * GroundMovementParameters.CrouchShuffleSpeedFactor }
            : parameters;
        bool sprintHeld = movement.Buttons.HasFlag(MovementButtons.Sprint);
        HeldItemState? held = heldItem is { } item
            ? HeldItemMovement.Step(State.HeldItem ?? default, item, time, seconds) with { SprintHeld = sprintHeld }
            : null;
        bool itemAllowsSprint = held is not { } heldState
            || HeldItemMovement.AllowsSprint(heldState, State.SprintStartedAt, sprintHeld && State.HeldItem is not { SprintHeld: true });
        bool sprinting = sprint is not null && (leavingSprint
            || (sprintHeld && strength > 0 && !crouching && !input.WalkRequested && itemAllowsSprint));
        double? sprintStarted = State.SprintStartedAt;
        if (sprinting && sprint is { } sprintParameters)
        {
            if (State.Locomotion != LocoMode.Sprinting || sprintStarted is null)
            {
                sprintStarted = time;
            }

            actual = actual with { MaximumSpeed = SprintMovement.MaximumSpeed(parameters.MaximumSpeed, sprintParameters, time - sprintStarted.Value) };
        }
        else if (held is { ForceWalk: true } && !shuffling)
        {
            // An item that forces a walk (aiming with some weapons) sets the aiming speed, standing or crouched.
            actual = actual with { MaximumSpeed = GroundMovementParameters.Aiming.MaximumSpeed };
        }

        if (held is { } heldNow)
        {
            held = heldNow with
            {
                ReloadBlocked = heldNow.ReloadBlocksSprint
                    && HeldItemMovement.BlocksReload(sprinting && sprintStarted is { } since ? time - since : 0, heldNow.Reloading),
            };
        }

        double expectedStrength = Math.Abs(1 - strength) < GroundMovementParameters.InputStrengthSnapTolerance ? 1 : strength;
        double expected = actual.MaximumSpeed * expectedStrength;
        // Locomotion compares the whole speed, the vertical part included.
        LocoMode locomotion = Locomotion(expected, Math.Sqrt((current * current) + (State.Velocity.Z * State.Velocity.Z)), sprinting, shuffling);
        // Ground movement runs along the ground the character stands on; uphill input on steep ground is weaker.
        Vector3D underfoot = body is { Airborne: false, Ground.Carries: true } standing
            ? new Vector3D(standing.Ground.Normal.X, standing.Ground.Normal.Y, standing.Ground.Normal.Z)
            : FlatGroundMovement.Level;
        (Vector3D slopeInput, double? slopeScalar) = FlatGroundMovement.SlopeInput(FlatGroundMovement.Direction(movement, yaw), underfoot);
        Vector3D velocity = FlatGroundMovement.StepVelocity(State.Velocity, slopeInput, underfoot, seconds, actual, steerAboveMaximum, fixBrakingOvershoot);
        int exitTicks = 0;
        CharacterStance stance = crouching ? CharacterStance.Crouch : CharacterStance.Stand;
        (CharacterStance actionStance, LocoMode actionLocomotion) = (stance, locomotion);
        if (slideStarted is { } slideStart && slide is { } slideParameters && time > slideStart)
        {
            Vector3D groundNormal = body is { Airborne: false, Ground.Carries: true } onGround
                ? new Vector3D(onGround.Ground.Normal.X, onGround.Ground.Normal.Y, onGround.Ground.Normal.Z)
                : default;
            SlideStep step = FlatSlideMovement.Step(State.Slide ?? default, State.Velocity, movement, yaw, time - slideStart, (float)seconds,
                slideParameters, steerAboveMaximum, fixBrakingOvershoot, groundNormal: groundNormal, falling: airborne);

            // For its first ticks the slide's velocity only gradually replaces that of the ground movement it leaves.
            velocity = step.State.Ticks < SlideMovementParameters.BlendTicks
                ? Blend(velocity, step.Velocity, step.State.Ticks / (double)SlideMovementParameters.BlendTicks)
                : step.Velocity;

            // The finishing tick still shows the slide; the next one leaves it.
            slideState = step.State;
            slideStarted = step.Finished ? null : slideStarted;
            exitTicks = step.Finished ? 1 : 0;
            (actionStance, actionLocomotion) = (SlideMovementParameters.ActionStance, SlideMovementParameters.ActionLocomotion);

            // The slide crouches the character whatever the crouch button does; the movement state shows it a tick later.
            // The ground movement does not run, so its locomotion stays as it was.
            stance = State.Slide is { Ticks: > 0 } ? CharacterStance.Crouch : State.MovementStance;
            locomotion = State.MovementLocomotion;
        }
        else
        {
            switch (slide)
            {
                case { } exitParameters when State.SlideExitTicks is > 0 and < SlideMovementParameters.BlendTicks:
                {
                    // For a few ticks after a slide, the new movement only gradually replaces slide braking.
                    Vector3D braking = FlatGroundMovement.CalculateVelocity(State.Velocity, default, seconds,
                        SlideMovementParameters.Ground with { MaximumSpeed = exitParameters.MaximumSpeed }, steerAboveMaximum, fixBrakingOvershoot);
                    velocity = Blend(braking, velocity, State.SlideExitTicks / (double)SlideMovementParameters.BlendTicks);
                    exitTicks = State.SlideExitTicks + 1;
                    break;
                }
                case null:
                    slideStarted = null;
                    break;
            }
        }

        if (airborne && slideStarted is null)
        {
            velocity = FlatGroundMovement.CalculateVelocity(State.Velocity, FlatGroundMovement.Direction(movement, yaw), seconds,
                AirMovementParameters.Horizontal(actual.MaximumSpeed), steerAboveMaximum, fixBrakingOvershoot);
            (stance, actionStance) = (CharacterStance.InAir, CharacterStance.InAir);
        }

        DodgeRollState? roll = null;
        bool rollFinished = false;
        int rollExit = 0;
        switch (State)
        {
            case { Roll: { } running } when rollRunning:
                // The roll's animation drives the horizontal velocity; the movement state under the action is left as it was.
                DodgeRollStep step = DodgeRollMovement.Step(running, State.Velocity, !airborne, -AirMovementParameters.FallAcceleration, seconds,
                    movement.Buttons.HasFlag(MovementButtons.Crouch) && !State.RollFinished);
                velocity = step.Velocity with { Z = 0 };
                (actionStance, actionLocomotion) = (step.ActionStance, step.ActionLocomotion);
                (stance, locomotion) = (State.MovementStance, State.MovementLocomotion);
                roll = step.State;
                rollFinished = !State.RollFinished && step.Finished;
                rollExit = State.RollFinished ? 1 : 0;
                (slideStarted, slideState, exitTicks) = (null, null, 0);
                break;
            case { Roll: { } ending, RollExitTicks: > 0 and < DodgeRollParameters.ExitBlendTicks }:
                // After the roll's last animated tick, ground movement takes over from that tick's velocity in equal steps.
                Vector3D animated = DodgeRollMovement.Step(ending with { Ticks = ending.Ticks - 1 }, State.Velocity, true, 0, seconds).Velocity;
                velocity = Blend(animated, velocity, State.RollExitTicks / (double)DodgeRollParameters.ExitBlendTicks);
                rollExit = State.RollExitTicks + 1 < DodgeRollParameters.ExitBlendTicks ? State.RollExitTicks + 1 : 0;
                roll = rollExit > 0 ? ending : null;
                break;
            case { Roll: null } when heavyLanding == 0 && DodgeRollMovement.CanStart(movement, airborne):
                // The tick of the press keeps its ground or slide movement; the roll steps from the next tick and ends a slide.
                roll = DodgeRollMovement.Begin(DodgeRollMovement.Select(locomotion == LocoMode.Sprinting), movement, yaw, velocity, yaw);
                (slideStarted, exitTicks) = (null, 0);
                break;
        }

        bool landingFinished = false;
        (int Taken, int Total) landingBlend = default;
        if (landing is { } landingState)
        {
            // The landing's animation moves the character and ignores the movement input; the movement state shows the
            // landing a tick later and keeps its locomotion.
            HeavyLandingStep step = HeavyLandingMovement.Step(landingState, State.Velocity, !airborne, -AirMovementParameters.FallAcceleration, seconds);
            velocity = step.Velocity with { Z = 0 };
            (actionStance, actionLocomotion) = (step.ActionStance, step.ActionLocomotion);
            (stance, locomotion) = (landingState.Ticks == 0 ? State.MovementStance : CharacterStance.Stand, State.MovementLocomotion);
            (landing, landingFinished, heavyLanding) = (step.State, step.Finished, step.State.Ticks);
            landingBlend = step.Finished ? (0, step.ExitBlendTicks) : default;
        }
        else if (State is { Landing: { } ending, LandingBlend: { Total: > 0 } blend })
        {
            // After the last step, ground movement takes over from the animation's velocity in equal steps.
            Vector3D animated = HeavyLandingMovement.Step(ending, State.Velocity, !airborne, -AirMovementParameters.FallAcceleration, seconds).Velocity;
            velocity = Blend(animated, velocity, blend.Taken / (double)blend.Total);
            landingBlend = blend.Taken + 1 < blend.Total ? (blend.Taken + 1, blend.Total) : default;
            landing = landingBlend.Total > 0 ? ending with { Ticks = ending.Ticks + 1 } : null;
        }

        // A slide leaves the character crouched; that crouch lasts until the character stands up.
        bool crouchLatched = actionStance == CharacterStance.Slide || (crouching && State.CrouchLatched);
        if (_world is not null && body is { } begun)
        {
            BodyState moved = BodyMotion.Move(_world, begun, velocity, seconds);
            (body, position, velocity) = (moved, moved.Position, moved.Velocity);
            unsettled = unsettled || moved.Airborne || moved.Landing || moved.JumpRequestedAt is not null;
        }

        unsettled = unsettled || landing is not null;
        NodeLayout nodes = State.Nodes.Next(input.Frame, slideState, exitTicks);

        Step(input, position, electrified);
        State = State with
        {
            Body = body,
            Velocity = velocity,
            Yaw = yaw,
            SimulationSeconds = time,
            MovementStartedAt = started,
            SprintStartedAt = sprintStarted,
            Stance = actionStance,
            Locomotion = actionLocomotion,
            MovementStance = stance,
            MovementLocomotion = locomotion,
            Shuffling = shuffling,
            SlideStartedAt = slideStarted,
            Slide = slideState,
            SlideExitTicks = exitTicks,
            Roll = roll,
            RollFinished = rollFinished,
            RollExitTicks = rollExit,
            HeavyLandingTicks = heavyLanding,
            Landing = landing,
            LandingFinished = landingFinished,
            LandingBlend = landingBlend,
            LandingFollowed = landing is not null && State.LandingFollowed,
            RecoveryPressedAt = recoveryPressedAt,
            LandingRecoveryTag = heavyLanding > 0 ? landing!.Value.Recovery : State.LandingRecoveryTag,
            JumpAirTicks = body is { Airborne: true, Jumping: true } ? State.JumpAirTicks + 1 : 0,
            CrouchBlockTicks = Math.Max(0, State.CrouchBlockTicks - 1),
            CrouchLatched = crouchLatched,
            Ladder = null,
            LadderFlags = 0,
            LadderFollowedTicks = 0,
            Vault = null,
            VaultFinished = false,
            VaultMissed = vaultMissed,
            VaultFollowedTicks = 0,
            Unsettled = unsettled,
            HeldItem = held ?? State.HeldItem,
            Nodes = nodes,
            GroundNode = nodes.GroundAdded
                ? GroundNodeState.Fresh(input.Frame, movement)
                : State.GroundNode.Next(input.Frame, movement, slideState is { Ticks: > 0 } && shuffling ? parameters.MaximumSpeed : actual.MaximumSpeed, sprinting,
                    sprinting && Nullable.Equals(sprintStarted, time), sprinting && sprint is { } sustained && slideStarted is null && actual.MaximumSpeed > sustained.BaseSpeed,
                    locomotion, stance, shuffling && slideState is not { Ticks: > 0 }, slideState is { Ticks: > 0 },
                    slideState is { Ticks: > 0 } ? null : slopeScalar),
        };
        return State;
    }

    /// <summary>
    /// Climbs a ladder: getting onto its line, climbing, sprinting up, sliding down, falling off, dropping off by crouching
    /// and stepping off at the bottom move the character along the ladder. The phases the game plays as animations (the
    /// top entry, climbing off the top, jumping off) follow the positions the client reports. The action layer shows the
    /// ladder; the movement state under it is left as it was.
    /// </summary>
    /// <param name="input">Character button claims.</param>
    /// <param name="movement">Movement axes and buttons from the same frame.</param>
    /// <param name="yaw">Control heading in radians.</param>
    /// <param name="seconds">Simulation step duration in seconds.</param>
    /// <param name="reported">Position the client reports after the tick, if known.</param>
    /// <param name="before">Position the client reported before the tick, if known.</param>
    /// <param name="start">The climb this tick's input starts (<see cref="LadderMovement.Begin"/>); null to go on.</param>
    /// <param name="unsettled">Whether the reported positions show the character jumping, falling or landing.</param>
    /// <param name="electrified">Whether electrification disables the flashlight.</param>
    /// <returns>The next character state.</returns>
    public CharacterState StepOnLadder(CharacterInput input, MovementInput movement, double yaw, double seconds, Vector3D? reported, Vector3D? before,
        LadderState? start = null, bool unsettled = false, bool electrified = false)
    {
        double time = State.SimulationSeconds + seconds;
        if ((start ?? State.Ladder) is not { } climbing)
        {
            // An animated phase after the ladder.
            Vector3D followed = reported ?? State.Position;
            Step(input, followed, electrified);
            return Follow(seconds, followed, before, yaw, unsettled, time, 0, State.LadderFollowedTicks + 1);
        }

        Vector3D feet = State.Body?.Position ?? State.Position;
        LadderStep step = LadderMovement.Step(climbing, LadderInput.From(movement, input, yaw, exhausted: false), feet, State.Velocity,
            DamPlayerStarts.Capsule.HalfHeight, seconds, _world);
        bool animated = step.Exit is LadderExit.JumpOff or LadderExit.ClimbOffTop;
        (Vector3D position, Vector3D velocity) = (climbing.PlayingTopEntry || animated) && reported is { } client
            ? (client, before is { } previous ? new Vector3D((client.X - previous.X) / seconds, (client.Y - previous.Y) / seconds, (client.Z - previous.Z) / seconds) : default)
            : (step.Position, step.Velocity);
        LadderState? next = step.Exit == LadderExit.None ? step.State : null;

        // On the ladder the character counts as on the ground; leaving it, the body is in the air when the floor is below.
        BodyState? body = _world is not null ? BodyMotion.Start(_world, position, velocity) with { Jumping = false } : null;
        if (next is not null && body is { } attached)
        {
            body = attached with { Airborne = false };
        }

        Step(input, position, electrified);
        AdvanceNodes(input.Frame);
        State = State with
        {
            Body = body,
            Velocity = velocity,
            Yaw = step.Yaw,
            SimulationSeconds = time,
            MovementStartedAt = null,
            Stance = CharacterStance.Ladder,
            Locomotion = step.ActionLocomotion,
            Shuffling = false,
            SlideStartedAt = null,
            Slide = null,
            SlideExitTicks = 0,
            Roll = null,
            RollFinished = false,
            RollExitTicks = 0,
            HeavyLandingTicks = 0,
            Landing = null,
            LandingFinished = false,
            LandingBlend = default,
            LandingFollowed = false,
            JumpAirTicks = 0,
            CrouchLatched = false,
            Ladder = next,
            LadderFlags = step.Flags,
            LadderFollowedTicks = animated ? 1 : 0,
            LadderDiverged = start is null && State.LadderDiverged,
            CrouchBlockTicks = (step.Flags & LadderParameters.CrouchInputBlockedFlag) != 0 ? LadderMovement.CrouchBlockTicks : Math.Max(0, State.CrouchBlockTicks - 1),
            Vault = null,
            VaultFinished = false,
            VaultMissed = false,
            VaultFollowedTicks = 0,
            Unsettled = unsettled,
        };
        return State;
    }

    /// <summary>
    /// Follows a character through a vault along a path the simulation does not know: its position is the one its client
    /// reports. The action layer shows the vault; the movement state under it is left as it was.
    /// </summary>
    /// <param name="input">Character button claims.</param>
    /// <param name="seconds">Simulation step duration in seconds.</param>
    /// <param name="reported">Position the client reports after the tick.</param>
    /// <param name="before">Position the client reported before the tick, if known.</param>
    /// <param name="yaw">Control heading in radians.</param>
    /// <param name="unsettled">Whether the reported positions show the character jumping, falling or landing.</param>
    /// <param name="electrified">Whether electrification disables the flashlight.</param>
    /// <returns>The next character state.</returns>
    public CharacterState StepFollowingVault(CharacterInput input, double seconds, Vector3D reported, Vector3D? before, double yaw,
        bool unsettled = false, bool electrified = false)
    {
        int followed = State.VaultFollowedTicks + 1;
        Step(input, reported, electrified);
        AdvanceNodes(input.Frame);
        return Follow(seconds, reported, before, yaw, unsettled, State.SimulationSeconds + seconds, followed, 0);
    }

    // A tick the ground movement does not run: running transitions go on, a slide it cut short ends, and the ground node
    // starts again where that left it without one.
    private void AdvanceNodes(uint frame)
    {
        NodeLayout nodes = State.Nodes.Next(frame, null, 0);
        State = State with
        {
            Nodes = nodes,
            GroundNode = nodes.GroundAdded ? GroundNodeState.Fresh(frame, new MovementInput(frame, 0, 0, default)) : State.GroundNode,
        };
    }

    /// <summary>
    /// Turns the latest tick into the first one of a vault followed from the client's reports: the vault it asked for was
    /// not found, or the client left the simulated vault's path.
    /// </summary>
    /// <param name="seconds">Simulation step duration in seconds.</param>
    /// <param name="reported">Position the client reports after the tick.</param>
    /// <param name="before">Position the client reported before the tick, if known.</param>
    /// <param name="unsettled">Whether the reported positions show the character jumping, falling or landing.</param>
    /// <returns>The resulting character state.</returns>
    public CharacterState FollowVault(double seconds, Vector3D reported, Vector3D? before, bool unsettled = false) =>
        Follow(seconds, reported, before, State.Yaw, unsettled, State.SimulationSeconds, 1, 0);

    // The state of a tick whose position is the one the client reports: a followed vault, or a ladder phase played by an
    // animation.
    private CharacterState Follow(double seconds, Vector3D reported, Vector3D? before, double yaw, bool unsettled, double time, int vaultTicks, int ladderTicks)
    {
        Vector3D velocity = before is { } start
            ? new Vector3D((reported.X - start.X) / seconds, (reported.Y - start.Y) / seconds, (reported.Z - start.Z) / seconds)
            : default;
        BodyState? body = _world is not null ? BodyMotion.Start(_world, reported, velocity) with { Jumping = false } : null;
        bool vault = vaultTicks > 0;
        State = State with
        {
            Position = reported,
            Body = body,
            Velocity = velocity,
            Yaw = yaw,
            SimulationSeconds = time,
            MovementStartedAt = null,
            Stance = vault ? VaultParameters.ActionStance : CharacterStance.Uncontrolled,
            Locomotion = vault ? VaultParameters.ActionLocomotion : LocoMode.Still,
            Shuffling = false,
            SlideStartedAt = null,
            Slide = null,
            SlideExitTicks = 0,
            Roll = null,
            RollFinished = false,
            RollExitTicks = 0,
            HeavyLandingTicks = 0,
            Landing = null,
            LandingFinished = false,
            LandingBlend = default,
            LandingFollowed = false,
            JumpAirTicks = 0,
            CrouchLatched = false,
            Ladder = null,
            LadderFlags = 0,
            LadderFollowedTicks = ladderTicks,
            CrouchBlockTicks = Math.Max(0, State.CrouchBlockTicks - 1),
            Vault = null,
            VaultFinished = false,
            VaultMissed = false,
            VaultFollowedTicks = vaultTicks,
            Unsettled = unsettled,
        };
        return State;
    }

    // A vault step: the search on the tick the client asks for a vault, then the vault's path until its last step.
    private CharacterState? StepVault(CharacterInput input, MovementInput movement, double yaw, double pitch, double seconds,
        VaultDefinitionSet? vaults, bool electrified, out bool missed)
    {
        missed = false;
        if (_world is null || State.Body is not { } body)
        {
            return null;
        }

        VaultState? vault = State is { Vault: { } running, VaultFinished: false } ? running : null;
        if (vault is null)
        {
            if (vaults is null || State.Vault is not null || State.Roll is not null || State.Slide is not null || State.HeavyLandingTicks > 0
                || !VaultDetection.Requested(input) || !VaultDetection.FallSlowEnough(State.Velocity))
            {
                return null;
            }

            VaultQuery query = new(body.Position with { Z = body.Position.Z + s_vaultCapsule.HalfHeight }, yaw, yaw, pitch, DodgeRollMovement.Direction(movement),
                State.Velocity, body.Ground.Supported ? body.Ground.Normal : Vector3.UnitZ, body.Ground.Carries, body.Airborne, s_vaultCapsule);
            if (VaultDetection.FallCatch(body.Airborne, State.Velocity) || VaultDetection.Trace(_world, query) is not { } trace
                || VaultDetection.Select(vaults, trace, body.Airborne, State.Velocity) is not { } clip || !VaultMovement.CanPlay(clip))
            {
                missed = true;
                return null;
            }

            vault = VaultMovement.Begin(clip, vaults, trace, body.Position, State.Velocity, !body.Airborne);
        }

        // The vault places the feet along its path; over an obstacle the character is in the air, onto one on the ground.
        VaultStep step = VaultMovement.Step(vault.Value, body.Position, seconds);
        bool over = step.Mode == RoughMovementMode.Air;
        BodyState moved = BodyMotion.Start(_world, step.Position, step.Velocity) with { Airborne = over, Jumping = false, JumpRequestedAt = null, Landing = false };
        Step(input, step.Position, electrified);
        AdvanceNodes(input.Frame);
        State = State with
        {
            Body = moved,
            Velocity = step.Velocity,
            Yaw = yaw,
            SimulationSeconds = State.SimulationSeconds + seconds,
            Stance = step.ActionStance,
            Locomotion = step.ActionLocomotion,
            MovementStance = over ? CharacterStance.InAir : CharacterStance.Crouch,
            MovementLocomotion = over ? VaultParameters.ActionLocomotion : State.MovementLocomotion,
            Shuffling = false,
            SlideStartedAt = null,
            Slide = null,
            SlideExitTicks = 0,
            Roll = null,
            RollFinished = false,
            RollExitTicks = 0,
            HeavyLandingTicks = 0,
            Landing = null,
            LandingFinished = false,
            LandingBlend = default,
            CrouchLatched = !over,
            Ladder = null,
            LadderFollowedTicks = 0,
            Vault = step.State,
            VaultFinished = step.Finished,
            VaultMissed = false,
            VaultFollowedTicks = 0,
            Unsettled = false,
        };
        return State;
    }

    // The heavy landing of a body that falls onto the ground: its level from the fall height, its animation from the impact
    // direction against the heading (the control heading stands in for the game's smoothed steering angle).
    private static HeavyLandingState BeginLanding(BodyState falling, double heading, bool pressed, bool blocked)
    {
        double height = HeavyLandingParameters.FallHeight(falling.Position.Z - falling.Ground.Height, falling.Velocity.Z, AirMovementParameters.FallAcceleration);
        Vector3D impact = new(Math.Round(falling.Velocity.X), Math.Round(falling.Velocity.Y), Math.Round(falling.Velocity.Z));
        return HeavyLandingMovement.Begin(height, impact, falling.Velocity, heading, HeavyLandingMovement.IsRecoveryRoll(height, pressed, blocked));
    }

    // Whether a body falling onto the ground lands heavily.
    private static bool LandsHeavily(BodyState falling) =>
        HeavyLandingParameters.FallHeight(falling.Position.Z - falling.Ground.Height, falling.Velocity.Z, AirMovementParameters.FallAcceleration)
        > HeavyLandingParameters.Height;

    // Whether the character, crouched where it is, has room to stand up.
    private bool CanStand() => _world is null || State.Body is not { } body || BodyMotion.CanStand(_world, body.Position);

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

        return expectingMovement ? LocoMode.Running : State.MovementLocomotion;
    }

    // The velocity a share of the way from one movement's to another's.
    private static Vector3D Blend(Vector3D from, Vector3D to, double share) =>
        new(from.X + ((to.X - from.X) * share), from.Y + ((to.Y - from.Y) * share), 0);
}
