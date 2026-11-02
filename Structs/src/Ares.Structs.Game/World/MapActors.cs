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
