namespace Ares.Structs.Game.Collision;

/// <summary>
/// Members of a <c>UStaticMeshSocket</c> subobject of a static mesh (a named point components can attach to). A
/// <c>UStaticMeshGameplayTagSocket</c> stores its gameplay tags first, so its socket members are one index later.
/// </summary>
public static class StaticMeshSocketMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.StaticMeshSocket";

    /// <summary>Script path of the gameplay-tag socket class.</summary>
    public const string GameplayTagSocketClassPath = "/Script/Engine.StaticMeshGameplayTagSocket";

    /// <summary>Gameplay-tag sockets only: <c>GameplayTags</c>, a count then names.</summary>
    public const int GameplayTags = 0;

    /// <summary><c>SocketName</c>: name (8 bytes).</summary>
    public const int SocketName = 0;

    /// <summary><c>RelativeLocation</c>: vector of three doubles.</summary>
    public const int RelativeLocation = 1;

    /// <summary><c>RelativeRotation</c>: rotator of three doubles.</summary>
    public const int RelativeRotation = 2;

    /// <summary><c>RelativeScale</c>: vector of three doubles.</summary>
    public const int RelativeScale = 3;

    /// <summary><c>Tag</c>: string.</summary>
    public const int Tag = 4;

    /// <summary>Bytes of native data after a socket's properties.</summary>
    public const int NativeDataSize = 4;
}
