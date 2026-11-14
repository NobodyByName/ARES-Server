using System.Numerics;
using Ares.Common.Maps;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Raid.World.Collision;

/// <summary>
/// The static collision one raid queries: the placements and height-field tiles of the map's base cells and of the
/// cells in the raid's active data layers, indexed once when the raid is prepared: a bounding volume hierarchy over
/// the placements, one over the height-field tiles and one per large triangle mesh. Placements that block nothing, have
/// no shapes or have a zero scale component are left out. Hulls and meshes take the placement's scale exactly; boxes,
/// spheres and capsules stay rigid: a box scales each half extent by its body axis's absolute scale, a sphere's
/// radius by the smallest absolute scale, a capsule's radius by the larger absolute X or Y scale and its full half
/// height by the absolute Z scale. Read-only after construction, so queries may run on any thread.
/// </summary>
public sealed partial class CollisionWorld
{
    private const float MinimumScale = 1e-6f;
    private const int MeshTreeMinimumTriangles = 16;
    private const PlacementFlags QueryFlags = PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable;

    private readonly int[] _placements;
    private readonly BoundingVolumeHierarchy _placementTree;
    private readonly int[] _tiles;
    private readonly WorldBox[] _tileBounds;
    private readonly BoundingVolumeHierarchy _tileTree;
    private readonly Dictionary<TriangleMesh, BoundingVolumeHierarchy> _meshTrees = new(ReferenceEqualityComparer.Instance);

    /// <summary>Indexes the collision of a map for one raid.</summary>
    /// <param name="map">The map's static collision.</param>
    /// <param name="activeDataLayers">
    /// Names of the raid's active data layers. A cell that belongs to data layers collides when any of them is active;
    /// base-map cells always collide. Null or empty keeps the base map only.
    /// </param>
    public CollisionWorld(MapCollision map, IReadOnlyCollection<string>? activeDataLayers = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        Map = map;
        HashSet<string> active = new(activeDataLayers ?? [], StringComparer.Ordinal);
        bool[] activeCells = [.. map.Cells.Select(cell => cell.IsBaseMap || cell.DataLayers.Any(active.Contains))];

        List<int> placements = [];
        List<WorldBox> placementBounds = [];
        for (int index = 0; index < map.Placements.Length; index++)
        {
            BodyPlacement placement = map.Placements[index];
            if (!activeCells[placement.CellIndex] || (placement.Flags & QueryFlags) == 0 || !HasVolume(placement) || !HasShapes(map.Bodies[placement.BodyIndex]))
            {
                continue;
            }

            placements.Add(index);
            AddMeshTrees(map.Bodies[placement.BodyIndex]);
            placementBounds.Add(new WorldBox(Double3.From(placement.WorldMinimum), Double3.From(placement.WorldMaximum)));
        }

        List<int> tiles = [];
        List<WorldBox> tileBounds = [];
        for (int index = 0; index < map.HeightFields.Length; index++)
        {
            HeightFieldTile tile = map.HeightFields[index];
            if (!activeCells[tile.CellIndex])
            {
                continue;
            }

            tiles.Add(index);
            tileBounds.Add(TileBounds(tile));
        }

        _placements = [.. placements];
        _placementTree = BoundingVolumeHierarchy.Build([.. placementBounds]);
        _tiles = [.. tiles];
        _tileBounds = [.. tileBounds];
        _tileTree = BoundingVolumeHierarchy.Build(_tileBounds);
    }

    /// <summary>The map the world was built from.</summary>
    private MapCollision Map { get; }

    /// <summary>Number of placements the world collides with.</summary>
    public int PlacementCount => _placements.Length;

    /// <summary>Number of height-field tiles the world collides with.</summary>
    public int HeightFieldCount => _tiles.Length;

    /// <summary>
    /// Casts a ray and returns the first surface it reaches within <paramref name="maxDistance"/>, or null. Height
    /// fields always collide; placements collide when they block any of <paramref name="mask"/>. Simple collision only:
    /// boxes, spheres, capsules and convex hulls are solid, triangle meshes and height fields collide from both sides.
    /// </summary>
    /// <param name="origin">World start of the ray, in centimetres.</param>
    /// <param name="direction">Ray direction; it is normalized.</param>
    /// <param name="maxDistance">Longest distance to test, in centimetres.</param>
    /// <param name="mask">What the placements must block to be hit.</param>
    /// <exception cref="ArgumentException">An argument is not finite, the direction is zero or the mask is empty.</exception>
    public CollisionHit? Raycast(Vector3D origin, Vector3 direction, double maxDistance, PlacementFlags mask)
    {
        Double3 start = Double3.From(origin);
        Double3 unit = Double3.From(direction);
        double length = unit.Length();
        if (!double.IsFinite(start.X) || !double.IsFinite(start.Y) || !double.IsFinite(start.Z))
        {
            throw new ArgumentException("The origin must be finite.", nameof(origin));
        }

        if (!double.IsFinite(length) || length == 0)
        {
            throw new ArgumentException("The direction must be finite and not zero.", nameof(direction));
        }

        if (!double.IsFinite(maxDistance) || maxDistance < 0)
        {
            throw new ArgumentException("The distance must be finite and not negative.", nameof(maxDistance));
        }

        ValidateMask(mask);
        unit *= 1 / length;
        PlacementRay placementRay = new(this, start, unit, mask);
        _placementTree.Raycast(start, unit, maxDistance, ref placementRay);
        double limit = placementRay.Index >= 0 ? placementRay.Distance : maxDistance;
        TileRay tileRay = new(this, start, unit);
        _tileTree.Raycast(start, unit, limit, ref tileRay);
        if (tileRay.Index < 0)
        {
            return placementRay.Index >= 0
                ? Hit(start, unit, placementRay.Distance, placementRay.Normal, CollisionHitKind.Placement, placementRay.Index) with { Surface = placementRay.Surface }
                : null;
        }

        CollisionHit tileHit = Hit(start, unit, tileRay.Distance, tileRay.Normal, CollisionHitKind.HeightField, tileRay.Index);
        return tileHit with { Surface = Map.HeightFields[tileRay.Index].SurfaceAt(tileHit.Point.X, tileHit.Point.Y) };
    }

    /// <summary>
    /// Height of the first surface straight below (<paramref name="x"/>, <paramref name="y"/>, <paramref name="fromZ"/>)
    /// within <paramref name="maxDrop"/>, or null; a downward <see cref="Raycast"/>.
    /// </summary>
    public double? GroundHeight(double x, double y, double fromZ, double maxDrop, PlacementFlags mask) =>
        Raycast(new Vector3D(x, y, fromZ), -Vector3.UnitZ, maxDrop, mask)?.Point.Z;

    private static void ValidateMask(PlacementFlags mask)
    {
        if ((mask & QueryFlags) == 0 || (mask & ~QueryFlags) != 0)
        {
            throw new ArgumentException("The mask must name what the placements block.", nameof(mask));
        }
    }

    private static CollisionHit Hit(Double3 origin, Double3 direction, double distance, Double3 normal, CollisionHitKind kind, int index) =>
        new(distance, (origin + (direction * distance)).ToVector3D(), normal.ToVector3(), kind, index);

    private static bool HasVolume(in BodyPlacement placement) =>
        Math.Abs(placement.Scale.X) >= MinimumScale && Math.Abs(placement.Scale.Y) >= MinimumScale && Math.Abs(placement.Scale.Z) >= MinimumScale;

    private static bool HasShapes(CollisionBody body) =>
        body.Boxes.Length + body.Spheres.Length + body.Capsules.Length + body.ConvexHulls.Length + body.TriangleMeshes.Length > 0;

    /// <summary>Indexes the triangles of the body's meshes that are large enough for the hierarchy to pay off.</summary>
    private void AddMeshTrees(CollisionBody body)
    {
        foreach (TriangleMesh mesh in body.TriangleMeshes.Where(mesh => mesh.TriangleCount > MeshTreeMinimumTriangles && !_meshTrees.ContainsKey(mesh)))
        {
            _meshTrees.Add(mesh, ShapeRaycasts.MeshTree(mesh));
        }
    }

    private static WorldBox TileBounds(HeightFieldTile tile)
    {
        short lowest = short.MaxValue;
        short highest = short.MinValue;
        foreach (short height in tile.Heights)
        {
            lowest = Math.Min(lowest, height);
            highest = Math.Max(highest, height);
        }

        double low = lowest * (double)tile.HeightScale;
        double high = highest * (double)tile.HeightScale;
        Vector3D origin = tile.Origin;
        return new WorldBox(
            new Double3(origin.X, origin.Y, origin.Z + Math.Min(low, high)),
            new Double3(
                origin.X + ((tile.CountX - 1) * (double)tile.SpacingX),
                origin.Y + ((tile.CountY - 1) * (double)tile.SpacingY),
                origin.Z + Math.Max(low, high)));
    }

    /// <summary>Casts a world ray against one placement; the normal is a unit world vector, the surface type that of the shape hit.</summary>
    private bool RaycastPlacement(int placementIndex, Double3 origin, Double3 direction, double maxT, out double t, out Double3 normal,
        out PhysicalSurfaceType surface)
    {
        ref readonly BodyPlacement placement = ref Map.Placements.ItemRef(placementIndex);
        (int bodyIndex, _, Vector3D translation, Quaternion rotation, Vector3 scale, _, _) = placement;
        CollisionBody body = Map.Bodies[bodyIndex];
        surface = placement.Surface;
        Double3 frameOrigin = (origin - Double3.From(translation)).RotateInverse(rotation);
        Double3 frameDirection = direction.RotateInverse(rotation);
        Double3 localOrigin = frameOrigin.DivideBy(scale);
        Double3 localDirection = frameDirection.DivideBy(scale);
        t = maxT;
        Double3 frameNormal = default;
        bool hit = false;
        foreach (CollisionBox box in body.Boxes)
        {
            hit |= Closer(ShapeRaycasts.Box(ConvexShapes.Box(box, scale), frameOrigin, frameDirection, t, out double candidate, out Double3 shapeNormal), candidate, shapeNormal, ref t, ref frameNormal);
        }

        foreach (CollisionSphere sphere in body.Spheres)
        {
            SegmentShape core = ConvexShapes.Sphere(sphere, scale, out double radius);
            hit |= Closer(ShapeRaycasts.Sphere(core.Center, radius, frameOrigin, frameDirection, t, out double candidate, out Double3 shapeNormal), candidate, shapeNormal, ref t, ref frameNormal);
        }

        foreach (CollisionCapsule capsule in body.Capsules)
        {
            SegmentShape core = ConvexShapes.Capsule(capsule, scale, out double radius, out double halfLength);
            hit |= Closer(
                ShapeRaycasts.Capsule(core.Center, capsule.Rotation, radius, halfLength, frameOrigin, frameDirection, t, out double candidate, out Double3 shapeNormal),
                candidate,
                shapeNormal,
                ref t,
                ref frameNormal);
        }

        // Body-space normals transform with the inverse transpose of the scale.
        foreach (ConvexHull hull in body.ConvexHulls)
        {
            hit |= Closer(ShapeRaycasts.Hull(hull, localOrigin, localDirection, t, out double candidate, out Double3 shapeNormal), candidate, shapeNormal.DivideBy(scale), ref t, ref frameNormal);
        }

        foreach (TriangleMesh mesh in body.TriangleMeshes)
        {
            bool closer = Closer(
                ShapeRaycasts.Mesh(mesh, _meshTrees.GetValueOrDefault(mesh), localOrigin, localDirection, t, out double candidate, out Double3 shapeNormal, out int triangle),
                candidate,
                shapeNormal.DivideBy(scale),
                ref t,
                ref frameNormal);
            if (closer)
            {
                surface = placement.TriangleSurface(mesh, triangle);
            }

            hit |= closer;
        }

        normal = !hit ? default : t == 0 ? -direction : frameNormal.Rotate(rotation).Normalized();
        return hit;
    }

    private static bool Closer(bool hit, double candidate, Double3 candidateNormal, ref double t, ref Double3 normal)
    {
        if (!hit || candidate >= t)
        {
            return false;
        }

        t = candidate;
        normal = candidateNormal;
        return true;
    }

    /// <summary>Keeps the closest placement a ray hits.</summary>
    private struct PlacementRay(CollisionWorld world, Double3 origin, Double3 direction, PlacementFlags mask) : IRayVisitor
    {
        public int Index { get; private set; } = -1;

        public double Distance { get; private set; }

        public Double3 Normal { get; private set; }

        public PhysicalSurfaceType Surface { get; private set; }

        public double Visit(int item, double maxDistance)
        {
            int placementIndex = world._placements[item];
            if ((world.Map.Placements[placementIndex].Flags & mask) == 0
                || !world.RaycastPlacement(placementIndex, origin, direction, maxDistance, out double t, out Double3 normal, out PhysicalSurfaceType surface))
            {
                return maxDistance;
            }

            Index = placementIndex;
            Distance = t;
            Normal = normal;
            Surface = surface;
            return t;
        }
    }

    /// <summary>Keeps the closest height-field tile a ray hits.</summary>
    private struct TileRay(CollisionWorld world, Double3 origin, Double3 direction) : IRayVisitor
    {
        private readonly Double3 _inverseDirection = WorldBox.InverseDirection(direction);

        public int Index { get; private set; } = -1;

        public double Distance { get; private set; }

        public Double3 Normal { get; private set; }

        public double Visit(int item, double maxDistance)
        {
            int tileIndex = world._tiles[item];
            if (!world._tileBounds[item].Clip(origin, _inverseDirection, maxDistance, out double enter, out double exit)
                || !HeightFieldRaycast.Raycast(world.Map.HeightFields[tileIndex], origin, direction, enter, exit, maxDistance, out double t, out Double3 normal))
            {
                return maxDistance;
            }

            Index = tileIndex;
            Distance = t;
            Normal = normal.Normalized();
            return t;
        }
    }
}
