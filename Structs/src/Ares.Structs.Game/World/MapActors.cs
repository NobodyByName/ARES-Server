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
