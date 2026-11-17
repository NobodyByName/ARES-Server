namespace Ares.Structs.Game.World;

/// <summary>
/// Chain facts of an <c>AEmbarkPlayerStart</c> actor (a single placed start), derived class first: its own
/// <c>SpawnTags</c>, then <c>APlayerStart</c>, <c>ANavigationObjectBase</c> and <c>AActor</c>.
/// </summary>
public static class EmbarkPlayerStartMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Angelscript.EmbarkPlayerStart";

    /// <summary><c>SpawnTags</c>: gameplay tag container (a count and that many names).</summary>
    public const int SpawnTags = 0;

    /// <summary><c>CapsuleComponent</c>: object reference (4 bytes).</summary>
    public const int CapsuleComponent = 2;

    /// <summary><c>GoodSprite</c>: object reference (4 bytes).</summary>
    public const int GoodSprite = 3;

    /// <summary><c>BadSprite</c>: object reference (4 bytes).</summary>
    public const int BadSprite = 4;

    /// <summary><c>SpawnCollisionHandlingMethod</c>: enum, one byte.</summary>
    public const int SpawnCollisionHandlingMethod = 52;

    /// <summary><c>RootComponent</c>: object reference (4 bytes).</summary>
    public const int RootComponent = 64;
}

/// <summary>
/// Chain facts of an <c>APioneerPlayerStart</c> actor (a group start), derived class first: the members of
/// <c>APioneerPlayerStartBase</c>, then <c>AActor</c>.
/// </summary>
public static class PioneerPlayerStartMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Angelscript.PioneerPlayerStart";

    /// <summary><c>SpawnTags</c>: gameplay tag container (a count and that many names).</summary>
    public const int SpawnTags = 0;

    /// <summary><c>SpawnPoints</c>: array of <see cref="PlayerSpawnPointMembers"/> structs.</summary>
    public const int SpawnPoints = 1;

    /// <summary><c>NameForTelemetry</c>: string.</summary>
    public const int NameForTelemetry = 2;

    /// <summary><c>PioneerPlayerStartComponent</c>: object reference (4 bytes).</summary>
    public const int PioneerPlayerStartComponent = 3;

    /// <summary><c>RootComponent</c>: object reference (4 bytes).</summary>
    public const int RootComponent = 62;

    /// <summary>Most spawn points a group start may hold.</summary>
    public const int MaxSpawnPoints = 64;
}

/// <summary>
/// Chain facts of a <c>UPioneerPlayerStartComponent</c>, a group start's root: a scene component with no reflected
/// members of its own.
/// </summary>
public static class PioneerPlayerStartComponentMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Angelscript.PioneerPlayerStartComponent";

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 0;
}

/// <summary>Members of the unversioned <c>FPlayerSpawnPoint</c> struct, one local spawn point of a group start.</summary>
public static class PlayerSpawnPointMembers
{
    /// <summary><c>SpawnTransform</c>: transform (see <c>TransformMembers</c>) in the start's space.</summary>
    public const int SpawnTransform = 0;

    /// <summary>Members of the struct.</summary>
    public const int MemberCount = 1;
}
