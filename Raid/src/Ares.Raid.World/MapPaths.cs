using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>
/// Static ids of objects the server names by path for the client to find, never load: map objects in the cells
/// it loaded (cell package, world, persistent level, placed actors and their named components) and classes those
/// keep loaded. Shared by the door and ladder catalogs so that each path has one id. They carry checksum 0, which
/// the client does not check.
/// </summary>
public sealed class MapPaths
{
    private const ulong FirstGuid = 0x200001;

    private readonly List<StaticObjectExport> _records = [];
    private readonly Dictionary<(ulong Outer, string Path), ulong> _guids = [];

    /// <summary>The persistent level of a cell (with the cell package and world).</summary>
    /// <param name="cell">Cell package.</param>
    /// <returns>The level id.</returns>
    public ulong Level(string cell) => MapObject(MapObject(MapObject(0, cell), DamMap.WorldName), DamMap.PersistentLevelName);

    /// <summary>A map object the client finds instead of loading.</summary>
    /// <param name="outer">Outer id; 0 for a package.</param>
    /// <param name="path">Name inside the outer, or the package path.</param>
    /// <returns>The id.</returns>
    public ulong MapObject(ulong outer, string path)
    {
        if (_guids.TryGetValue((outer, path), out ulong existing))
        {
            return existing;
        }

        ulong guid = FirstGuid + (2 * (ulong)_records.Count);
        _records.Add(new StaticObjectExport(guid, outer, path, 0, true));
        _guids[(outer, path)] = guid;
        return guid;
    }

    /// <summary>The exports of every path named so far.</summary>
    /// <returns>The exports.</returns>
    public ObjectExports Exports() => new(_records);
}
