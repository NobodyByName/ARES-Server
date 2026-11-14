using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// The snap hook item's hook (the item node's own state). Times are simulation time in
/// <see cref="PawnBlackboard.TimePerSecond"/> units.
/// </summary>
/// <param name="Thrown">Whether a hook is out.</param>
/// <param name="Attached">Whether the hook holds on to something (set when it arrives, even when it then lets go at once).</param>
/// <param name="HasTarget">Whether the throw's trace hit something.</param>
/// <param name="Cancelled">Whether an attached hook was cancelled.</param>
/// <param name="PullsPlayer">Whether the hook pulls its owner (a move target exists).</param>
/// <param name="ThrownAt">Simulation time of the throw, or -1.</param>
/// <param name="AttachPoint">World point the hook flies to: the hit, or the end of a missed throw.</param>
/// <param name="Surface">Surface type of the hit.</param>
/// <param name="OnShield">Whether the hit is an ARC shield, which the hook lets go of on arrival.</param>
public readonly record struct SnapHookHook(
    bool Thrown = false,
    bool Attached = false,
    bool HasTarget = false,
    bool Cancelled = false,
    bool PullsPlayer = false,
    int ThrownAt = -1,
    Vector3D AttachPoint = default,
    PhysicalSurfaceType Surface = PhysicalSurfaceType.Default,
    bool OnShield = false)
{
    /// <summary>No hook out.</summary>
    public static SnapHookHook Stowed => new();
}

/// <summary>One end of the hook's pull, as the pull shares its strength between the two ends.</summary>
/// <param name="CanBePulled">Whether the end can move: a character or a physics-simulating body.</param>
/// <param name="IsCharacter">Whether the end is a character.</param>
/// <param name="Mass">Mass of the end.</param>
public readonly record struct SnapHookPullEnd(bool CanBePulled, bool IsCharacter, double Mass)
{
    /// <summary>The level itself: nothing that moves.</summary>
    public static SnapHookPullEnd World => new(false, false, SnapHookParameters.DefaultMass);

    /// <summary>A character.</summary>
    public static SnapHookPullEnd Character => new(true, true, SnapHookParameters.DefaultMass);
}

/// <summary>What a hook tick did.</summary>
public enum SnapHookHookEvent : byte
{
    /// <summary>Nothing changed state.</summary>
    None = 0,

    /// <summary>The hook arrived and holds on.</summary>
    Attached = 1,

    /// <summary>The hook is gone: back from a miss, let go of a shield, or its pull ended.</summary>
    Cleared = 2,

    /// <summary>The hook was cancelled.</summary>
    Cancelled = 3,
}

/// <summary>Result of one hook tick.</summary>
/// <param name="Hook">The hook after the tick.</param>
/// <param name="Event">What happened.</param>
/// <param name="Target">The move target the hook created this tick, if any.</param>
/// <param name="Refund">Gadget power given back (a hook that attached to nothing).</param>
public readonly record struct SnapHookHookStep(SnapHookHook Hook, SnapHookHookEvent Event, SnapHookTarget? Target, double Refund);

/// <summary>
/// The snap hook item (<c>USnaphookItem</c>): its gadget power, the throw (a line trace along the aim, the length of
/// the hook's range), the hook's flight out and, after a miss, back, and what it does on arrival: hold on and, when
/// what it holds is heavy enough, create the move target that pulls its owner (<see cref="SnapHookMovement"/>).
/// </summary>
public static class SnapHookThrow
{
    /// <summary>Seconds as simulation time units (the engine's tick-time conversion, rounded).</summary>
    /// <param name="seconds">Seconds.</param>
    /// <returns>The time units.</returns>
    public static int TimeUnits(double seconds) => (int)Math.Round(seconds * PawnBlackboard.TimePerSecond);

    /// <summary>Whether the item can throw: no hook out and enough gadget power.</summary>
    /// <param name="hook">The hook.</param>
    /// <param name="power">Gadget power.</param>
    /// <param name="infinitePower">Whether the character has infinite power (<c>Attribute.Power.Infinite</c>).</param>
    /// <returns>Whether a throw starts.</returns>
    public static bool CanThrow(SnapHookHook hook, double power, bool infinitePower = false) =>
        !hook.Thrown && (infinitePower || power >= SnapHookItem.ActivationCost);

    /// <summary>Gadget power after a throw pays for itself.</summary>
    /// <param name="power">Gadget power before.</param>
    /// <param name="infinitePower">Whether the character has infinite power.</param>
    /// <returns>Gadget power after.</returns>
    public static double PowerAfterThrow(double power, bool infinitePower = false) =>
        infinitePower ? power : Math.Max(power - SnapHookItem.ActivationCost, 0);

    /// <summary>Gadget power after a tick's recharge.</summary>
    /// <param name="power">Gadget power before.</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="blocked">Whether recharging is blocked this tick.</param>
    /// <returns>Gadget power after.</returns>
    public static double RechargePower(double power, double seconds, bool blocked = false) =>
        !blocked && power < SnapHookParameters.PowerMaximum ? power + (SnapHookParameters.PowerRechargePerSecond * seconds) : power;

    /// <summary>
    /// Throws the hook: a trace from the shooting location towards the aim target, as long as the hook's range,
    /// decides where it flies; without a hit it flies to the end of the trace.
    /// </summary>
    /// <param name="time">Simulation time of the throw.</param>
    /// <param name="shootLocation">The character's shooting location.</param>
    /// <param name="aimTarget">The point the character aims at.</param>
    /// <param name="trace">The throw's world trace, ignoring the owner and its item actor.</param>
    /// <returns>The thrown hook.</returns>
    public static SnapHookHook Throw(int time, Vector3D shootLocation, Vector3D aimTarget, GrappleTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        Vector3D direction = Vectors.Normal(Vectors.Subtract(aimTarget, shootLocation));
        Vector3D end = Vectors.Add(shootLocation, Vectors.Scale(direction, SnapHookItem.Range));
        GrappleHit? hit = trace(shootLocation, end);
        return hit is { } found
            ? new SnapHookHook(Thrown: true, HasTarget: true, ThrownAt: time, AttachPoint: found.Point, Surface: found.Surface, OnShield: found.IsArcShield)
            : new SnapHookHook(Thrown: true, ThrownAt: time, AttachPoint: end);
    }

    /// <summary>
    /// Where the flying hook is: from the throw start towards its attach point at the range per extend duration;
    /// after a miss's extend duration, back from that point towards the throw start at the range per pullback duration.
    /// </summary>
    /// <param name="hook">The hook.</param>
    /// <param name="throwStart">The throw start now.</param>
    /// <param name="elapsed">Seconds since the throw.</param>
    /// <returns>The hook's world position.</returns>
    public static Vector3D HookLocation(SnapHookHook hook, Vector3D throwStart, double elapsed)
    {
        if (hook.Attached)
        {
            return hook.AttachPoint;
        }

        if (hook.HasTarget || elapsed < SnapHookParameters.ExtendSeconds)
        {
            const double Speed = SnapHookItem.Range / SnapHookParameters.ExtendSeconds;
            return Vectors.Add(throwStart, Vectors.ClampToMaxSize(Vectors.Subtract(hook.AttachPoint, throwStart), Speed * elapsed));
        }

        const double Back = SnapHookItem.Range / SnapHookParameters.PullbackSeconds;
        return Vectors.Add(hook.AttachPoint,
            Vectors.ClampToMaxSize(Vectors.Subtract(throwStart, hook.AttachPoint), Back * (elapsed - SnapHookParameters.ExtendSeconds)));
    }

    /// <summary>
    /// Speed the pull moves the hook's owner with: the pull strength scaled by the owner's share of the pull. The
    /// owner does not move when what it hooked can be pulled and is lighter than the threshold; it gets the full pull
    /// against the level or another character.
    /// </summary>
    /// <param name="hooked">What the hook holds.</param>
    /// <returns>The pull speed; zero when the hooked object is pulled instead.</returns>
    public static double PullSpeed(SnapHookPullEnd hooked)
    {
        SnapHookPullEnd owner = SnapHookPullEnd.Character;
        const double Threshold = SnapHookParameters.PlayerMovementWeightThreshold;
        double share;
        if (!hooked.CanBePulled)
        {
            share = 1;
        }
        else if (owner.IsCharacter && hooked is { IsCharacter: false, Mass: < Threshold })
        {
            share = 0;
        }
        else if (hooked.IsCharacter && owner.Mass < Threshold)
        {
            share = 1;
        }
        else
        {
            share = 1 - (owner.Mass / Math.Max(owner.Mass + hooked.Mass, SnapHookParameters.MinimumCombinedMass));
        }

        return share * SnapHookParameters.PullStrength * SnapHookParameters.PlayerForceMultiplier;
    }

    /// <summary>
    /// Advances a thrown hook by one tick (<c>TickHook</c>): cancelled by a heavy landing; while attached, cleared
    /// once the pull it created has ended; while flying to a hit, attached on arrival or when the extend duration is
    /// over; a miss's end sinks while flying out and the hook is cleared once it is back.
    /// </summary>
    /// <param name="hook">The hook.</param>
    /// <param name="time">Simulation time of the tick.</param>
    /// <param name="throwStart">The throw start now.</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="previousFlags">Graph state flags of the previous tick.</param>
    /// <param name="target">The owner's move target, if any.</param>
    /// <param name="hooked">What the hook holds, for the pull on arrival.</param>
    /// <returns>The hook after the tick and what happened.</returns>
    public static SnapHookHookStep Tick(SnapHookHook hook, int time, Vector3D throwStart, double seconds, ulong previousFlags, SnapHookTarget? target,
        SnapHookPullEnd hooked)
    {
        if (!hook.Thrown)
        {
            return new SnapHookHookStep(hook, SnapHookHookEvent.None, null, 0);
        }

        if ((previousFlags & SnapHookParameters.CancellingFlags) != 0)
        {
            return Cancel(hook);
        }

        if (hook.Attached)
        {
            bool pullOver = hook.PullsPlayer && target is not { IsActive: true };
            return pullOver ? Clear(hook) : new SnapHookHookStep(hook, SnapHookHookEvent.None, null, 0);
        }

        double elapsed = (time - hook.ThrownAt) / (double)PawnBlackboard.TimePerSecond;
        if (hook.HasTarget)
        {
            double distance = Vectors.Length(Vectors.Subtract(hook.AttachPoint, HookLocation(hook, throwStart, elapsed)));
            return distance < SnapHookParameters.ReachedTargetDistance || elapsed >= SnapHookParameters.ExtendSeconds
                ? Attach(hook, time, hooked)
                : new SnapHookHookStep(hook, SnapHookHookEvent.None, null, 0);
        }

        if (elapsed >= SnapHookParameters.ExtendSeconds)
        {
            return elapsed >= SnapHookParameters.ExtendSeconds + SnapHookParameters.PullbackSeconds
                ? Clear(hook)
                : new SnapHookHookStep(hook, SnapHookHookEvent.None, null, 0);
        }

        double sink = -AirMovementParameters.FallAcceleration * SnapHookParameters.MissArcGravityScale * seconds;
        hook = hook with { AttachPoint = hook.AttachPoint with { Z = hook.AttachPoint.Z + sink } };
        return new SnapHookHookStep(hook, SnapHookHookEvent.None, null, 0);
    }

    /// <summary>Cancels the hook (secondary use, or a heavy landing): it is cleared, and marked cancelled if it was attached.</summary>
    /// <param name="hook">The hook.</param>
    /// <returns>The cleared hook.</returns>
    public static SnapHookHookStep Cancel(SnapHookHook hook)
    {
        SnapHookHookStep cleared = Clear(hook);
        return cleared with { Hook = cleared.Hook with { Cancelled = hook.Attached }, Event = SnapHookHookEvent.Cancelled };
    }

    // On arrival the hook holds on; a shield makes it let go at once, and only a pull that moves the owner creates a
    // move target, valid after the pull delay.
    private static SnapHookHookStep Attach(SnapHookHook hook, int time, SnapHookPullEnd hooked)
    {
        SnapHookHook attached = hook with { Attached = true };
        if (hook.OnShield)
        {
            return Clear(attached);
        }

        double speed = PullSpeed(hooked);
        if (speed <= 0)
        {
            return new SnapHookHookStep(attached, SnapHookHookEvent.Attached, null, 0);
        }

        SnapHookTarget target = new(hook.AttachPoint, time + TimeUnits(SnapHookParameters.PullDelaySeconds), speed, SnapHookParameters.AllowedWiggleSpeed);
        return new SnapHookHookStep(attached with { PullsPlayer = true }, SnapHookHookEvent.Attached, target, 0);
    }

    // A hook that never attached gives its power back.
    private static SnapHookHookStep Clear(SnapHookHook hook) =>
        new(SnapHookHook.Stowed, SnapHookHookEvent.Cleared, null, hook.Attached ? 0 : SnapHookItem.ActivationCost);
}

file static class Vectors
{
    public static Vector3D Add(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3D Subtract(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3D Scale(Vector3D a, double factor) => new(a.X * factor, a.Y * factor, a.Z * factor);

    public static double Length(Vector3D a) => Math.Sqrt((a.X * a.X) + (a.Y * a.Y) + (a.Z * a.Z));

    // The engine's safe normal: zero for (nearly) zero vectors.
    public static Vector3D Normal(Vector3D a)
    {
        double square = (a.X * a.X) + (a.Y * a.Y) + (a.Z * a.Z);
        return square > 1e-8 ? Scale(a, 1 / Math.Sqrt(square)) : default;
    }

    // The engine's clamp of a vector's length.
    public static Vector3D ClampToMaxSize(Vector3D a, double maximum)
    {
        if (maximum < 1e-4)
        {
            return default;
        }

        double square = (a.X * a.X) + (a.Y * a.Y) + (a.Z * a.Z);
        return square > maximum * maximum ? Scale(a, maximum / Math.Sqrt(square)) : a;
    }
}
