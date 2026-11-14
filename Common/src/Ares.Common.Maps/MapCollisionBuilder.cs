using System.Collections.Immutable;
using System.Numerics;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Common.Maps;

/// <summary>
/// Collects the cells, bodies, placements and height-field tiles of one map and builds a <see cref="MapCollision"/>.
/// Bodies are shared by name: adding a body whose name is already known returns the existing index. Every addition is
/// checked, so a built map is always valid.
/// </summary>
public sealed class MapCollisionBuilder
{
    private readonly int _gameBuildId;
    private readonly string _mapName;
    private readonly List<MapCell> _cells = [];
    private readonly List<CollisionBody> _bodies = [];
    private readonly Dictionary<string, int> _bodyIndices = new(StringComparer.Ordinal);
    private readonly List<BodyPlacement> _placements = [];
    private readonly List<HeightFieldTile> _heightFields = [];

    /// <summary>Starts an empty map.</summary>
    /// <param name="gameBuildId">The game build the collision is read from.</param>
    /// <param name="mapName">Identifies the map (its world asset).</param>
    public MapCollisionBuilder(int gameBuildId, string mapName)
    {
        ArgumentException.ThrowIfNullOrEmpty(mapName);
        _gameBuildId = gameBuildId;
        _mapName = mapName;
    }

    /// <summary>Number of cells added so far.</summary>
    public int CellCount => _cells.Count;

    /// <summary>Number of distinct bodies added so far.</summary>
    public int BodyCount => _bodies.Count;

    /// <summary>Number of placements added so far.</summary>
    public int PlacementCount => _placements.Count;

    /// <summary>Adds a cell and returns its index.</summary>
    /// <param name="packageName">The cell's package name.</param>
    /// <param name="dataLayers">Names of the data layers the cell belongs to; empty for the base map.</param>
    public int AddCell(string packageName, IEnumerable<string> dataLayers)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageName);
        ArgumentNullException.ThrowIfNull(dataLayers);
        ImmutableArray<string> layers = [.. dataLayers];
        foreach (string layer in layers)
        {
            ArgumentException.ThrowIfNullOrEmpty(layer, nameof(dataLayers));
        }

        _cells.Add(new MapCell(packageName, layers));
        return _cells.Count - 1;
    }

    /// <summary>Finds the index of a body already added under a name.</summary>
    public bool TryGetBody(string name, out int index) => _bodyIndices.TryGetValue(name, out index);

    /// <summary>
    /// Adds a body and returns its index, or returns the index of the body already added under the same name (the new
    /// body is then ignored).
    /// </summary>
    /// <exception cref="ArgumentException">The body is invalid.</exception>
    public int AddBody(CollisionBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentException.ThrowIfNullOrEmpty(body.Name, nameof(body));
        if (_bodyIndices.TryGetValue(body.Name, out int existing))
        {
            return existing;
        }

        string? error = MapCollisionRules.FindBodyError(body);
        if (error is not null)
        {
            throw new ArgumentException($"Body '{body.Name}' {error}.", nameof(body));
        }

        _bodies.Add(body);
        _bodyIndices.Add(body.Name, _bodies.Count - 1);
        return _bodies.Count - 1;
    }

    /// <summary>Places a body in the world; its bounds are computed from the body and the transform.</summary>
    /// <param name="bodyIndex">Index returned by <see cref="AddBody"/>.</param>
    /// <param name="cellIndex">Index returned by <see cref="AddCell"/>.</param>
    /// <param name="worldTransform">The placement's world transform; the rotation is normalized.</param>
    /// <param name="flags">What the placement blocks.</param>
    /// <param name="surface">Surface type of the placement (see <see cref="BodyPlacement.Surface"/>).</param>
    /// <param name="materialSurfaces">Surface type of each material slot (see <see cref="BodyPlacement.MaterialSurfaces"/>), or none.</param>
    /// <exception cref="ArgumentException">An index is out of range or the transform or a surface type is invalid.</exception>
    public void AddPlacement(
        int bodyIndex,
        int cellIndex,
        Transform worldTransform,
        PlacementFlags flags,
        PhysicalSurfaceType surface = PhysicalSurfaceType.Default,
        IEnumerable<PhysicalSurfaceType>? materialSurfaces = null)
    {
        Ares.Structs.Core.Geometry.Quaternion source = worldTransform.Rotation;
        double length = Math.Sqrt((source.X * source.X) + (source.Y * source.Y) + (source.Z * source.Z) + (source.W * source.W));
        if (!double.IsFinite(length) || length < 1e-6)
        {
            throw new ArgumentException("The rotation is not a valid quaternion.", nameof(worldTransform));
        }

        Quaternion rotation = new((float)(source.X / length), (float)(source.Y / length), (float)(source.Z / length), (float)(source.W / length));
        Vector3 scale = new((float)worldTransform.Scale3D.X, (float)worldTransform.Scale3D.Y, (float)worldTransform.Scale3D.Z);
        BoundingBox bounds = (uint)bodyIndex < (uint)_bodies.Count
            ? MapCollisionRules.PlacedBounds(_bodies[bodyIndex], rotation, scale)
            : default;
        BodyPlacement placement = new(bodyIndex, cellIndex, worldTransform.Translation, rotation, scale, flags, bounds)
        {
            Surface = surface,
            MaterialSurfaces = [.. materialSurfaces ?? []],
        };
        string? error = MapCollisionRules.FindPlacementError(placement, _bodies.Count, _cells.Count);
        if (error is not null)
        {
            throw new ArgumentException($"Placement {error}.", nameof(worldTransform));
        }

        _placements.Add(placement);
    }

    /// <summary>Adds a landscape height-field tile.</summary>
    /// <exception cref="ArgumentException">The tile is invalid or refers to an unknown cell.</exception>
    public void AddHeightField(HeightFieldTile tile)
    {
        ArgumentNullException.ThrowIfNull(tile);
        string? error = MapCollisionRules.FindTileError(tile, _cells.Count);
        if (error is not null)
        {
            throw new ArgumentException($"Height-field tile {error}.", nameof(tile));
        }

        _heightFields.Add(tile);
    }

    /// <summary>Builds the map from everything added so far.</summary>
    public MapCollision Build() =>
        new(_gameBuildId, _mapName, [.. _cells], [.. _bodies], [.. _placements], [.. _heightFields]);
}
