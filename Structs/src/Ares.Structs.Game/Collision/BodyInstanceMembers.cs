namespace Ares.Structs.Game.Collision;

/// <summary>Members of the unversioned <c>FBodyInstance</c> struct (a primitive component's collision settings).</summary>
public static class BodyInstanceMembers
{
    /// <summary><c>ObjectType</c>: the body's <see cref="CollisionChannel"/>, one byte.</summary>
    public const int ObjectType = 3;

    /// <summary><c>CollisionEnabled</c>: one byte, a <see cref="Collision.CollisionEnabled"/>.</summary>
    public const int CollisionEnabled = 4;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown20 = 20;

    /// <summary><c>CollisionProfileName</c>: name (8 bytes).</summary>
    public const int CollisionProfileName = 30;

    /// <summary><c>CollisionResponses</c>: an <c>FCollisionResponse</c> struct (see <see cref="CollisionResponseMembers"/>).</summary>
    public const int CollisionResponses = 31;

    /// <summary><c>WalkableSlopeOverride</c>: an <c>FWalkableSlopeOverride</c> struct (see <see cref="WalkableSlopeOverrideMembers"/>).</summary>
    public const int WalkableSlopeOverride = 41;

    /// <summary><c>PhysMaterialOverride</c>: object reference (4 bytes), a physical material that replaces the body's own.</summary>
    public const int PhysMaterialOverride = 42;

    /// <summary>Unknown member, 4 bytes.</summary>
    public const int Unknown43 = 43;

    /// <summary>Unknown member, 4 bytes (a float).</summary>
    public const int Unknown33 = 33;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown48 = 48;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown49 = 49;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown52 = 52;

    /// <summary>Members of the struct's chain.</summary>
    public const int MemberCount = 80;

    /// <summary>The struct's default <c>CollisionEnabled</c>, for a body instance that stores none and names no profile.</summary>
    public const CollisionEnabled DefaultCollisionEnabled = Collision.CollisionEnabled.QueryAndPhysics;

    /// <summary>The struct's default <c>ObjectType</c>, for a body instance that stores none and names no profile.</summary>
    public const CollisionChannel DefaultObjectType = CollisionChannel.WorldStatic;

    /// <summary>Bytes of the value of a fixed-size member; <see langword="null"/> for structs and unknown members.</summary>
    public static int? FixedValueSize(int member) => member switch
    {
        ObjectType or CollisionEnabled or Unknown20 or Unknown48 or Unknown49 or Unknown52 => 1,
        CollisionProfileName => 8,
        Unknown33 or PhysMaterialOverride or Unknown43 => 4,
        _ => null,
    };
}

/// <summary>Members of the unversioned <c>FCollisionResponse</c> struct.</summary>
public static class CollisionResponseMembers
{
    /// <summary><c>ResponseArray</c>: array of <c>FResponseChannel</c> (see <see cref="ResponseChannelMembers"/>), the per-channel overrides.</summary>
    public const int ResponseArray = 1;

    /// <summary>Members of the struct's chain.</summary>
    public const int MemberCount = 4;
}

/// <summary>Members of the unversioned <c>FResponseChannel</c> struct: one channel's response override.</summary>
public static class ResponseChannelMembers
{
    /// <summary><c>Channel</c>: the channel's name (8 bytes).</summary>
    public const int Channel = 0;

    /// <summary><c>Response</c>: one byte, a <see cref="CollisionResponse"/>.</summary>
    public const int Response = 1;

    /// <summary>Members of the struct's chain.</summary>
    public const int MemberCount = 2;

    /// <summary>The response of an entry that does not store one.</summary>
    public const CollisionResponse DefaultResponse = CollisionResponse.Block;
}

/// <summary>Members of the unversioned <c>FWalkableSlopeOverride</c> struct.</summary>
public static class WalkableSlopeOverrideMembers
{
    /// <summary><c>WalkableSlopeBehavior</c>: one byte.</summary>
    public const int WalkableSlopeBehavior = 0;

    /// <summary><c>WalkableSlopeAngle</c>: float, degrees.</summary>
    public const int WalkableSlopeAngle = 1;
}
