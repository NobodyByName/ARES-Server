namespace Ares.Structs.Game.World;

/// <summary>A class and its class-default object (archetype).</summary>
/// <param name="Class">Class path.</param>
/// <param name="Archetype">Class-default object path.</param>
public readonly record struct ArchetypeClass(string Class, string Archetype);

/// <summary>
/// A global actor spawned with a stable name in the persistent level on both server and client;
/// the server replicates it.
/// </summary>
/// <param name="Name">Actor name.</param>
/// <param name="Class">Actor class path.</param>
public readonly record struct GlobalActor(string Name, string Class);

/// <summary>
/// The objects of a World Partition map's persistent world that the server names: the world, its persistent level and
/// the level's level script actor and world data layers actor.
/// </summary>
/// <param name="WorldName">World object name in the map package.</param>
/// <param name="PersistentLevelName">Persistent level object name in the world.</param>
/// <param name="LevelScriptActorName">Level script actor name in the persistent level.</param>
/// <param name="LevelScriptClass">Level script actor class (the map's level script Blueprint class).</param>
/// <param name="WorldDataLayersActorName">World data layers actor name in the persistent level.</param>
public sealed record MapWorldObjects(
    string WorldName,
    string PersistentLevelName,
    string LevelScriptActorName,
    string LevelScriptClass,
    string WorldDataLayersActorName);

/// <summary>Native classes of a map's persistent world objects.</summary>
public static class MapWorldClasses
{
    /// <summary>Script path of the world class.</summary>
    public const string World = "/Script/Engine.World";

    /// <summary>Script path of the level class.</summary>
    public const string Level = "/Script/Engine.Level";

    /// <summary>Script path of the world data layers actor class.</summary>
    public const string WorldDataLayers = "/Script/Engine.WorldDataLayers";
}

/// <summary>
/// The native data that closes a cooked placed actor export after its properties: two int32 words
/// (<see cref="FirstWord"/>, <see cref="SecondWord"/>), the actor's label as a string (an int32 length counting the
/// terminating zero, then that many one-byte characters) and <see cref="TailSize"/> more bytes.
/// </summary>
public static class PlacedActorNativeData
{
    /// <summary>The first word.</summary>
    public const int FirstWord = 0;

    /// <summary>The second word.</summary>
    public const int SecondWord = 1;

    /// <summary>The shortest label length (with its terminating zero).</summary>
    public const int MinLabelLength = 2;

    /// <summary>The longest label length (with its terminating zero).</summary>
    public const int MaxLabelLength = 256;

    /// <summary>Bytes after the label.</summary>
    public const int TailSize = 32;
}

/// <summary>
/// Members of the native <c>AActor</c> part of a placed actor's unversioned property chain. The chain lists the actor
/// class's script members first (derived class first) and the native <c>AActor</c> members after them, so the indices
/// here are relative to the end of the script members.
/// </summary>
public static class ActorMembers
{
    /// <summary><c>RootComponent</c>: object reference (4 bytes).</summary>
    public const int RootComponent = 58;
}
