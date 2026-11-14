using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Movement;

/// <summary>An interaction component's settings that decide what it does to movement.</summary>
/// <param name="Type">Press, hold or continuous.</param>
/// <param name="HoldSeconds">Seconds the interaction is held after the alignment (0 for none).</param>
/// <param name="AlignmentSeconds">Seconds the character lines up before the interaction (or its hold) starts.</param>
/// <param name="ForceImmobilize">Whether the interaction immobilizes even without a hold.</param>
/// <param name="CancelOnMovementInput">Whether movement input cancels the interaction while it immobilizes.</param>
/// <param name="DesiredDistance">Horizontal distance from the interaction the character is lined up at.</param>
/// <param name="DistanceTolerance">How far the character may be from the desired distance before it is moved.</param>
/// <param name="PromptDistance">Prompt distance of the interaction.</param>
/// <param name="EquipsRaiderTool">Whether the interaction needs the raider tool in hand (its hold is the breach hold).</param>
/// <param name="IsLoot">Whether the interaction searches a loot container.</param>
/// <param name="OpensLootWindow">Whether the finished hold opens the looting window (the interaction is restaged, not finished).</param>
/// <param name="OwnContainer">The component's own animation container, or null.</param>
/// <param name="Container">The animation container the interactor remembers: the own one, else the character's fallback.</param>
public sealed record InteractionSettings(
    StandardInteractionType Type,
    double HoldSeconds,
    double AlignmentSeconds,
    bool ForceImmobilize,
    bool CancelOnMovementInput,
    double DesiredDistance,
    double DistanceTolerance,
    double PromptDistance,
    bool EquipsRaiderTool,
    bool IsLoot,
    bool OpensLootWindow,
    InteractionAnimationContainer? OwnContainer,
    InteractionAnimationContainer Container)
{
    /// <summary>Whether the interaction is held or continuous.</summary>
    public bool HoldOrContinuous => HoldSeconds > 0 || Type == StandardInteractionType.Continuous;

    /// <summary>Whether the character is immobilized while the interaction runs.</summary>
    public bool Immobilizes => ForceImmobilize || HoldOrContinuous;

    /// <summary>Whether the interaction plays as root motion in the interact node.</summary>
    public bool RootMotion => OwnContainer is { Alignment: not InteractionAlignment.None };

    /// <summary>A loot container's search.</summary>
    /// <param name="type">The container class.</param>
    /// <param name="opened">Whether the container has been opened before (no hold, no tool).</param>
    /// <param name="semanticTags">Semantic tags of the search, which pick the fallback container; the first-search tag when null.</param>
    /// <returns>The settings.</returns>
    public static InteractionSettings ForLootContainer(LootContainerClass type, bool opened, IReadOnlyCollection<string>? semanticTags = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        InteractionAnimationContainer? own = type.SearchAnimation is { } path ? InteractionAnimationContainers.Find(path) : null;
        return new InteractionSettings(StandardInteractionType.Press, opened ? LootInteractions.OpenedHoldSeconds : type.HoldSeconds,
            type.AlignmentSeconds, InteractionMovementParameters.LootSearchImmobilizes, true,
            InteractionMovementParameters.DefaultDesiredCharacterDistance, InteractionMovementParameters.DefaultDesiredCharacterDistanceTolerance,
            opened ? type.OpenedPromptDistance : type.PromptDistance, type.EquipsRaiderTool && !opened, true, true, own,
            own ?? InteractionMovement.FallbackContainer(semanticTags ?? [InteractionMovementParameters.LootSearchSemanticTag]));
    }

    /// <summary>A door's open or close button.</summary>
    /// <returns>The settings.</returns>
    public static InteractionSettings ForDoorButton() =>
        new(StandardInteractionType.Press, 0, InteractionMovementParameters.DoorButtonAlignmentSeconds, false, true,
            InteractionMovementParameters.DefaultDesiredCharacterDistance, InteractionMovementParameters.DefaultDesiredCharacterDistanceTolerance,
            InteractionMovementParameters.StandardPromptDistance, false, false, false, null, InteractionMovement.FallbackContainer([]));

    /// <summary>A door's breach button.</summary>
    /// <returns>The settings.</returns>
    public static InteractionSettings ForDoorBreach() =>
        new(StandardInteractionType.Hold, DamDoors.BreachHoldSeconds, DamDoors.BreachAlignmentSeconds, false, true,
            InteractionMovementParameters.DefaultDesiredCharacterDistance, InteractionMovementParameters.DefaultDesiredCharacterDistanceTolerance,
            InteractionMovementParameters.StationaryPromptDistance, true, false, false, InteractionAnimationContainers.DoorBreach,
            InteractionAnimationContainers.DoorBreach);

    /// <summary>An extraction elevator's call or descend button.</summary>
    /// <param name="button">The button.</param>
    /// <returns>The settings.</returns>
    public static InteractionSettings ForElevatorButton(ExtractionButton button)
    {
        if (button.InteractionType is not { } type)
        {
            throw new ArgumentException("A repair button has no interaction settings.", nameof(button));
        }

        InteractionAnimationContainer? own = button.AnimationContainer is { } path ? InteractionAnimationContainers.Find(path) : null;
        bool immobilizes = button.HoldSeconds > 0 || type == StandardInteractionType.Continuous;
        return new InteractionSettings(type, button.HoldSeconds, button.AlignmentSeconds, false, true,
            InteractionMovementParameters.DefaultDesiredCharacterDistance, InteractionMovementParameters.DefaultDesiredCharacterDistanceTolerance,
            immobilizes ? InteractionMovementParameters.StationaryPromptDistance : InteractionMovementParameters.StandardPromptDistance,
            false, false, false, own, own ?? InteractionMovement.FallbackContainer([]));
    }
}

/// <summary>The interactor layer's state of the character's interaction.</summary>
/// <param name="Event">The interaction's last event.</param>
/// <param name="RequestedTick">Simulation tick of the request.</param>
/// <param name="AlignmentStartedTick">Simulation tick the character started lining up.</param>
/// <param name="HoldStartedTick">Simulation tick the interaction (or its hold) started.</param>
/// <param name="LastAcceptableDirection">Input direction that does not cancel the interaction.</param>
/// <param name="Sequence">The character's interaction count.</param>
public readonly record struct InteractorState(
    LastInteractionEvent Event = LastInteractionEvent.None,
    int RequestedTick = 0,
    int AlignmentStartedTick = 0,
    int HoldStartedTick = 0,
    InputDirection LastAcceptableDirection = InputDirection.None,
    uint Sequence = 0)
{
    /// <summary>Whether an interaction runs: lining up, started or restaged.</summary>
    public bool Interacting => Event is LastInteractionEvent.AlignmentStarted or LastInteractionEvent.Started or LastInteractionEvent.Restaged;
}

/// <summary>What the interactor layer reads on a tick.</summary>
/// <param name="Tick">Simulation tick.</param>
/// <param name="Pressed">Whether the input asks to start the focused interaction.</param>
/// <param name="FocusValid">Whether an interaction is focused.</param>
/// <param name="FocusChanged">Whether the focused interaction changed on this tick.</param>
/// <param name="CanAccess">Whether the character may use the focused interaction (the server's flag in its state).</param>
/// <param name="PromptDistanceSquared">Squared distance from the character to the interaction's prompt.</param>
/// <param name="Direction">Eight-way direction of the movement input (persistent action layer).</param>
/// <param name="Strength">Strength of the movement input, 0 to 1 (persistent action layer).</param>
/// <param name="RaiderToolReady">Whether the raider tool is in hand and fully equipped.</param>
/// <param name="Blocked">Whether the input claims a block tag or the graph holds <c>Ability.Block.ContextInteract</c>.</param>
/// <param name="EquipRequested">Whether the input asks to equip an item.</param>
/// <param name="LootingMenuClosed">Whether the input reports the looting menu closed.</param>
/// <param name="Enabled">Whether the interaction is enabled.</param>
/// <param name="EndedSuccessfully">Whether a disabled interaction ended successfully.</param>
/// <param name="BreachMultiplier">The interactor's breach duration multiplier.</param>
/// <param name="PersistedSeconds">Hold time the interaction kept from earlier holds.</param>
public readonly record struct InteractorInput(
    int Tick,
    bool Pressed,
    bool FocusValid,
    bool FocusChanged,
    bool CanAccess,
    double PromptDistanceSquared,
    InputDirection Direction,
    double Strength,
    bool RaiderToolReady = false,
    bool Blocked = false,
    bool EquipRequested = false,
    bool LootingMenuClosed = false,
    bool Enabled = true,
    bool EndedSuccessfully = false,
    double BreachMultiplier = 1,
    double PersistedSeconds = 0);

/// <summary>How an interaction changes the ground node's crouch.</summary>
public enum InteractionCrouch : byte
{
    /// <summary>The crouch follows the player.</summary>
    Unchanged = 0,

    /// <summary>The crouch button does not crouch: the interaction is above <see cref="InteractionMovementParameters.NoCrouchHeight"/>.</summary>
    Suppressed = 1,

    /// <summary>The character crouches: the interaction is below its container's crouch height.</summary>
    Forced = 2,
}

/// <summary>What a running interaction does to the ground node on a tick.</summary>
/// <param name="Aligning">Whether the character is being lined up (its movement input is replaced).</param>
/// <param name="AlignmentDirection">World direction of the full-strength input that lines the character up, zero when not aligning.</param>
/// <param name="ZeroesInput">Whether the player's movement input is zeroed (immobilized and not aligning).</param>
/// <param name="LocomotionFromCurrentSpeed">Whether the locomotion mode follows the current speed instead of the expected one.</param>
/// <param name="Crouch">How the crouch changes.</param>
public readonly record struct InteractionGroundEffect(
    bool Aligning,
    Vector3D AlignmentDirection,
    bool ZeroesInput,
    bool LocomotionFromCurrentSpeed,
    InteractionCrouch Crouch);

/// <summary>Interactions and their effect on movement: the interactor layer's events and the ground node's alignment.</summary>
public static class InteractionMovement
{
    /// <summary>Advances the interactor layer by one simulation tick.</summary>
    /// <param name="state">Interactor state.</param>
    /// <param name="settings">The focused (or running) interaction's settings.</param>
    /// <param name="input">What the layer reads.</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <returns>The interactor state after the tick.</returns>
    public static InteractorState Advance(InteractorState state, InteractionSettings settings, InteractorInput input, double seconds)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        double reach = Math.Pow(settings.PromptDistance + InteractionMovementParameters.DistanceLeeway, 2);
        if (input.FocusChanged && state.Event == LastInteractionEvent.Requested)
        {
            state = state with { Event = LastInteractionEvent.None };
        }

        if (input is { Pressed: true, FocusValid: true } && !state.Interacting && input.PromptDistanceSquared < reach)
        {
            state = state with { Event = LastInteractionEvent.Requested, RequestedTick = input.Tick };
        }

        if (state.Interacting)
        {
            if (Stopped(state, settings, input, reach) is { } stopped)
            {
                return state with { Event = stopped };
            }

            if (settings is { CancelOnMovementInput: true, Immobilizes: true } && input.Strength < InteractionMovementParameters.CancelInputStrength)
            {
                state = state with { LastAcceptableDirection = InputDirection.None };
            }

            if (state.Event == LastInteractionEvent.Restaged && input.LootingMenuClosed)
            {
                return state with { Event = LastInteractionEvent.Succeeded };
            }
        }

        if (state.Event == LastInteractionEvent.Requested)
        {
            if (!input.FocusValid || Reached(input.Tick, state.RequestedTick, InteractionMovementParameters.RequestTimeoutSeconds, seconds))
            {
                return state with { Event = LastInteractionEvent.Failed };
            }

            if (input.CanAccess && (!settings.EquipsRaiderTool || input.RaiderToolReady))
            {
                state = state with
                {
                    Event = LastInteractionEvent.AlignmentStarted,
                    AlignmentStartedTick = input.Tick,
                    LastAcceptableDirection = input.Direction,
                    Sequence = state.Sequence + 1,
                };
            }
        }

        if (state.Event == LastInteractionEvent.AlignmentStarted && Reached(input.Tick, state.AlignmentStartedTick, settings.AlignmentSeconds, seconds))
        {
            state = state with { Event = LastInteractionEvent.Started, HoldStartedTick = input.Tick };
        }

        if (state.Event != LastInteractionEvent.Started || settings.Type == StandardInteractionType.Continuous)
        {
            return state;
        }

        double hold = (settings.EquipsRaiderTool ? settings.HoldSeconds * input.BreachMultiplier : settings.HoldSeconds) - input.PersistedSeconds;
        return Reached(input.Tick, state.HoldStartedTick, hold, seconds)
            ? state with { Event = settings.OpensLootWindow ? LastInteractionEvent.Restaged : LastInteractionEvent.Succeeded }
            : state;
    }

    /// <summary>Whether the persistent action layer holds the interaction tags.</summary>
    /// <param name="state">Interactor state after the tick.</param>
    /// <param name="settings">The interaction's settings.</param>
    /// <returns>True while an immobilizing interaction runs.</returns>
    public static bool HoldsTags(InteractorState state, InteractionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return state.Interacting && settings.Immobilizes;
    }

    /// <summary>Whether the graph moves the character into the root-motion interact node.</summary>
    /// <param name="state">Interactor state.</param>
    /// <param name="settings">The interaction's settings.</param>
    /// <param name="character">The character's location.</param>
    /// <param name="interaction">World location of the interaction (its prompt).</param>
    /// <returns>True when the interaction plays as root motion.</returns>
    public static bool EntersRootMotion(InteractorState state, InteractionSettings settings, Vector3D character, Vector3D interaction)
    {
        ArgumentNullException.ThrowIfNull(settings);
        double x = interaction.X - character.X;
        double y = interaction.Y - character.Y;
        return state.Interacting && settings.RootMotion
            && (x * x) + (y * y) <= InteractionMovementParameters.MarkerFarDistance * InteractionMovementParameters.MarkerFarDistance;
    }

    /// <summary>What a running interaction does to the ground node on a tick.</summary>
    /// <param name="state">Interactor state.</param>
    /// <param name="settings">The interaction's settings.</param>
    /// <param name="feet">The character's feet (capsule centre lowered by the current half height).</param>
    /// <param name="interaction">World location of the interaction, where the interactor started it.</param>
    /// <param name="immobilized">Whether the character is immobilized (input claim or <c>Status.Immobilized</c> in the graph).</param>
    /// <param name="blendingOut">Whether the ground node is blending out to another node.</param>
    /// <returns>The effect.</returns>
    public static InteractionGroundEffect Ground(InteractorState state, InteractionSettings settings, Vector3D feet, Vector3D interaction,
        bool immobilized, bool blendingOut = false)
    {
        ArgumentNullException.ThrowIfNull(settings);
        bool aligning = false;
        Vector3D direction = default;
        InteractionCrouch crouch = InteractionCrouch.Unchanged;
        if (!state.Interacting || blendingOut)
        {
            return new InteractionGroundEffect(aligning, direction, immobilized && !aligning, immobilized, crouch);
        }

        double x = interaction.X - feet.X;
        double y = interaction.Y - feet.Y;
        double height = interaction.Z - feet.Z;
        double distance = Math.Sqrt((x * x) + (y * y));
        if (Math.Abs(distance - settings.DesiredDistance) > settings.DistanceTolerance)
        {
            aligning = true;
            double sign = distance < settings.DesiredDistance ? -1 : 1;
            direction = distance > 0 ? new Vector3D(sign * x / distance, sign * y / distance, 0) : default;
        }

        if (!settings.Container.FullBody)
        {
            crouch = height < settings.Container.CrouchHeight ? InteractionCrouch.Forced
                : height > InteractionMovementParameters.NoCrouchHeight ? InteractionCrouch.Suppressed
                : InteractionCrouch.Unchanged;
        }

        return new InteractionGroundEffect(aligning, direction, immobilized && !aligning, immobilized, crouch);
    }

    /// <summary>
    /// The character's fallback animation container for an interaction without its own: the first entry whose semantic
    /// tags the interaction has (a tag also counts for its parents), else the first entry.
    /// </summary>
    /// <param name="semanticTags">The interaction's semantic tags.</param>
    /// <returns>The container.</returns>
    public static InteractionAnimationContainer FallbackContainer(IReadOnlyCollection<string> semanticTags)
    {
        ArgumentNullException.ThrowIfNull(semanticTags);
        TaggedInteractionAnimationContainer entry = InteractionAnimationContainers.TagBased.FirstOrDefault(candidate =>
            candidate.SemanticTags.Any(tag => semanticTags.Any(own => own == tag || own.StartsWith(tag + ".", StringComparison.Ordinal))))
            ?? InteractionAnimationContainers.TagBased[0];
        return InteractionAnimationContainers.Find(entry.Container)
            ?? throw new InvalidOperationException($"Fallback container {entry.Container} is not listed.");
    }

    // The event a running interaction ends with on this tick, in the interactor layer's checks, or null when it goes on.
    private static LastInteractionEvent? Stopped(InteractorState state, InteractionSettings settings, InteractorInput input, double reach)
    {
        if (!input.Enabled)
        {
            return input.EndedSuccessfully ? LastInteractionEvent.Succeeded : LastInteractionEvent.Failed;
        }

        bool turned = settings is { CancelOnMovementInput: true, Immobilizes: true } && input.Strength >= InteractionMovementParameters.CancelInputStrength
            && input.Direction != state.LastAcceptableDirection;
        bool cancelled = input.Blocked || input.PromptDistanceSquared > reach || (settings.HoldOrContinuous && input.EquipRequested)
            || (settings.EquipsRaiderTool && !input.RaiderToolReady) || turned;

        // A cancelled loot search that already opened its window ends successfully; anything else fails.
        return !cancelled ? null
            : settings.IsLoot && state.Event == LastInteractionEvent.Restaged ? LastInteractionEvent.Succeeded
            : LastInteractionEvent.Failed;
    }

    // Whether the given seconds have passed from a start tick to a tick.
    private static bool Reached(int tick, int start, double seconds, double step) =>
        ((tick - start) * step) >= seconds - InteractionMovementParameters.ElapsedTolerance;
}
