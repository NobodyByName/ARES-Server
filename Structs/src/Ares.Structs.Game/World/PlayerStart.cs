using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>An authored player start placed in a map.</summary>
/// <param name="Actor">Actor name in the persistent level.</param>
/// <param name="ClassName">Start class; see <see cref="PlayerStartClassNames"/>.</param>
/// <param name="Tags">Gameplay tags that decide which modes and rounds may use it.</param>
/// <param name="RootComponent">Name of the root component holding the transform.</param>
/// <param name="RootLocation">Root location.</param>
/// <param name="RootRotation">Root rotation (yaw only in current maps).</param>
/// <param name="SpawnPoints">Local spawn transforms of a group start; empty for a single placed start.</param>
public sealed record PlayerStart(
    string Actor,
    string ClassName,
    IReadOnlyList<string> Tags,
    string RootComponent,
    Vector3D RootLocation,
    Rotator RootRotation,
    IReadOnlyList<Transform> SpawnPoints);

/// <summary>Player-start class names.</summary>
public static class PlayerStartClassNames
{
    /// <summary>A single placed start.</summary>
    public const string Placed = "EmbarkPlayerStart";

    /// <summary>A group start holding several local spawn points.</summary>
    public const string Group = "PioneerPlayerStart";
}

/// <summary>Capsule of a group start's generated spawn points.</summary>
/// <param name="HalfHeight">Capsule half height; spawn points are raised by it along their up vector.</param>
/// <param name="Radius">Capsule radius.</param>
public readonly record struct PlayerStartCapsule(double HalfHeight, double Radius);
