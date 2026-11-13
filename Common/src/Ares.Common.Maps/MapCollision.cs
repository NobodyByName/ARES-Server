using System.Collections.Immutable;

namespace Ares.Common.Maps;

/// <summary>
/// The static collision of one map: a library of collision bodies in body space, their placements in the world,
/// landscape height-field tiles and the cells that own placements and tiles. Lengths are centimetres, Z is up.
/// Built with <see cref="MapCollisionBuilder"/>, stored with <see cref="MapCollisionFile"/>.
/// </summary>
public sealed class MapCollision
{
    internal MapCollision(
        int gameBuildId,
        string mapName,
        ImmutableArray<MapCell> cells,
        ImmutableArray<CollisionBody> bodies,
        ImmutableArray<BodyPlacement> placements,
        ImmutableArray<HeightFieldTile> heightFields)
    {
        GameBuildId = gameBuildId;
        MapName = mapName;
        Cells = cells;
        Bodies = bodies;
        Placements = placements;
        HeightFields = heightFields;
    }

    /// <summary>The game build the collision was read from.</summary>
    public int GameBuildId { get; }

    /// <summary>Identifies the map (its world asset).</summary>
    public string MapName { get; }

    /// <summary>Map cells, addressed by <see cref="BodyPlacement.CellIndex"/> and <see cref="HeightFieldTile.CellIndex"/>.</summary>
    public ImmutableArray<MapCell> Cells { get; }

    /// <summary>Collision bodies, addressed by <see cref="BodyPlacement.BodyIndex"/>.</summary>
    public ImmutableArray<CollisionBody> Bodies { get; }

    /// <summary>Bodies placed in the world.</summary>
    public ImmutableArray<BodyPlacement> Placements { get; }

    /// <summary>Landscape height-field tiles.</summary>
    public ImmutableArray<HeightFieldTile> HeightFields { get; }
}
