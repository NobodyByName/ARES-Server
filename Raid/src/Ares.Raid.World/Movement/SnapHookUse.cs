using System.Numerics;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Movement;

/// <summary>The Snap Hook controls of one tick, as the item reads them.</summary>
/// <param name="Ready">Whether the Snap Hook is in hand and not being put away.</param>
/// <param name="Throw">Whether the primary use was pressed with this frame, allowed and not blocked.</param>
/// <param name="FireDown">Whether the primary use is held.</param>
/// <param name="SecondaryDown">Whether the secondary use is held.</param>
/// <param name="SecondaryPressed">Whether the secondary use went down with this frame.</param>
/// <param name="Yaw">Control heading in radians.</param>
/// <param name="Pitch">Control pitch in radians, positive up (<see cref="InputHistory.Pitch"/>).</param>
/// <param name="Camera">Where the camera is relative to the feet, in the control heading's frame (<see cref="InputHistory.CameraOffset"/>).</param>
public readonly record struct SnapHookControls(bool Ready, bool Throw, bool FireDown, bool SecondaryDown, bool SecondaryPressed, double Yaw, double Pitch,
    Vector3D? Camera = null)
{
    /// <summary>Reads the controls from a frame's weapon input.</summary>
    /// <param name="ready">Whether the Snap Hook is in hand and not being put away.</param>
    /// <param name="weapon">The frame's weapon controls, if it has them.</param>
    /// <param name="yaw">Control heading in radians.</param>
    /// <param name="pitch">Control pitch in radians, positive up.</param>
    /// <param name="camera">Where the camera is relative to the feet, if known.</param>
    /// <returns>The controls.</returns>
    public static SnapHookControls From(bool ready, WeaponInput? weapon, double yaw, double pitch, Vector3D? camera = null) =>
        new(ready, weapon is { FirePressed: true, AllowPrimary: true, FireBlocked: false }, weapon?.FireDown ?? false, weapon?.SecondaryDown ?? false,
            weapon?.SecondaryPressed ?? false, yaw, pitch, camera);
}

/// <summary>
/// The Snap Hook's use in the character state: the hook it threw, the move target that pulls its owner (kept after
/// the pull with its removal time), the pull itself and what the item node remembers of the latest throw. Times are
/// simulation time in <see cref="PawnBlackboard.TimePerSecond"/> units: tick T runs at (T - 1) times
/// <see cref="PawnBlackboard.TimePerTick"/>.
/// </summary>
/// <param name="Hook">The hook (stowed when none is out).</param>
/// <param name="Target">The latest move target, if a hook ever pulled.</param>
/// <param name="Pull">The pull under way, if any.</param>
/// <param name="LastThrownAt">Simulation time of the latest throw, if any.</param>
/// <param name="PreviousThrownAt">Simulation time of the throw before it, if any.</param>
/// <param name="Released">Whether the primary use was released since the latest throw.</param>
/// <param name="LastPoint">Where the latest throw's hook went.</param>
/// <param name="Aiming">Whether the character aims the item: the secondary use pressed and held while no hook is out.</param>
public readonly record struct SnapHookUse(
    SnapHookHook Hook,
    SnapHookTarget? Target = null,
    SnapHookState? Pull = null,
    int? LastThrownAt = null,
    int? PreviousThrownAt = null,
    bool Released = false,
    Vector3D LastPoint = default,
    bool Aiming = false)
{
    /// <summary>No use yet.</summary>
    public static SnapHookUse None => new(SnapHookHook.Stowed);

    /// <summary>Whether a hook is out or a pull runs: the client's own state, which no pawn frame carries.</summary>
    public bool Busy => Hook.Thrown || Pull is not null;

    /// <summary>Simulation time of a tick.</summary>
    /// <param name="frame">Input frame.</param>
    /// <returns>Its time units.</returns>
    public static int Time(uint frame) => checked((int)((frame - 1L) * PawnBlackboard.TimePerTick));

    /// <summary>
    /// One tick of the item (<c>USnaphookItem</c>), before the movement node runs: a held hook is called back by the
    /// secondary use (which removes its move target), a thrown one flies, attaches or clears; then, with no hook out, a
    /// primary press throws a new one along the aim.
    /// </summary>
    /// <param name="controls">The tick's controls.</param>
    /// <param name="frame">Input frame of the tick.</param>
    /// <param name="feet">The character's feet before the tick.</param>
    /// <param name="previousFlags">Graph state flags of the previous tick.</param>
    /// <param name="world">The level's collision, or null when the hook hits nothing.</param>
    /// <returns>The use after the tick.</returns>
    public SnapHookUse StepItem(SnapHookControls controls, uint frame, Vector3D feet, ulong previousFlags, CollisionWorld? world)
    {
        int time = Time(frame);
        SnapHookUse next = this with
        {
            Aiming = controls is { Ready: true, SecondaryDown: true } && !Hook.Thrown && (Aiming || controls.SecondaryPressed),
            Released = Released || !controls.FireDown,
        };
        if (Hook.Thrown)
        {
            Vector3D start = feet with { Z = feet.Z + DamPlayerStarts.Capsule.HalfHeight + SnapHookParameters.ThrowHeight };
            SnapHookHookStep step = controls.SecondaryPressed
                ? SnapHookThrow.Cancel(Hook)
                : SnapHookThrow.Tick(Hook, time, start, PawnBlackboard.TickSeconds, previousFlags, Target, SnapHookPullEnd.World);
            SnapHookTarget? target = step.Target ?? Target;
            if (step.Event == SnapHookHookEvent.Cancelled && target is { IsActive: true } removed)
            {
                target = removed.Invalidate(time);
            }

            return next with { Hook = step.Hook, Target = target, LastPoint = step.Hook.Thrown ? step.Hook.AttachPoint : LastPoint };
        }

        if (!controls.Ready || !controls.Throw)
        {
            return next;
        }

        SnapHookHook thrown = SnapHookAim.Throw(time, feet, controls.Yaw, controls.Pitch, controls.Camera, world);
        return next with
        {
            Hook = thrown,
            LastThrownAt = time,
            PreviousThrownAt = LastThrownAt,
            Released = false,
            LastPoint = thrown.AttachPoint,
            Aiming = false,
        };
    }

    /// <summary>
    /// The item node's hook fields (<see cref="QuickItemProtocol"/>): the target, the throw times, the state bits,
    /// what the item was last used for and the surface. A node keeps only what happened since it was laid out.
    /// </summary>
    /// <param name="nodeFrom">Input frame the item node was laid out at.</param>
    /// <returns>The field values.</returns>
    public IReadOnlyList<PendulumIntegerValue> ItemNodeValues(uint nodeFrom)
    {
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[QuickItemProtocol.ItemNodeDescriptor].Fields;
        ulong never = ((PendulumScalarField)fields[QuickItemProtocol.ThrowTimeField]).Default;
        int? since = LastThrownAt is { } last && last >= Time(nodeFrom) ? last : null;
        bool thrownSince = since is not null;
        List<PendulumIntegerValue> values = [];
        if (since is { } latest)
        {
            double[] point = [LastPoint.X, LastPoint.Y, LastPoint.Z];
            values.AddRange(point.Select((world, axis) => new PendulumIntegerValue(QuickItemProtocol.HookLocationXField + axis,
                Position(world, (PendulumScalarField)fields[QuickItemProtocol.HookLocationXField + axis]))));

            values.Add(new PendulumIntegerValue(QuickItemProtocol.LastThrowTimeField, PawnSlideState.TimeBias + (ulong)latest));
        }

        values.Add(new PendulumIntegerValue(QuickItemProtocol.ThrowTimeField, Hook.Thrown ? PawnSlideState.TimeBias + (ulong)Hook.ThrownAt : never));
        ulong bits = Hook.Thrown
            ? Bit(QuickItemProtocol.HookOutBit) | (Hook.Attached ? Bit(QuickItemProtocol.HookAttachedBit) : Bit(QuickItemProtocol.HookFlyingBit))
                | (Hook.HasTarget ? Bit(QuickItemProtocol.HookTargetBit) : 0) | (Hook.PullsPlayer ? Bit(QuickItemProtocol.HookPullsBit) : 0)
            : Hook.Cancelled && thrownSince ? Bit(QuickItemProtocol.HookCancelledBit) : 0;
        values.Add(new PendulumIntegerValue(QuickItemProtocol.HookStateField, bits | (Aiming ? Bit(QuickItemProtocol.AimingBit) : 0)));
        values.Add(new PendulumIntegerValue(QuickItemProtocol.ItemUseField,
            Aiming ? QuickItemProtocol.ItemUseAiming : thrownSince ? QuickItemProtocol.ItemUseThrown : 0UL));
        // A new node counts as fresh until the primary use is released after its first throw.
        bool fresh = !thrownSince || ((PreviousThrownAt is not { } previous || previous < Time(nodeFrom)) && !Released);
        values.Add(new PendulumIntegerValue(QuickItemProtocol.UnknownFreshField, fresh ? 1UL : 0));
        values.Add(new PendulumIntegerValue(QuickItemProtocol.SurfaceField, Hook is { Thrown: true, HasTarget: true } ? (ulong)Hook.Surface : 0));
        return values;
    }

    /// <summary>
    /// The move target and the pull node as other clients see them, in the slots after the given variables: the move
    /// target from the first attach on (it keeps its removal time after a pull), the pull node while the pull runs and
    /// pointing at the move target's slot. Other clients show the pulled pose and its heading from these.
    /// </summary>
    /// <param name="tick">The frame's tick.</param>
    /// <param name="variables">The other variables beyond the initial state (weapons and item nodes).</param>
    /// <returns>The variables, none before the first attach.</returns>
    public IReadOnlyList<PendulumVariable> ObservedNodes(uint tick, IReadOnlyList<PendulumVariable> variables)
    {
        if (Target is not { } target)
        {
            return [];
        }

        NodeSlots after = NodeSlots.After(PawnBlackboard.InitialState.Concat(variables), []);
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[QuickItemProtocol.MoveTargetDescriptor].Fields;
        double[] point = [target.Position.X, target.Position.Y, target.Position.Z];
        List<PendulumValue> values =
        [
            .. point.Select((world, axis) => new PendulumIntegerValue(QuickItemProtocol.MoveTargetPositionXField + axis,
                Position(world, (PendulumScalarField)fields[QuickItemProtocol.MoveTargetPositionXField + axis]))),
            new PendulumIntegerValue(QuickItemProtocol.MoveTargetCreatedField, Biased(target.CreatedAt)),
            new PendulumIntegerValue(QuickItemProtocol.MoveTargetRemovedField, Biased(target.RemovedAt)),
        ];
        List<PendulumVariable> result =
            [new(after.FirstSlot, after.FirstPosition, QuickItemProtocol.MoveTargetPages, QuickItemProtocol.MoveTargetDescriptor, values)];
        if (Pull is not { } pull)
        {
            return result;
        }

        PendulumQuaternionField direction = (PendulumQuaternionField)PendulumRegistry.Descriptors[QuickItemProtocol.PullNodeDescriptor].Fields[QuickItemProtocol.PullNodeDirectionField];
        result.Add(new PendulumVariable(after.FirstSlot + 1, after.FirstPosition + QuickItemProtocol.MoveTargetPages, QuickItemProtocol.PullNodePages,
            QuickItemProtocol.PullNodeDescriptor,
            [
                new PendulumQuaternionValue(QuickItemProtocol.PullNodeDirectionField,
                    PendulumRotations.FromDirection(pull.PullDirection.X, pull.PullDirection.Y, pull.PullDirection.Z, direction)),
                new PendulumIntegerValue(QuickItemProtocol.PullNodeDataField, Biased(QuickItemProtocol.PullNodeDataId)),
                // Like the other movement nodes, the pull starts at the end of its first tick.
                new PendulumIntegerValue(QuickItemProtocol.PullNodeStartTimeField, Biased(Time(tick - (uint)pull.Ticks) + PawnBlackboard.TimePerTick)),
                new PendulumIntegerValue(QuickItemProtocol.PullNodeMoveTargetField, (ulong)after.FirstSlot),
            ]));
        return result;
    }

    private static ulong Bit(int bit) => 1UL << bit;

    // A signed time (or -1 for none) in a biased time field.
    private static ulong Biased(long value) => (ulong)((long)PawnSlideState.TimeBias + value);

    // A world coordinate in a scalar field spanning the position range.
    private static ulong Position(double world, PendulumScalarField field) =>
        (ulong)Math.Clamp(Math.Round((world + PawnBlackboard.PositionRange) * (field.Maximum - 1.0) / (2 * PawnBlackboard.PositionRange)), 0, field.Maximum - 1.0);
}

/// <summary>
/// Where a Snap Hook throw goes. The crosshair trace runs along the view from the camera ray's point level with the
/// character's capsule centre and finds the aim point; the throw's trace then runs from the shooting location (beside and
/// above the capsule centre) towards it for the hook's range. Both traces hit complex collision.
/// </summary>
public static class SnapHookAim
{
    /// <summary>Throws the hook from the character's feet along its control rotation.</summary>
    /// <param name="time">Simulation time of the throw.</param>
    /// <param name="feet">The character's feet.</param>
    /// <param name="yaw">Control heading in radians.</param>
    /// <param name="pitch">Control pitch in radians, positive up.</param>
    /// <param name="camera">Where the camera is relative to the feet in the control heading's frame; unknown, the view starts at the capsule centre.</param>
    /// <param name="world">The level's collision, or null when nothing is hit.</param>
    /// <returns>The thrown hook.</returns>
    public static SnapHookHook Throw(int time, Vector3D feet, double yaw, double pitch, Vector3D? camera, CollisionWorld? world)
    {
        (double sinYaw, double cosYaw) = Math.SinCos(yaw);
        (double sinPitch, double cosPitch) = Math.SinCos(pitch);
        Vector3D forward = new(cosPitch * cosYaw, cosPitch * sinYaw, sinPitch);
        Vector3D centre = feet with { Z = feet.Z + DamPlayerStarts.Capsule.HalfHeight };
        Vector3D local = camera ?? new Vector3D(0, 0, DamPlayerStarts.Capsule.HalfHeight);
        Vector3D eye = Add(feet, Heading(local, sinYaw, cosYaw));
        Vector3D origin = Add(eye, Scale(forward, Math.Max(0, Dot(Subtract(centre, eye), forward))));
        Vector3D aim = Hit(world, origin, forward, SnapHookParameters.AimTraceLength, SnapHookParameters.AimTraceChannel)?.Point
            ?? Add(origin, Scale(forward, SnapHookParameters.AimTraceLength));
        Vector3D shoot = Add(centre, Heading(new Vector3D(0, SnapHookParameters.ShootRight, SnapHookParameters.ShootHeight), sinYaw, cosYaw));
        return SnapHookThrow.Throw(time, shoot, aim, (from, to) =>
        {
            Vector3D along = Subtract(to, from);
            double length = Math.Sqrt(Dot(along, along));
            return length > 0 && Hit(world, from, Scale(along, 1 / length), length, SnapHookParameters.ThrowTraceChannel) is { } hit
                ? new GrappleHit(hit.Point, ToDouble(hit.Normal), hit.Surface)
                : null;
        });
    }

    private static CollisionHit? Hit(CollisionWorld? world, Vector3D from, Vector3D direction, double length, CollisionChannel channel) =>
        world?.Raycast(from, new Vector3((float)direction.X, (float)direction.Y, (float)direction.Z), length, channel, CollisionComplexity.Complex);

    // A vector given along the heading (forward, right, up) in world axes.
    private static Vector3D Heading(Vector3D local, double sinYaw, double cosYaw) =>
        new((local.X * cosYaw) - (local.Y * sinYaw), (local.X * sinYaw) + (local.Y * cosYaw), local.Z);

    private static Vector3D ToDouble(Vector3 normal) => new(normal.X, normal.Y, normal.Z);

    private static Vector3D Add(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    private static Vector3D Subtract(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    private static Vector3D Scale(Vector3D a, double factor) => new(a.X * factor, a.Y * factor, a.Z * factor);

    private static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
}
