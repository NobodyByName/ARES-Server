using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Loot;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Enemies;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Players;

/// <summary>Why an operator command could not act on the player.</summary>
public enum AdminRefusal
{
    /// <summary>The command acted.</summary>
    None,

    /// <summary>The character is not possessed yet.</summary>
    NotPossessed,

    /// <summary>The player has extracted (an elevator left with them, or an operator extracted them).</summary>
    Departed,

    /// <summary>An earlier teleport has not reached the client yet.</summary>
    TeleportPending,

    /// <summary>The character is in a server-run interaction (a breach, search, open container or elevator hold).</summary>
    Busy,

    /// <summary>The destination is outside the range a frame can carry.</summary>
    OutOfRange,

    /// <summary>No container has that index.</summary>
    NoSuchContainer,

    /// <summary>The raid has no ids left for another container.</summary>
    NoContainerIds,

    /// <summary>The raid spawned as many ARCs as it can.</summary>
    NoArcIds,
}

/// <summary>
/// Operator commands acting on this player: moving the character and spawning containers and ARCs next to it. A teleport is a
/// pawn frame at the client's latest input frame that puts the character at rest at the destination; the client
/// takes every pawn frame as its character's state, rolls its prediction back to that frame and continues from
/// there. Until the client's reports come from the destination, no other frame goes out, so none moves it back.
/// </summary>
public sealed partial class PlayerChannels
{
    // How far past its prompt's depth, on a container's front, a teleport to it puts the character, and the height
    // above the container's base it starts from (it then settles on the floor).
    private const double LootTeleportDistance = 90;
    private const double LootTeleportLift = 50;

    // How far in front of the character a spawned container stands.
    private const double SpawnedLootDistance = 200;

    // How far in front of the character a spawned ARC hovers, and its height above the character's feet: about head
    // height, the top of the character's capsule.
    private const double SpawnedArcDistance = 350;
    private double SpawnedArcHeight => 2 * _raid.GameData.Map.PlayerStartCapsule.HalfHeight;

    // Seconds a teleport waits for the client's reports to come from its destination; then frames resume anyway.
    private const double TeleportConfirmSeconds = 3;

    private PendingTeleport? _teleport;

    /// <summary>The player's user id.</summary>
    public long UserId => _member.Id;

    /// <summary>The player's in-game name from the party profiles, or null when the party sent none.</summary>
    public string? DisplayName => _member.Profiles.FirstOrDefault(profile => profile.TenancyUserId.Value == (ulong)_member.Id)?.DisplayName;

    /// <summary>Where the character stands (at its feet): a pending teleport's destination, else the latest reported position.</summary>
    public Vector3D? CharacterPosition => _teleport?.Position ?? InputPosition;

    /// <summary>The character's heading in radians: a pending teleport's, else the latest reported one.</summary>
    public double? CharacterYaw => _teleport?.Yaw ?? InputYaw;

    /// <summary>Whether a teleport waits for the client's reports to come from its destination.</summary>
    public bool TeleportPending => _teleport is not null;

    /// <summary>Teleport frames sent.</summary>
    public int Teleports { get; private set; }

    /// <summary>The raid's loot containers, including those spawned during the raid.</summary>
    public IReadOnlyList<LootContainer> LootContainers => _raid.Loot.Containers;

    /// <summary>Moves the character to a place, at rest.</summary>
    /// <param name="position">Destination at the character's feet, in world units.</param>
    /// <param name="yaw">Heading in radians, or null to keep the current one.</param>
    /// <returns>Why it could not, or <see cref="AdminRefusal.None"/>.</returns>
    public AdminRefusal Teleport(Vector3D position, double? yaw = null)
    {
        if (Phase != DeliveryPhase.Possessed)
        {
            return AdminRefusal.NotPossessed;
        }

        if (_departure.Departed)
        {
            return AdminRefusal.Departed;
        }

        if (_teleport is not null)
        {
            return AdminRefusal.TeleportPending;
        }

        if (_frames.InteractionFramesHeld(this, _raid.Clock()) || _loot.ContainerOpen)
        {
            return AdminRefusal.Busy;
        }

        if (!(Math.Abs(position.X) < PawnBlackboard.PositionRange && Math.Abs(position.Y) < PawnBlackboard.PositionRange
            && Math.Abs(position.Z) < PawnBlackboard.PositionRange) || yaw is { } heading && !double.IsFinite(heading))
        {
            return AdminRefusal.OutOfRange;
        }

        Vector3D from = InputPosition ?? _member.Spawn.Location;
        _teleport = new PendingTeleport(position, yaw ?? InputYaw ?? (_member.Spawn.Yaw * Math.PI / 180), from);
        return AdminRefusal.None;
    }

    /// <summary>Moves the character in front of a container, facing its prompt.</summary>
    /// <param name="index">Index in <see cref="LootContainers"/>.</param>
    /// <returns>Why it could not, or <see cref="AdminRefusal.None"/>.</returns>
    public AdminRefusal TeleportToLoot(int index)
    {
        if (index < 0 || index >= _raid.Loot.Containers.Count)
        {
            return AdminRefusal.NoSuchContainer;
        }

        LootContainer container = _raid.Loot.Containers[index];
        // Stand on the container's front (its +X; the prompt is often on a side), past the prompt's depth, facing the
        // prompt so it stays within reach. Only the yaw turns it: with its pitch, a tilted container (a machine door)
        // aims the teleport into geometry.
        double depth = Math.Max(container.Class.PromptLocation.X, 0) + LootTeleportDistance;
        Vector3D front = new Rotator(0, container.Rotation.Yaw, 0).Rotate(new Vector3D(depth, 0, 0));
        Vector3D destination = new(container.Location.X + front.X, container.Location.Y + front.Y, container.Location.Z + LootTeleportLift);
        Vector3D prompt = container.Prompt;
        return Teleport(destination, Math.Atan2(prompt.Y - destination.Y, prompt.X - destination.X));
    }

    /// <summary>Spawns a container in front of the character, its prompt turned towards it.</summary>
    /// <param name="type">The container's class.</param>
    /// <param name="container">The spawned container.</param>
    /// <returns>Why it could not, or <see cref="AdminRefusal.None"/>.</returns>
    public AdminRefusal SpawnLoot(LootContainerClass type, out LootContainer? container)
    {
        ArgumentNullException.ThrowIfNull(type);
        container = null;
        if (Phase != DeliveryPhase.Possessed)
        {
            return AdminRefusal.NotPossessed;
        }

        Vector3D feet = CharacterPosition ?? _member.Spawn.Location;
        double yaw = CharacterYaw ?? (_member.Spawn.Yaw * Math.PI / 180);
        Vector3D location = new(Math.Round(feet.X + (SpawnedLootDistance * Math.Cos(yaw))), Math.Round(feet.Y + (SpawnedLootDistance * Math.Sin(yaw))),
            Math.Round(feet.Z));

        // The prompt lies at its class's offset from the container; turn that offset back towards the character.
        double promptHeading = Math.Atan2(type.PromptLocation.Y, type.PromptLocation.X) * 180 / Math.PI;
        double containerYaw = (yaw * 180 / Math.PI) + 180 - (Math.Abs(type.PromptLocation.X) + Math.Abs(type.PromptLocation.Y) < 1 ? 0 : promptHeading);
        containerYaw = ((containerYaw % 360) + 360) % 360;
        container = _raid.Loot.Spawn(type, location, new Rotator(0, Math.Round(containerYaw, 1), 0));
        return container is null ? AdminRefusal.NoContainerIds : AdminRefusal.None;
    }

    /// <summary>Spawns an ARC in front of the character at about head height, facing it.</summary>
    /// <param name="type">The ARC.</param>
    /// <param name="arc">The spawned ARC.</param>
    /// <returns>Why it could not, or <see cref="AdminRefusal.None"/>.</returns>
    public AdminRefusal SpawnArc(ArcClass type, out WorldArc? arc)
    {
        ArgumentNullException.ThrowIfNull(type);
        arc = null;
        if (Phase != DeliveryPhase.Possessed)
        {
            return AdminRefusal.NotPossessed;
        }

        Vector3D feet = CharacterPosition ?? _member.Spawn.Location;
        double yaw = CharacterYaw ?? (_member.Spawn.Yaw * Math.PI / 180);
        Vector3D location = new(Math.Round(feet.X + (SpawnedArcDistance * Math.Cos(yaw))), Math.Round(feet.Y + (SpawnedArcDistance * Math.Sin(yaw))),
            Math.Round(feet.Z + SpawnedArcHeight));
        double facing = (((yaw * 180 / Math.PI) + 180) % 360 + 360) % 360;
        arc = _raid.Arcs.Spawn(type, location, new Rotator(0, Math.Round(facing, 1), 0));
        return arc is null ? AdminRefusal.NoArcIds : AdminRefusal.None;
    }

    /// <summary>
    /// Extracts the player on the spot: at the next tick their round ends as if an elevator had left with them inside,
    /// so they keep their whole loadout.
    /// </summary>
    /// <returns>Why it could not, or <see cref="AdminRefusal.None"/>.</returns>
    public AdminRefusal Extract()
    {
        if (Phase != DeliveryPhase.Possessed)
        {
            return AdminRefusal.NotPossessed;
        }

        return _departure.Request() ? AdminRefusal.None : AdminRefusal.Departed;
    }

    /// <summary>Despawns every ARC of the raid.</summary>
    /// <returns>How many ARCs were despawned.</returns>
    public int DespawnArcs() => _raid.Arcs.DespawnAll();

    // Sends a pending teleport's frame, then holds the other frames until the client reports from the destination
    // (two reports in a row nearer to it than to where the character was) or the wait runs out. Returns whether the
    // character's ordinary frames must wait.
    private bool ServiceTeleport(ReliableSender sender, double now)
    {
        if (_teleport is not { } teleport)
        {
            return false;
        }

        if (teleport.Frame == 0)
        {
            // The character's own setup frames (its first frame and held-item links) go first.
            if (_frames.Frame == 0 || (_player.HeldItems.Count > 0 && !_frames.ItemsLinked))
            {
                return false;
            }

            if (InputFrame is not { } latest || latest <= _frames.Frame || latest > PawnBlackboardFrames.LastTick || _frames.Pending(sender)
                || _member.Weapons.HoldsFrames(latest))
            {
                return true;
            }

            _frames.SendRest(this, sender, latest, teleport.Position, teleport.Yaw, now);
            Teleports++;
            _teleport = teleport with { Frame = latest, SentAt = now };
            return true;
        }

        if (now - teleport.SentAt < TeleportConfirmSeconds
            && (InputFrame is not { } reported || reported <= teleport.Frame + 1 || !Arrived(reported) || !Arrived(reported - 1)))
        {
            return true;
        }

        // The client reports from the destination: the character's simulation starts there again, as the frame put it.
        // The first tick that read a report from there runs again as the restart's first.
        if (InputFrame is { } arrived && arrived > teleport.Frame + 1 && Arrived(arrived) && Arrived(arrived - 1) && _input.Position(arrived - 1) is { } there)
        {
            if (_frames.Movement is { } movement)
            {
                movement.Restart(Math.Max(teleport.Frame, arrived - 3), there, teleport.Yaw);
                while (movement.Frame + 1 < arrived)
                {
                    movement.Advance(_input, _member.Weapons.Armed);
                }
            }
        }

        _teleport = null;
        return false;

        bool Arrived(uint frame) => _input.Position(frame) is { } position
            && DistanceSquared(position, teleport.Position) < DistanceSquared(position, teleport.From);
    }

    private static double DistanceSquared(Vector3D a, Vector3D b) =>
        ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)) + ((a.Z - b.Z) * (a.Z - b.Z));

    // An operator teleport: destination, heading in radians and the place the character left; once its frame is sent,
    // the frame's input frame and when it was sent.
    private readonly record struct PendingTeleport(Vector3D Position, double Yaw, Vector3D From, uint Frame = 0, double SentAt = 0);
}
