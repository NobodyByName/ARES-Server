using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;
using Ares.Structs.Net.GameplayTags;

namespace Ares.Raid.World;

/// <summary>Where a player spawns: the start, its group slot, and the world location and rotation.</summary>
/// <param name="Start">Player start actor.</param>
/// <param name="Slot">Spawn point of a group start (null for a single start).</param>
/// <param name="Location">Capsule center.</param>
/// <param name="Rotation">Rotation.</param>
public sealed record SpawnPlacement(string Start, int? Slot, Vector3D Location, Quaternion Rotation)
{
    /// <summary>Authored horizontal facing, in degrees.</summary>
    public double Yaw => Math.Atan2(2 * ((Rotation.W * Rotation.Z) + (Rotation.X * Rotation.Y)),
        1 - (2 * ((Rotation.Y * Rotation.Y) + (Rotation.Z * Rotation.Z)))) * 180 / Math.PI;
}

/// <summary>The Dam player starts a round may use, and where their spawn points are in the world.</summary>
public static class Spawns
{
    /// <summary>Group starts the Salvage game mode may use in a round.</summary>
    /// <param name="round">The round.</param>
    /// <param name="pve">Whether the round is player-versus-environment only.</param>
    /// <returns>The starts, in map order.</returns>
    public static IReadOnlyList<PlayerStart> Eligible(RoundConfiguration round, bool pve = false)
    {
        ArgumentNullException.ThrowIfNull(round);
        GameplayTagQuery query = pve ? round.ContentQueryPvE : round.ContentQuery;
        return
        [
            .. DamPlayerStarts.All.Where(start => start.ClassName == PlayerStartClassNames.Group
                && start.Tags.Contains(DamMap.GameModeTag)
                && (!round.FilterPlayerStarts || query.Matches(start.Tags))),
        ];
    }

    /// <summary>A spawn point of a group start: its offset rotated by the start's yaw, lifted by the capsule's half height.</summary>
    /// <param name="start">A group start.</param>
    /// <param name="slot">Spawn point index.</param>
    /// <returns>The placement.</returns>
    public static SpawnPlacement Place(PlayerStart start, int slot)
    {
        ArgumentNullException.ThrowIfNull(start);
        if (start.ClassName != PlayerStartClassNames.Group || slot < 0 || slot >= start.SpawnPoints.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "Not a spawn point of a group start.");
        }

        double half = start.RootRotation.Yaw * Math.PI / 360;
        Quaternion root = new(0, 0, Math.Sin(half), Math.Cos(half));
        Transform point = start.SpawnPoints[slot];
        Quaternion rotation = Normalize(Multiply(root, Normalize(point.Rotation)));
        Vector3D offset = Rotate(root, point.Translation);
        Vector3D up = Rotate(rotation, new Vector3D(0, 0, 1));
        double lift = DamPlayerStarts.Capsule.HalfHeight;
        Vector3D location = new(
            start.RootLocation.X + offset.X + (lift * up.X),
            start.RootLocation.Y + offset.Y + (lift * up.Y),
            start.RootLocation.Z + offset.Z + (lift * up.Z));
        return new SpawnPlacement(start.Actor, slot, location, rotation);
    }

    private static Vector3D Rotate(Quaternion q, Vector3D v)
    {
        double tx = 2 * ((q.Y * v.Z) - (q.Z * v.Y));
        double ty = 2 * ((q.Z * v.X) - (q.X * v.Z));
        double tz = 2 * ((q.X * v.Y) - (q.Y * v.X));
        return new Vector3D(
            v.X + (q.W * tx) + (q.Y * tz) - (q.Z * ty),
            v.Y + (q.W * ty) + (q.Z * tx) - (q.X * tz),
            v.Z + (q.W * tz) + (q.X * ty) - (q.Y * tx));
    }

    private static Quaternion Multiply(Quaternion a, Quaternion b) => new(
        (a.W * b.X) + (a.X * b.W) + (a.Y * b.Z) - (a.Z * b.Y),
        (a.W * b.Y) - (a.X * b.Z) + (a.Y * b.W) + (a.Z * b.X),
        (a.W * b.Z) + (a.X * b.Y) - (a.Y * b.X) + (a.Z * b.W),
        (a.W * b.W) - (a.X * b.X) - (a.Y * b.Y) - (a.Z * b.Z));

    private static Quaternion Normalize(Quaternion q)
    {
        double norm = Math.Sqrt((q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z) + (q.W * q.W));
        return Math.Abs(norm - 1) <= 1e-5 ? new Quaternion(q.X / norm, q.Y / norm, q.Z / norm, q.W / norm) : throw new ArgumentException("Rotation is not normalized.", nameof(q));
    }
}
