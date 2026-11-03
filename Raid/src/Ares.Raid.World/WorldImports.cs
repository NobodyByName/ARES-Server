using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>
/// Static object exports of the Dam world: the player actors' class defaults, the map's persistent
/// level and its script actor, the game state and game mode, the data layers and global actors, the
/// HUD and the attribute set classes. Map objects are never loaded by the client from an export (it
/// loads the map itself on Welcome).
/// </summary>
public sealed class WorldImports
{
    private readonly List<StaticObjectExport> _records = [];
    private readonly Dictionary<string, ulong> _archetypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ulong> _classes = new(StringComparer.Ordinal);
    private readonly List<(GlobalActor Actor, ulong Guid)> _globalActors = [];

    /// <summary>Builds the exports.</summary>
    /// <param name="catalog">Replication schemas.</param>
    public WorldImports(ReplicationCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        uint package = catalog.Class(ClassPaths.Package).Checksum;
        foreach (ArchetypeClass player in (ArchetypeClass[])[PlayerClasses.PlayerController, PlayerClasses.PlayerState, PlayerClasses.Pawn])
        {
            (string archetypePackage, string name) = Split(player.Archetype);
            _archetypes[player.Class] = Add(Add(0, archetypePackage, package), name, catalog.Class(player.Class).Checksum);
        }

        ulong map = Add(0, DamMap.MapPackage, package, noLoad: true);
        ulong world = Add(map, DamMap.WorldName, catalog.Class(ClassPaths.World).Checksum, noLoad: true);
        ulong level = Add(world, DamMap.PersistentLevelName, catalog.Class(ClassPaths.Level).Checksum, noLoad: true);
        LevelScriptGuid = Add(level, DamMap.LevelScriptActorName, catalog.Class(DamMap.LevelScriptClass).Checksum, noLoad: true);

        (string statePackage, string stateName) = Split(DamMap.GameState.Archetype);
        _archetypes[DamMap.GameState.Class] = Add(Add(0, statePackage, package), stateName, catalog.Class(DamMap.GameState.Class).Checksum);
        (string modePackage, string modeName) = Split(DamMap.GameModeClass);
        _classes[DamMap.GameModeClass] = Add(Add(0, modePackage, package), modeName, catalog.Class(DamMap.GameModeClass).Checksum);

        DataLayersGuid = Add(level, DamMap.WorldDataLayersActorName, catalog.Class(DamMap.WorldDataLayersClass).Checksum, noLoad: true);
        foreach (GlobalActor actor in DamMap.GlobalActors)
        {
            _globalActors.Add((actor, Add(level, actor.Name, catalog.Class(actor.Class).Checksum, noLoad: true)));
        }

        (string hudPackage, string hudName) = Split(PlayerClasses.Hud);
        _classes[PlayerClasses.Hud] = Add(Add(0, hudPackage, package), hudName, PlayerClasses.HudExportChecksum);

        // Attribute sets are created at runtime: their classes are imported, not an archetype.
        foreach (AttributeSetDefaults set in CharacterDefaults.AttributeSets)
        {
            (string setPackage, string setName) = Split(set.Class);
            ulong outer = _records.FirstOrDefault(record => record.OuterGuid == 0 && record.Path == setPackage)?.Guid ?? Add(0, setPackage, package);
            _classes[set.Class] = Add(outer, setName, catalog.Class(set.Class).Checksum);
        }

        (string memberPackage, string memberName) = Split(PlayerClasses.SquadMember);
        ulong memberOuter = _records.FirstOrDefault(record => record.OuterGuid == 0 && record.Path == memberPackage)?.Guid ?? Add(0, memberPackage, package);
        (string _, string squadName) = Split(PlayerClasses.Squad.Archetype);
        _archetypes[PlayerClasses.Squad.Class] = Add(memberOuter, squadName, catalog.Class(PlayerClasses.Squad.Class).Checksum);
        _classes[PlayerClasses.SquadMember] = Add(memberOuter, memberName, catalog.Class(PlayerClasses.SquadMember).Checksum);

        Exports = new ObjectExports(_records);
    }

    /// <summary>All export records.</summary>
    public ObjectExports Exports { get; }

    /// <summary>The level script actor.</summary>
    public ulong LevelScriptGuid { get; }

    /// <summary>The world data layers actor.</summary>
    public ulong DataLayersGuid { get; }

    /// <summary>Class-default (archetype) exports by class path.</summary>
    public IReadOnlyDictionary<string, ulong> Archetypes => _archetypes;

    /// <summary>Class exports by class path.</summary>
    public IReadOnlyDictionary<string, ulong> Classes => _classes;

    /// <summary>Global actors of the persistent level with their ids.</summary>
    public IReadOnlyList<(GlobalActor Actor, ulong Guid)> GlobalActors => _globalActors;

    private static (string Outer, string Name) Split(string path)
    {
        int dot = path.LastIndexOf('.');
        return (path[..dot], path[(dot + 1)..]);
    }

    private ulong Add(ulong outer, string path, uint checksum, bool noLoad = false)
    {
        ulong guid = 3 + (2 * (ulong)_records.Count);
        _records.Add(new StaticObjectExport(guid, outer, path, checksum, noLoad));
        return guid;
    }
}
