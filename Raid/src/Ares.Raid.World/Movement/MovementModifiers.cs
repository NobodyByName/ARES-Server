using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>The speed attributes the ground node reads every tick (current values).</summary>
/// <param name="MaxSpeed"><c>MaxSpeedModifier</c>: scales every ground speed.</param>
/// <param name="Run"><c>RunSpeedModifier</c>: scales the run speeds, the stick remapping and the slide entry speed.</param>
/// <param name="Sprint"><c>SprintSpeedModifier</c>: scales the sprint speeds.</param>
/// <param name="Crouch"><c>CrouchSpeedModifier</c>: scales the crouch speed.</param>
/// <param name="Ads"><c>ADSSpeedModifier</c>: scales the speed while the held item forces a walk.</param>
public readonly record struct SpeedAttributes(float MaxSpeed, float Run, float Sprint, float Crouch, float Ads)
{
    /// <summary>The attributes of a new character.</summary>
    public static SpeedAttributes Default { get; } = new(
        MovementModifierParameters.StartingMaxSpeedModifier,
        MovementModifierParameters.StartingRunSpeedModifier,
        MovementModifierParameters.StartingSprintSpeedModifier,
        MovementModifierParameters.StartingCrouchSpeedModifier,
        MovementModifierParameters.StartingAdsSpeedModifier);

    /// <summary>
    /// The attributes with one more effect multiplying <c>MaxSpeedModifier</c> (as the heavy-load effect does with its
    /// magnitude).
    /// </summary>
    /// <param name="magnitude">The effect's magnitude.</param>
    /// <returns>The changed attributes.</returns>
    public SpeedAttributes WithMaxSpeedMultiplier(float magnitude) => this with { MaxSpeed = MaxSpeed * magnitude };
}

/// <summary>How a carried load compares with the carrying limit.</summary>
public enum CarriedLoad : byte
{
    /// <summary>Below the carrying limit.</summary>
    Normal = 0,

    /// <summary>At or above the carrying limit.</summary>
    OverEncumbered = 1,

    /// <summary>At or above the carrying limit plus <see cref="MovementModifierParameters.SuperHeavyWeightMargin"/>.</summary>
    SuperHeavy = 2,
}

/// <summary>What changes the ground movement from outside the movement graph during one tick.</summary>
/// <param name="Attributes">The speed attributes.</param>
/// <param name="Heavy">Whether the character has <c>Attribute.Movement.Heavy</c>.</param>
/// <param name="SuperHeavy">Whether the character has <c>Attribute.Movement.SuperHeavy</c>.</param>
/// <param name="ForcedWalk">Whether the character has <c>Ability.Walk</c> (carrying an object).</param>
/// <param name="SlowedByCarryable">Whether the character has <c>GameplayEffect.CarryableSlowedMovement</c>.</param>
/// <param name="Exhausted">Whether the character has <c>Status.Exhausted</c>.</param>
/// <param name="DisableExhaustedVault">Whether the character has <c>GameplayEffect.DisableExhaustedVault</c>.</param>
/// <param name="EncumberedSprinting">Whether the skill <c>CharacterSkillEffect.EncumberedSprinting</c> is enabled.</param>
/// <param name="EncumberedMoveSpeedMultiplier">Skill value <c>EncumberedMoveSpeedMultiplier</c>.</param>
/// <param name="CrouchSpeedMultiplier">Skill value <c>CrouchMovementSpeedMultiplier</c>.</param>
public readonly record struct MovementModifierInput(
    SpeedAttributes Attributes,
    bool Heavy = false,
    bool SuperHeavy = false,
    bool ForcedWalk = false,
    bool SlowedByCarryable = false,
    bool Exhausted = false,
    bool DisableExhaustedVault = false,
    bool EncumberedSprinting = false,
    double EncumberedMoveSpeedMultiplier = 1,
    double CrouchSpeedMultiplier = 1)
{
    /// <summary>A new character's input: default attributes, no load effects, statuses or skills.</summary>
    public static MovementModifierInput Default { get; } = new(SpeedAttributes.Default);
}

/// <summary>
/// Ground speeds and blocked abilities after attributes, carried load and statuses: the ground node's speed table
/// (<c>GetMovementParameterModifications</c>) and the load rules of the roll, vault and jump. Speeds are world units per
/// second before the node stores the chosen one as a float.
/// </summary>
/// <param name="ArmedRunSpeed">Running with an equipped weapon.</param>
/// <param name="UnarmedRunSpeed">Running with empty hands.</param>
/// <param name="CrouchSpeed">Crouch movement.</param>
/// <param name="CrouchShuffleSpeed">Initial crouch movement (shuffle); only <c>MaxSpeedModifier</c> scales it.</param>
/// <param name="AimingSpeed">Movement while the held item forces a walk.</param>
/// <param name="WalkingSpeed">Slow walking; only <c>MaxSpeedModifier</c> scales it.</param>
/// <param name="ArmedSprintSpeed">Sustained sprint with an equipped weapon.</param>
/// <param name="UnarmedSprintSpeed">Sustained sprint with empty hands.</param>
/// <param name="StickRemapBaseSpeed">Base speed of the stick remapping.</param>
/// <param name="RunScale">
/// <c>MaxSpeedModifier</c> times <c>RunSpeedModifier</c> (a float): the multiplier of the slide entry speed
/// (<see cref="SlideMovement.CanStart"/>).
/// </param>
/// <param name="SprintBurstAllowed">Whether a sprint starts with the authored burst (not with a heavy load).</param>
/// <param name="WalkForced">Whether the upright ground speed is forced to a walk (super-heavy load or carrying).</param>
/// <param name="JumpBlocked">Whether jumping is blocked (<c>Ability.Block.Jump</c> from the super-heavy effect).</param>
/// <param name="HeavyRoll">Whether the roll picks its heavy-load variant.</param>
/// <param name="ExhaustedVaults">Whether vaults use the exhausted definition set.</param>
/// <param name="GraphTags">Graph-state tag bits the load effects hold.</param>
public readonly record struct MovementModifiers(
    double ArmedRunSpeed,
    double UnarmedRunSpeed,
    double CrouchSpeed,
    double CrouchShuffleSpeed,
    double AimingSpeed,
    double WalkingSpeed,
    double ArmedSprintSpeed,
    double UnarmedSprintSpeed,
    double StickRemapBaseSpeed,
    double RunScale,
    bool SprintBurstAllowed,
    bool WalkForced,
    bool JumpBlocked,
    bool HeavyRoll,
    bool ExhaustedVaults,
    ulong GraphTags)
{
    /// <summary>The modifiers of a new character: the unchanged base speeds, nothing blocked.</summary>
    public static MovementModifiers Default { get; } = From(MovementModifierInput.Default);

    /// <summary>Horizontal speed from which a slide may start.</summary>
    public double SlideEntrySpeed => SlideMovementParameters.EntrySpeed * RunScale;

    /// <summary>Running parameters for the equipment.</summary>
    /// <param name="armed">Whether a weapon is equipped.</param>
    /// <returns>The ground parameters.</returns>
    public GroundMovementParameters Run(bool armed) =>
        GroundMovementParameters.Unarmed with { MaximumSpeed = armed ? ArmedRunSpeed : UnarmedRunSpeed };

    /// <summary>Sprint parameters for the equipment.</summary>
    /// <param name="armed">Whether a weapon is equipped.</param>
    /// <returns>The sprint parameters.</returns>
    public SprintMovementParameters Sprint(bool armed) =>
        new(armed ? ArmedSprintSpeed : UnarmedSprintSpeed, BurstAllowed: SprintBurstAllowed);

    /// <summary>Computes the modifiers.</summary>
    /// <param name="input">Attributes, load effects, statuses and skill values.</param>
    /// <returns>The modifiers.</returns>
    public static MovementModifiers From(MovementModifierInput input)
    {
        SpeedAttributes attributes = input.Attributes;
        double maxSpeed = attributes.MaxSpeed;
        double run = attributes.MaxSpeed * attributes.Run;
        double sprint = attributes.MaxSpeed * attributes.Sprint;
        double crouch = attributes.MaxSpeed * attributes.Crouch * input.CrouchSpeedMultiplier;
        double aiming = attributes.MaxSpeed * attributes.Ads;
        double crouchBase = GroundMovementParameters.Crouched.MaximumSpeed;
        float stick = attributes.MaxSpeed * MovementModifierParameters.StickRemapBaseSpeed * attributes.Run;
        ulong tags = (input.Heavy ? GraphStateTags.AttributeMovementHeavy : 0) | (input.SuperHeavy ? GraphStateTags.AttributeMovementSuperHeavy : 0);
        return new MovementModifiers(
            GroundMovementParameters.Armed.MaximumSpeed * run,
            GroundMovementParameters.Unarmed.MaximumSpeed * run,
            crouchBase * crouch,
            crouchBase * GroundMovementParameters.CrouchShuffleSpeedFactor * maxSpeed,
            GroundMovementParameters.Aiming.MaximumSpeed * aiming,
            GroundMovementParameters.Walking.MaximumSpeed * maxSpeed,
            SprintMovementParameters.Armed.BaseSpeed * sprint,
            SprintMovementParameters.Unarmed.BaseSpeed * sprint,
            stick,
            run,
            !input.Heavy,
            input.SuperHeavy || input.ForcedWalk,
            input.SuperHeavy,
            input.Heavy,
            input is { Exhausted: true, DisableExhaustedVault: false } || input.Heavy,
            tags);
    }

    /// <summary>
    /// The upright ground speed while <see cref="WalkForced"/>: a super-heavy character with the encumbered-sprinting
    /// skill sprints at a share of the sprint speed; a character slowed by a carried object runs at the armed run speed
    /// while it sprints and is not exhausted; otherwise it walks. A super-heavy load then multiplies the speed by the
    /// encumbered skill value. Crouching is not affected.
    /// </summary>
    /// <param name="input">The input the modifiers were computed from.</param>
    /// <param name="armed">Whether a weapon is equipped.</param>
    /// <param name="sprintClaimed">Whether the sprint input is held.</param>
    /// <param name="sprintAllowed">Whether the sprint rule of the ground node would sprint this tick.</param>
    /// <returns>The maximum speed the ground node stores, or null when the walk is not forced.</returns>
    public double? ForcedWalkSpeed(MovementModifierInput input, bool armed, bool sprintClaimed, bool sprintAllowed)
    {
        if (!WalkForced)
        {
            return null;
        }

        double speed;
        if (input is { SuperHeavy: true, EncumberedSprinting: true } && sprintClaimed && sprintAllowed)
        {
            speed = (armed ? ArmedSprintSpeed : UnarmedSprintSpeed) * MovementModifierParameters.EncumberedSprintSpeedShare;
        }
        else if (input.SlowedByCarryable && sprintClaimed && !input.Exhausted)
        {
            speed = ArmedRunSpeed;
        }
        else
        {
            speed = WalkingSpeed;
        }

        float stored = (float)speed;
        return input.SuperHeavy ? (float)(stored * input.EncumberedMoveSpeedMultiplier) : stored;
    }

    /// <summary>How a carried weight compares with the carrying limit (the thresholds the game's HUD shows).</summary>
    /// <param name="weight">The <c>Weight</c> attribute.</param>
    /// <param name="maximum">The <c>MaxWeight</c> attribute.</param>
    /// <returns>The load class.</returns>
    public static CarriedLoad Classify(float weight, float maximum)
    {
        if (weight >= maximum + MovementModifierParameters.SuperHeavyWeightMargin)
        {
            return CarriedLoad.SuperHeavy;
        }

        return weight >= maximum ? CarriedLoad.OverEncumbered : CarriedLoad.Normal;
    }
}
