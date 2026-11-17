using Ares.Common.GameData;
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
    /// <param name="gameData">The game data: the map and the items a character can hold.</param>
    public WorldImports(ReplicationCatalog catalog, RaidGameData gameData)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(gameData);
        MapData dam = gameData.Map;
        HeldItemClasses = gameData.Items.HeldItemClasses;
        uint package = catalog.Class(ClassPaths.Package).Checksum;
        foreach (ArchetypeClass player in (ArchetypeClass[])[PlayerClasses.PlayerController, PlayerClasses.PlayerState, PlayerClasses.Pawn])
        {
            (string archetypePackage, string name) = Split(player.Archetype);
            _archetypes[player.Class] = Add(Add(0, archetypePackage, package), name, catalog.Class(player.Class).Checksum);
        }

        ulong map = Add(0, dam.MapPackage, package, noLoad: true);
        ulong world = Add(map, dam.WorldName, catalog.Class(ClassPaths.World).Checksum, noLoad: true);
        ulong level = Add(world, dam.World.PersistentLevelName, catalog.Class(ClassPaths.Level).Checksum, noLoad: true);
        LevelScriptGuid = Add(level, dam.World.LevelScriptActorName, catalog.Class(dam.World.LevelScriptClass).Checksum, noLoad: true);

        ArchetypeClass gameState = dam.GameMode.GameState;
        string gameMode = dam.GameMode.GameModeClass;
        (string statePackage, string stateName) = Split(gameState.Archetype);
        _archetypes[gameState.Class] = Add(Add(0, statePackage, package), stateName, catalog.Class(gameState.Class).Checksum);
        (string modePackage, string modeName) = Split(gameMode);
        _classes[gameMode] = Add(Add(0, modePackage, package), modeName, catalog.Class(gameMode).Checksum);

        DataLayersGuid = Add(level, dam.World.WorldDataLayersActorName, catalog.Class(MapWorldClasses.WorldDataLayers).Checksum, noLoad: true);
        foreach (GlobalActor actor in DamProtocol.GlobalActors)
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

        foreach (string itemClass in HeldItemClasses.Values)
        {
            (string itemPackage, string itemName) = Split(itemClass);
            uint checksum = catalog.ReferenceClasses[itemClass].ExportChecksum
                ?? throw new InvalidOperationException("Held item class has no export checksum");
            _classes[itemClass] = Add(Add(0, itemPackage, package), itemName, checksum);
        }

        Exports = new ObjectExports(_records);
    }

    /// <summary>All export records.</summary>
    public ObjectExports Exports { get; }

    /// <summary>Gameplay classes of the items a character can hold, by usable-item index.</summary>
    public IReadOnlyDictionary<ushort, string> HeldItemClasses { get; }

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
