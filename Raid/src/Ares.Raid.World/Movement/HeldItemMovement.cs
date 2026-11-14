using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>The held item and the weapon controls in one tick, as the movement rules read them.</summary>
/// <param name="Item">The held item's movement members.</param>
public readonly record struct HeldItemInput(HeldItemMovementParameters Item)
{
    /// <summary>Whether the held item is a weapon (its fire state drives the fire tag).</summary>
    public bool Weapon { get; init; }

    /// <summary>Whether the item's data enables aiming.</summary>
    public bool EnablesZoom { get; init; }

    /// <summary>
    /// Whether the item and the character allow aiming now; a weapon does not while it reloads, works its bolt or
    /// bashes.
    /// </summary>
    public bool ZoomAllowed { get; init; }

    /// <summary>Whether the item forces an aim regardless of the controls.</summary>
    public bool ForcingZoom { get; init; }

    /// <summary>Whether an equip or unequip is under way, which ends an aim.</summary>
    public bool Changing { get; init; }

    /// <summary>Whether the character may start a primary action; otherwise the use-sprint block is cleared.</summary>
    public bool PrimaryStartAllowed { get; init; } = true;

    /// <summary>Primary control held.</summary>
    public bool FireDown { get; init; }

    /// <summary>Primary control blocked.</summary>
    public bool FireBlocked { get; init; }

    /// <summary>Secondary (aim) control held.</summary>
    public bool SecondaryDown { get; init; }

    /// <summary>Secondary (aim) control held on the previous input.</summary>
    public bool SecondaryDownBefore { get; init; }

    /// <summary>Toggle-aim control held.</summary>
    public bool ToggleZoomDown { get; init; }

    /// <summary>Toggle-aim control held on the previous input.</summary>
    public bool ToggleZoomDownBefore { get; init; }

    /// <summary>Whether the client allows the item's primary use.</summary>
    public bool AllowPrimary { get; init; }

    /// <summary>Whether the client allows the item's secondary use.</summary>
    public bool AllowSecondary { get; init; }

    /// <summary>Reload control held.</summary>
    public bool ReloadDown { get; init; }

    /// <summary>Reload control held on the previous input.</summary>
    public bool ReloadDownBefore { get; init; }

    /// <summary>Whether the item is in its primary use after its own update this tick.</summary>
    public bool BeingUsed { get; init; }

    /// <summary>Whether the item is in its secondary use after its own update this tick.</summary>
    public bool SecondaryBeingUsed { get; init; }

    /// <summary>Whether a primary use is still buffered after the item's update this tick.</summary>
    public bool PrimaryUseBuffered { get; init; }

    /// <summary>Whether the weapon fired this tick.</summary>
    public bool Fired { get; init; }

    /// <summary>Whether the weapon is in its firing state.</summary>
    public bool Firing { get; init; }

    /// <summary>Whether the weapon is in its reloading state.</summary>
    public bool Reloading { get; init; }

    /// <summary>
    /// Whether a running reload blocks sprinting: the game's reload-sprint option is on and no effect allows reloading
    /// while sprinting.
    /// </summary>
    public bool ReloadBlocksSprint { get; init; }

    /// <summary>Whether the primary control fires: held and not blocked.</summary>
    public bool PrimaryHeld => FireDown && !FireBlocked;

    /// <summary>Whether the secondary control is held, by the aim or the toggle-aim control.</summary>
    public bool SecondaryHeld => SecondaryDown || ToggleZoomDown;
}

/// <summary>What the held item leaves for the movement after a tick.</summary>
public readonly record struct HeldItemState
{
    /// <summary>Whether the character aims.</summary>
    public bool Zoom { get; init; }

    /// <summary>Whether the aim started this tick, which blocks the sprint input for the tick.</summary>
    public bool ZoomStarted { get; init; }

    /// <summary>When the latest aim ended, in simulation seconds.</summary>
    public double? ZoomEndedAt { get; init; }

    /// <summary>Whether the item forces a walk.</summary>
    public bool ForceWalk { get; init; }

    /// <summary>Timer (simulation time units, one byte) blocking sprinting after a use of the item.</summary>
    public int UseSprintBlockTimer { get; init; }

    /// <summary>When the reload control was last pressed, in simulation seconds.</summary>
    public double? ReloadPressedAt { get; init; }

    /// <summary>Whether the weapon reloads.</summary>
    public bool Reloading { get; init; }

    /// <summary>Whether a running reload blocks sprinting (<see cref="HeldItemInput.ReloadBlocksSprint"/>).</summary>
    public bool ReloadBlocksSprint { get; init; }

    /// <summary>Whether a sprint blocks the running reload this tick (<see cref="HeldItemMovement.BlocksReload"/>).</summary>
    public bool ReloadBlocked { get; init; }

    /// <summary>Whether the sprint control was held this tick; a sprint press is a held control not held before.</summary>
    public bool SprintHeld { get; init; }

    /// <summary>Whether the primary-use tag is set: a weapon that fired or fires, another item in use.</summary>
    public bool PrimaryUseTag { get; init; }
}

/// <summary>Rules by which the held item changes movement: aiming, item use, forced walking and sprint blocks.</summary>
public static class HeldItemMovement
{
    /// <summary>Advances the held item's movement state by one tick.</summary>
    /// <param name="previous">The state after the previous tick.</param>
    /// <param name="input">The held item and controls of this tick.</param>
    /// <param name="time">Simulation time of this tick, in seconds.</param>
    /// <param name="seconds">Step duration in seconds.</param>
    /// <returns>The state after this tick.</returns>
    public static HeldItemState Step(HeldItemState previous, HeldItemInput input, double time, double seconds)
    {
        bool zoom = !input.Changing && WantsZoom(previous.Zoom, input) && input is { EnablesZoom: true, ZoomAllowed: true }
            && !ZoomRetriggerBlocked(previous, input.Item, time);
        bool secondaryUse = zoom || (input.SecondaryHeld && (input.SecondaryBeingUsed || input.AllowSecondary));
        bool primaryWalk = input.Item.WalkOnPrimaryUse && input.PrimaryHeld && (input.BeingUsed || input.AllowPrimary);
        bool primaryTag = input.Weapon ? input.Fired || input.Firing : input.BeingUsed;
        return new HeldItemState
        {
            Zoom = zoom,
            ZoomStarted = zoom && !previous.Zoom,
            ZoomEndedAt = previous.Zoom && !zoom ? time : previous.ZoomEndedAt,
            ForceWalk = primaryWalk || (input.Item.WalkOnSecondaryUse && secondaryUse),
            UseSprintBlockTimer = NextUseSprintBlockTimer(previous.UseSprintBlockTimer, input, seconds),
            ReloadPressedAt = input is { ReloadDown: true, ReloadDownBefore: false } ? time : previous.ReloadPressedAt,
            Reloading = input.Reloading,
            ReloadBlocksSprint = input.ReloadBlocksSprint,
            SprintHeld = previous.SprintHeld,
            PrimaryUseTag = primaryTag,
        };
    }

    /// <summary>The aim the controls ask for: held aim, release, toggle press, or an item that forces it.</summary>
    /// <param name="zoom">Whether the character aimed after the previous tick.</param>
    /// <param name="input">The controls of this tick.</param>
    /// <returns>Whether the controls ask to aim.</returns>
    private static bool WantsZoom(bool zoom, HeldItemInput input)
    {
        bool wanted = zoom;
        if (input.SecondaryDown)
        {
            wanted = true;
        }
        else if (input.SecondaryDownBefore)
        {
            wanted = false;
        }
        else if (input is { ToggleZoomDown: true, ToggleZoomDownBefore: false })
        {
            wanted = !zoom;
        }

        return wanted || input.ForcingZoom;
    }

    /// <summary>
    /// The use-sprint block timer: set when a use starts or lasts (if the item blocks sprinting during use), cleared when
    /// no primary action may start, and counted down on ticks without a shot.
    /// </summary>
    /// <param name="timer">The timer after the previous tick, in simulation time units.</param>
    /// <param name="input">The held item of this tick.</param>
    /// <param name="seconds">Step duration in seconds.</param>
    /// <returns>The timer after this tick.</returns>
    public static int NextUseSprintBlockTimer(int timer, HeldItemInput input, double seconds)
    {
        if (!input.PrimaryStartAllowed)
        {
            timer = 0;
        }
        else if (input.Item.BlockSprintDuringPrimaryUse && (input.PrimaryUseBuffered || input.Fired || input.BeingUsed || input.SecondaryBeingUsed))
        {
            // The timer is one byte: a longer block wraps around.
            timer = TimeUnits(input.Item.BlockSprintAfterPrimaryUseSeconds + HeldItemMovementParameters.SprintBlockAfterUseExtraSeconds) & byte.MaxValue;
        }

        return !input.Fired && timer > 0 ? Math.Max(0, timer - TimeUnits(seconds)) : timer;
    }

    /// <summary>
    /// Whether the held item lets the character sprint: no use-sprint block, no forced walk, and, where a running
    /// reload blocks sprinting, a sprint started after the reload press or pressed this tick.
    /// </summary>
    /// <param name="state">The held item's state after this tick.</param>
    /// <param name="sprintStartedAt">When the latest sprint started, in simulation seconds, or null.</param>
    /// <param name="sprintPressed">Whether the sprint control was pressed this tick.</param>
    /// <returns>Whether sprinting is allowed.</returns>
    public static bool AllowsSprint(HeldItemState state, double? sprintStartedAt, bool sprintPressed)
    {
        if (state.UseSprintBlockTimer > 0 || state.ForceWalk)
        {
            return false;
        }

        bool startedAfterReload = sprintStartedAt is { } started && (state.ReloadPressedAt is not { } pressed || started > pressed);
        return !state.ReloadBlocksSprint || !state.Reloading || startedAfterReload || sprintPressed;
    }

    /// <summary>Whether a sprint blocks the running reload: it has lasted long enough while the weapon reloads.</summary>
    /// <param name="sprintSeconds">Seconds the current sprint has lasted, 0 when not sprinting.</param>
    /// <param name="reloading">Whether the weapon reloads.</param>
    /// <returns>Whether the reload-block tag is set.</returns>
    public static bool BlocksReload(double sprintSeconds, bool reloading) =>
        reloading && sprintSeconds > HeldItemMovementParameters.ReloadBlockedAfterSprintSeconds;

    /// <summary>Simulation time units for a duration, rounded to the nearest unit.</summary>
    /// <param name="seconds">Duration in seconds.</param>
    /// <returns>The duration in simulation time units.</returns>
    private static int TimeUnits(double seconds) => (int)Math.Round(seconds * PawnBlackboard.TimePerSecond);

    private static bool ZoomRetriggerBlocked(HeldItemState previous, HeldItemMovementParameters item, double time) =>
        !previous.Zoom && item.ZoomRetriggerDelaySeconds > 0 && previous.ZoomEndedAt is { } ended && time - ended < item.ZoomRetriggerDelaySeconds;
}
