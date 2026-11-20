using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>How the item on a loose item socket is placed (the socket's <c>Socket.LootOptions.*</c> gameplay tags).</summary>
[Flags]
public enum LooseItemOptions
{
    /// <summary>No option: the item stays where the socket is.</summary>
    None = 0,

    /// <summary>The item simulates physics and comes to rest below the socket (<see cref="LooseItemTags.EnablePhysics"/>).</summary>
    EnablePhysics = 1,

    /// <summary>The item keeps the socket's rotation (<see cref="LooseItemTags.UseFixedRotation"/>).</summary>
    UseFixedRotation = 2,

    /// <summary>The socket's tilt is not limited (<see cref="LooseItemTags.IgnoreTiltLimit"/>).</summary>
    IgnoreTiltLimit = 4,
}

/// <summary>The gameplay tags of loose item sockets (gameplay tag names compare without case).</summary>
public static class LooseItemTags
{
    /// <summary>
    /// The tag of loose item sockets: a socket tagged with it or a tag below it (<c>Socket.Loot.Console</c>) holds one
    /// item stack. <c>Socket.LootContainer.*</c> and <c>Socket.LootOptions.*</c> are not below it.
    /// </summary>
    public const string Family = "Socket.Loot";

    /// <summary>The option that makes the item simulate physics.</summary>
    public const string EnablePhysics = "Socket.LootOptions.EnablePhysics";

    /// <summary>The option that keeps the socket's rotation.</summary>
    public const string UseFixedRotation = "Socket.LootOptions.UseFixedRotation";

    /// <summary>The option that lifts the socket's tilt limit.</summary>
    public const string IgnoreTiltLimit = "Socket.LootOptions.IgnoreTiltLimit";

    /// <summary>Each option with its gameplay tag.</summary>
    public static IReadOnlyList<(LooseItemOptions Option, string Tag)> Options { get; } =
    [
        (LooseItemOptions.EnablePhysics, EnablePhysics),
        (LooseItemOptions.UseFixedRotation, UseFixedRotation),
        (LooseItemOptions.IgnoreTiltLimit, IgnoreTiltLimit),
    ];

    /// <summary>Whether a socket tag marks a loose item socket: <see cref="Family"/> itself or a tag below it, without case.</summary>
    /// <param name="tag">The socket tag.</param>
    /// <returns>True for a loose item tag.</returns>
    public static bool IsLooseItemTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return tag.StartsWith(Family, StringComparison.OrdinalIgnoreCase) && (tag.Length == Family.Length || tag[Family.Length] == '.');
    }
}

/// <summary>
/// A loose item socket: a socket of a placed prop's static mesh, tagged as a place where the server may put one item
/// stack lying in the world. The socket's cell, actor, component and name identify it.
/// </summary>
/// <param name="Cell">
/// Generated World Partition cell that places the prop (the last segment of its package name), or empty for the
/// map's persistent level.
/// </param>
/// <param name="Actor">Placed actor that owns the prop.</param>
/// <param name="Component">The actor's static mesh component that shows the prop.</param>
/// <param name="Socket">Socket name in the mesh.</param>
/// <param name="Tag">The socket's <c>Socket.Loot.*</c> gameplay tag, spelled as in the mesh.</param>
/// <param name="Location">World location.</param>
/// <param name="Rotation">World rotation in degrees.</param>
/// <param name="Options">How the item is placed.</param>
/// <param name="DataLayer">Data layer that streams the prop, or null when it is always present.</param>
public sealed record LooseItemSocket(
    string Cell,
    string Actor,
    string Component,
    string Socket,
    string Tag,
    Vector3D Location,
    Rotator Rotation,
    LooseItemOptions Options,
    string? DataLayer);
