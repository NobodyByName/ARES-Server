using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World.Movement;

public sealed class InteractionMovementTests
{
    private const double Tick = 1.0 / 30;

    private static readonly LootContainerClass s_plain = LootContainerClasses.All.First(type =>
        type is { SearchAnimation: null, HoldSeconds: 0, AlignmentSeconds: InteractionMovementParameters.DefaultAlignmentSeconds });

    private static readonly LootContainerClass s_breach = LootContainerClasses.All.First(type =>
        type.SearchAnimation is { } path && InteractionAnimationContainers.Find(path) == InteractionAnimationContainers.PryFromAbove);

    private static InteractorInput Input(int tick, bool pressed = false, InputDirection direction = InputDirection.None, double strength = 0) =>
        new(tick, pressed, FocusValid: true, FocusChanged: false, CanAccess: true, PromptDistanceSquared: 100 * 100, direction, strength);

    private static InteractorState Run(InteractorState state, InteractionSettings settings, int from, int to, Func<int, InteractorInput>? input = null)
    {
        for (int tick = from; tick <= to; tick++)
        {
            state = InteractionMovement.Advance(state, settings, input?.Invoke(tick) ?? Input(tick), Tick);
        }

        return state;
    }

    [Fact]
    public void A_plain_search_lines_up_for_half_a_second_then_opens_the_looting_window_while_holding_the_immobilizing_tags()
    {
        InteractionSettings search = InteractionSettings.ForLootContainer(s_plain, opened: false);
        InteractorState pressed = InteractionMovement.Advance(default, search, Input(100, pressed: true), Tick);
        Assert.Equal(LastInteractionEvent.AlignmentStarted, pressed.Event);
        Assert.Equal(1u, pressed.Sequence);
        Assert.True(InteractionMovement.HoldsTags(pressed, search));
        Assert.False(search.RootMotion);

        InteractorState aligning = Run(pressed, search, 101, 114);
        Assert.Equal(LastInteractionEvent.AlignmentStarted, aligning.Event);
        InteractorState opened = Run(aligning, search, 115, 115);
        Assert.Equal(LastInteractionEvent.Restaged, opened.Event);
        Assert.Equal(115, opened.HoldStartedTick);
        Assert.True(InteractionMovement.HoldsTags(opened, search));

        InteractorState closed = InteractionMovement.Advance(opened, search, Input(200) with { LootingMenuClosed = true }, Tick);
        Assert.Equal(LastInteractionEvent.Succeeded, closed.Event);
        Assert.False(InteractionMovement.HoldsTags(closed, search));
    }

    [Fact]
    public void A_door_button_lines_up_for_six_ticks_and_never_immobilizes()
    {
        InteractionSettings button = InteractionSettings.ForDoorButton();
        Assert.False(button.Immobilizes);
        Assert.Equal(InteractionAnimationContainers.LootLocker, button.Container);
        InteractorState pressed = InteractionMovement.Advance(default, button, Input(10, pressed: true), Tick);
        Assert.False(InteractionMovement.HoldsTags(pressed, button));
        Assert.Equal(LastInteractionEvent.AlignmentStarted, Run(pressed, button, 11, 15).Event);
        Assert.Equal(LastInteractionEvent.Succeeded, Run(pressed, button, 11, 16).Event);
    }

    [Fact]
    public void A_breach_search_waits_for_the_raider_tool_plays_as_root_motion_and_holds_three_and_a_half_seconds()
    {
        InteractionSettings breach = InteractionSettings.ForLootContainer(s_breach, opened: false);
        Assert.True(breach.RootMotion);
        Assert.True(breach.EquipsRaiderTool);
        Assert.Equal(3.5, breach.HoldSeconds);

        InteractorState waiting = InteractionMovement.Advance(default, breach, Input(0, pressed: true), Tick);
        Assert.Equal(LastInteractionEvent.Requested, waiting.Event);
        Assert.Equal(LastInteractionEvent.Requested, Run(waiting, breach, 1, 89).Event);
        Assert.Equal(LastInteractionEvent.Failed, Run(waiting, breach, 1, 90).Event);

        Func<int, InteractorInput> ready = tick => Input(tick) with { RaiderToolReady = true };
        InteractorState aligning = Run(waiting, breach, 10, 10, ready);
        Assert.Equal(LastInteractionEvent.AlignmentStarted, aligning.Event);
        Assert.True(InteractionMovement.EntersRootMotion(aligning, breach, new Vector3D(0, 0, 0), new Vector3D(300, 200, 0)));
        Assert.False(InteractionMovement.EntersRootMotion(aligning, breach, new Vector3D(0, 0, 0), new Vector3D(300, 250, 0)));

        // One second of alignment, then the 3.5 s hold (105 ticks) before the looting window opens.
        InteractorState holding = Run(aligning, breach, 11, 40, ready);
        Assert.Equal(LastInteractionEvent.Started, holding.Event);
        Assert.Equal(LastInteractionEvent.Started, Run(holding, breach, 41, 144, ready).Event);
        Assert.Equal(LastInteractionEvent.Restaged, Run(holding, breach, 41, 145, ready).Event);
    }

    [Fact]
    public void A_strong_turn_cancels_an_immobilizing_interaction_but_the_starting_direction_and_weak_input_do_not()
    {
        InteractionSettings search = InteractionSettings.ForLootContainer(s_plain, opened: false);
        InteractorState pressed = InteractionMovement.Advance(default, search, Input(0, pressed: true, InputDirection.Forward, 1), Tick);
        Assert.Equal(InputDirection.Forward, pressed.LastAcceptableDirection);

        Assert.Equal(LastInteractionEvent.AlignmentStarted,
            InteractionMovement.Advance(pressed, search, Input(1, direction: InputDirection.Forward, strength: 1), Tick).Event);
        Assert.Equal(LastInteractionEvent.Failed,
            InteractionMovement.Advance(pressed, search, Input(1, direction: InputDirection.Left, strength: 0.75), Tick).Event);

        InteractorState released = InteractionMovement.Advance(pressed, search, Input(1, direction: InputDirection.Left, strength: 0.74), Tick);
        Assert.Equal(LastInteractionEvent.AlignmentStarted, released.Event);
        Assert.Equal(InputDirection.None, released.LastAcceptableDirection);
        Assert.Equal(LastInteractionEvent.Failed,
            InteractionMovement.Advance(released, search, Input(2, direction: InputDirection.Forward, strength: 1), Tick).Event);

        InteractorState opened = Run(pressed, search, 1, 15);
        Assert.Equal(LastInteractionEvent.Restaged, opened.Event);
        Assert.Equal(LastInteractionEvent.Succeeded,
            InteractionMovement.Advance(opened, search, Input(16, direction: InputDirection.Backward, strength: 1), Tick).Event);

        InteractionSettings button = InteractionSettings.ForDoorButton();
        InteractorState door = InteractionMovement.Advance(default, button, Input(0, pressed: true), Tick);
        Assert.Equal(LastInteractionEvent.AlignmentStarted,
            InteractionMovement.Advance(door, button, Input(1, direction: InputDirection.Left, strength: 1), Tick).Event);
    }

    [Fact]
    public void An_interaction_starts_within_its_prompt_distance_plus_the_leeway_and_ends_beyond_it()
    {
        InteractionSettings search = InteractionSettings.ForLootContainer(s_plain, opened: false);
        double reach = s_plain.PromptDistance + InteractionMovementParameters.DistanceLeeway;
        Assert.Equal(270, reach);
        Assert.Equal(LastInteractionEvent.None,
            InteractionMovement.Advance(default, search, Input(0, pressed: true) with { PromptDistanceSquared = reach * reach }, Tick).Event);

        InteractorState pressed = InteractionMovement.Advance(default, search, Input(0, pressed: true), Tick);
        Assert.Equal(LastInteractionEvent.Failed,
            InteractionMovement.Advance(pressed, search, Input(1) with { PromptDistanceSquared = (reach * reach) + 1 }, Tick).Event);
        Assert.Equal(LastInteractionEvent.Failed, InteractionMovement.Advance(pressed, search, Input(1) with { Blocked = true }, Tick).Event);
        Assert.Equal(LastInteractionEvent.Succeeded,
            InteractionMovement.Advance(pressed, search, Input(1) with { Enabled = false, EndedSuccessfully = true }, Tick).Event);
    }

    [Fact]
    public void A_request_without_access_waits_and_a_focus_change_drops_it()
    {
        InteractionSettings button = InteractionSettings.ForDoorButton();
        InteractorState requested = InteractionMovement.Advance(default, button, Input(0, pressed: true) with { CanAccess = false }, Tick);
        Assert.Equal(LastInteractionEvent.Requested, requested.Event);
        Assert.Equal(LastInteractionEvent.None,
            InteractionMovement.Advance(requested, button, Input(1) with { CanAccess = false, FocusChanged = true }, Tick).Event);
        Assert.Equal(LastInteractionEvent.Failed, InteractionMovement.Advance(requested, button, Input(1) with { FocusValid = false }, Tick).Event);
    }

    [Fact]
    public void The_ground_node_lines_the_character_up_between_fifty_and_one_hundred_units_from_the_interaction()
    {
        InteractionSettings search = InteractionSettings.ForLootContainer(s_plain, opened: false);
        InteractorState running = new(LastInteractionEvent.AlignmentStarted);
        Vector3D interaction = new(0, 0, 100);

        InteractionGroundEffect far = InteractionMovement.Ground(running, search, new Vector3D(-100.5, 0, 0), interaction, immobilized: true);
        Assert.True(far.Aligning);
        Assert.Equal(new Vector3D(1, 0, 0), far.AlignmentDirection);
        Assert.False(far.ZeroesInput);
        Assert.True(far.LocomotionFromCurrentSpeed);

        InteractionGroundEffect near = InteractionMovement.Ground(running, search, new Vector3D(0, -49.5, 0), interaction, immobilized: true);
        Assert.Equal(new Vector3D(0, -1, 0), near.AlignmentDirection);

        InteractionGroundEffect lined = InteractionMovement.Ground(running, search, new Vector3D(-100, 0, 0), interaction, immobilized: true);
        Assert.False(lined.Aligning);
        Assert.True(lined.ZeroesInput);

        InteractionGroundEffect free = InteractionMovement.Ground(running, InteractionSettings.ForDoorButton(), new Vector3D(-75, 0, 0), interaction,
            immobilized: false);
        Assert.Equal(new InteractionGroundEffect(false, default, false, false, InteractionCrouch.Unchanged), free);
        Assert.False(InteractionMovement.Ground(default, search, new Vector3D(-200, 0, 0), interaction, immobilized: false).Aligning);
        Assert.False(InteractionMovement.Ground(running, search, new Vector3D(-200, 0, 0), interaction, immobilized: false, blendingOut: true).Aligning);
    }

    [Theory]
    [InlineData(49.9, InteractionCrouch.Forced)]
    [InlineData(50, InteractionCrouch.Unchanged)]
    [InlineData(150, InteractionCrouch.Unchanged)]
    [InlineData(150.1, InteractionCrouch.Suppressed)]
    public void A_low_interaction_crouches_the_character_and_a_high_one_keeps_it_standing(double height, InteractionCrouch crouch)
    {
        InteractionSettings search = InteractionSettings.ForLootContainer(s_plain, opened: false);
        InteractionGroundEffect effect = InteractionMovement.Ground(new InteractorState(LastInteractionEvent.Started), search, new Vector3D(-75, 0, 0),
            new Vector3D(0, 0, height), immobilized: true);
        Assert.Equal(crouch, effect.Crouch);
    }

    [Fact]
    public void Containers_and_tags_are_the_decoded_ones()
    {
        Assert.Equal(InteractionAlignment.LookAtAlignment, InteractionAnimationContainers.PryFromAbove.Alignment);
        Assert.Equal(InteractionAlignment.LookAtAlignment, InteractionAnimationContainers.PryFromBelow.Alignment);
        Assert.Equal(InteractionAlignment.LookAtMatrixComponentUp, InteractionAnimationContainers.DoorBreach.Alignment);
        Assert.Equal(InteractionAlignment.ComponentAlignment, InteractionAnimationContainers.ExtractionElevatorBox.Alignment);
        Assert.Equal(75, InteractionAnimationContainers.ScavengeHold.CrouchHeight);
        Assert.All(InteractionAnimationContainers.All, container => Assert.False(container.FullBody));

        Assert.Equal(InteractionAnimationContainers.LootOpenCupboard, InteractionMovement.FallbackContainer([InteractionMovementParameters.LootSearchSemanticTag]));
        Assert.Equal(InteractionAnimationContainers.LootOpenLid, InteractionMovement.FallbackContainer(["Animation.Interaction.Container.OpenLid"]));
        Assert.Equal(InteractionAnimationContainers.LootOpenCupboard, InteractionMovement.FallbackContainer(["Animation.Interaction.Container.OpenCupboard"]));
        Assert.Equal(InteractionAnimationContainers.LootLocker, InteractionMovement.FallbackContainer([]));

        Assert.True(InteractionSettings.ForDoorBreach().RootMotion);
        ExtractionButton descend = DamExtraction.Buttons.First(button => button.Role == ExtractionButtonRole.Descend);
        ExtractionButton call = DamExtraction.Buttons.First(button => button.Role == ExtractionButtonRole.Summon);
        Assert.True(InteractionSettings.ForElevatorButton(descend).RootMotion);
        Assert.True(InteractionSettings.ForElevatorButton(descend).Immobilizes);
        Assert.False(InteractionSettings.ForElevatorButton(call).Immobilizes);

        Assert.Equal(1UL << 62, InteractionMovementParameters.Tags);
        Assert.Equal(0x3fUL, InteractionMovementParameters.UpperTags);
        Assert.Equal((1UL << 0) | (1UL << 1) | (1UL << 2) | (1UL << 4) | (1UL << 63), InteractionMovementParameters.RootMotionNodeTags);
    }
}
