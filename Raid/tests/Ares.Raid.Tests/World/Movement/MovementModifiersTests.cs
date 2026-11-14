using Ares.Raid.World.Movement;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class MovementModifiersTests
{
    [Fact]
    public void Default_attributes_keep_the_base_ground_speeds()
    {
        MovementModifiers modifiers = MovementModifiers.Default;
        Assert.Equal(300, modifiers.ArmedRunSpeed);
        Assert.Equal(375, modifiers.UnarmedRunSpeed);
        Assert.Equal(150, modifiers.CrouchSpeed);
        Assert.Equal(75, modifiers.CrouchShuffleSpeed);
        Assert.Equal(150, modifiers.AimingSpeed);
        Assert.Equal(200, modifiers.WalkingSpeed);
        Assert.Equal(550, modifiers.ArmedSprintSpeed);
        Assert.Equal(650, modifiers.UnarmedSprintSpeed);
        Assert.Equal(300, modifiers.StickRemapBaseSpeed);
        Assert.Equal(200, modifiers.SlideEntrySpeed);
        Assert.True(modifiers.SprintBurstAllowed);
        Assert.False(modifiers.WalkForced || modifiers.JumpBlocked || modifiers.HeavyRoll || modifiers.ExhaustedVaults);
        Assert.Equal(0UL, modifiers.GraphTags);
        Assert.Equal(GroundMovementParameters.Armed, modifiers.Run(armed: true));
        Assert.Equal(SprintMovementParameters.Unarmed, modifiers.Sprint(armed: false));
    }

    [Fact]
    public void Attribute_pairs_multiply_as_floats_before_scaling_the_base_speeds()
    {
        SpeedAttributes attributes = new(0.9f, 1.1f, 1.2f, 0.7f, 0.6f);
        MovementModifiers modifiers = MovementModifiers.From(new MovementModifierInput(attributes, CrouchSpeedMultiplier: 1.25));
        const float Run = 0.9f * 1.1f;
        Assert.Equal(300 * (double)Run, modifiers.ArmedRunSpeed);
        Assert.Equal(375 * (double)Run, modifiers.UnarmedRunSpeed);
        Assert.Equal(150 * (0.9f * 0.7f * 1.25), modifiers.CrouchSpeed);
        Assert.Equal(75 * (double)0.9f, modifiers.CrouchShuffleSpeed);
        Assert.Equal(150 * (double)(0.9f * 0.6f), modifiers.AimingSpeed);
        Assert.Equal(200 * (double)0.9f, modifiers.WalkingSpeed);
        Assert.Equal(650 * (double)(0.9f * 1.2f), modifiers.UnarmedSprintSpeed);
        Assert.Equal(0.9f * 300f * 1.1f, modifiers.StickRemapBaseSpeed);
        Assert.Equal(Run, modifiers.RunScale);
        Assert.Equal(200 * (double)Run, modifiers.SlideEntrySpeed);
    }

    [Fact]
    public void A_heavy_load_scales_through_the_max_speed_attribute_and_drops_the_sprint_burst()
    {
        MovementModifierInput input = new(SpeedAttributes.Default.WithMaxSpeedMultiplier(0.5f), Heavy: true);
        MovementModifiers modifiers = MovementModifiers.From(input);
        Assert.Equal(187.5, modifiers.UnarmedRunSpeed);
        Assert.Equal(325, modifiers.UnarmedSprintSpeed);
        Assert.False(modifiers.SprintBurstAllowed);
        Assert.False(modifiers.Sprint(armed: false).BurstAllowed);
        Assert.True(modifiers.HeavyRoll);
        Assert.True(modifiers.ExhaustedVaults);
        Assert.False(modifiers.WalkForced);
        Assert.False(modifiers.JumpBlocked);
        Assert.Equal(GraphStateTags.AttributeMovementHeavy, modifiers.GraphTags);
        Assert.Null(modifiers.ForcedWalkSpeed(input, armed: false, sprintClaimed: true, sprintAllowed: true));
    }

    [Fact]
    public void A_super_heavy_load_forces_a_walk_and_blocks_the_jump()
    {
        MovementModifierInput input = new(SpeedAttributes.Default, SuperHeavy: true);
        MovementModifiers modifiers = MovementModifiers.From(input);
        Assert.True(modifiers.WalkForced);
        Assert.True(modifiers.JumpBlocked);
        Assert.False(modifiers.HeavyRoll);
        Assert.Equal(GraphStateTags.AttributeMovementSuperHeavy, modifiers.GraphTags);
        Assert.Equal(200, modifiers.ForcedWalkSpeed(input, armed: false, sprintClaimed: true, sprintAllowed: true));
        Assert.Equal(150, modifiers.CrouchSpeed);
    }

    [Fact]
    public void The_encumbered_sprinting_skill_lets_a_super_heavy_character_sprint_at_a_share_of_the_sprint_speed()
    {
        MovementModifierInput input = new(SpeedAttributes.Default, SuperHeavy: true, EncumberedSprinting: true, EncumberedMoveSpeedMultiplier: 1.5);
        MovementModifiers modifiers = MovementModifiers.From(input);
        Assert.Equal((float)((float)(650 * 0.525) * 1.5), modifiers.ForcedWalkSpeed(input, armed: false, sprintClaimed: true, sprintAllowed: true));
        Assert.Equal((float)((float)(550 * 0.525) * 1.5), modifiers.ForcedWalkSpeed(input, armed: true, sprintClaimed: true, sprintAllowed: true));
        Assert.Equal(300, modifiers.ForcedWalkSpeed(input, armed: false, sprintClaimed: false, sprintAllowed: true));
    }

    [Fact]
    public void A_carried_object_runs_at_the_armed_speed_while_sprinting_unless_exhausted()
    {
        MovementModifierInput input = new(SpeedAttributes.Default, ForcedWalk: true, SlowedByCarryable: true);
        MovementModifiers modifiers = MovementModifiers.From(input);
        Assert.False(modifiers.JumpBlocked);
        Assert.Equal(300, modifiers.ForcedWalkSpeed(input, armed: false, sprintClaimed: true, sprintAllowed: false));
        Assert.Equal(200, modifiers.ForcedWalkSpeed(input, armed: false, sprintClaimed: false, sprintAllowed: false));
        MovementModifierInput exhausted = input with { Exhausted = true };
        Assert.Equal(200, MovementModifiers.From(exhausted).ForcedWalkSpeed(exhausted, armed: false, sprintClaimed: true, sprintAllowed: false));
    }

    [Fact]
    public void Exhaustion_picks_the_exhausted_vaults_unless_disabled_but_a_heavy_load_always_does()
    {
        Assert.True(MovementModifiers.From(MovementModifierInput.Default with { Exhausted = true }).ExhaustedVaults);
        Assert.False(MovementModifiers.From(MovementModifierInput.Default with { Exhausted = true, DisableExhaustedVault = true }).ExhaustedVaults);
        Assert.True(MovementModifiers.From(MovementModifierInput.Default with { Heavy = true, DisableExhaustedVault = true }).ExhaustedVaults);
    }

    [Theory]
    [InlineData(29.9f, 30f, CarriedLoad.Normal)]
    [InlineData(30f, 30f, CarriedLoad.OverEncumbered)]
    [InlineData(59.9f, 30f, CarriedLoad.OverEncumbered)]
    [InlineData(60f, 30f, CarriedLoad.SuperHeavy)]
    [InlineData(130f, 100f, CarriedLoad.SuperHeavy)]
    public void Loads_are_classified_against_the_carrying_limit(float weight, float maximum, CarriedLoad expected)
    {
        Assert.Equal(expected, MovementModifiers.Classify(weight, maximum));
    }

    [Fact]
    public void Ice_replaces_the_friction_and_scales_acceleration_and_braking()
    {
        GroundMovementParameters ground = GroundMovementParameters.Unarmed;
        GroundMovementParameters walking = IceMovement.OnGround(ground, PhysicalSurfaceType.Ice, 0.25f, IceMovementParameters.General);
        Assert.Equal(0.25, walking.Friction);
        Assert.Equal(600, walking.Acceleration);
        Assert.Equal(40, walking.BrakingDeceleration);
        Assert.Equal(ground.MaximumSpeed, walking.MaximumSpeed);
        Assert.Equal(ground.BrakingFrictionFactor, walking.BrakingFrictionFactor);

        GroundMovementParameters sliding = IceMovement.OnGround(SlideMovementParameters.Ground, PhysicalSurfaceType.Ice, 0.25f, IceMovementParameters.Sliding);
        Assert.Equal((float)(0.25f * 0.8), sliding.Friction);
        Assert.Equal(800, sliding.Acceleration);
        Assert.Equal(80, sliding.BrakingDeceleration);
    }

    [Fact]
    public void Other_surfaces_leave_the_ground_parameters_unchanged()
    {
        Assert.Equal(GroundMovementParameters.Unarmed, IceMovement.OnGround(GroundMovementParameters.Unarmed, PhysicalSurfaceType.Snow, 0.25f, IceMovementParameters.General));
    }
}
