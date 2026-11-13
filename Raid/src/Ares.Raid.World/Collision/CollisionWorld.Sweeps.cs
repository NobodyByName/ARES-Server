using System.Numerics;
using Ares.Common.Maps;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Raid.World.Collision;

/// <summary>Shape sweeps, overlaps and penetration queries; see <see cref="Raycast"/> for the collision they see.</summary>
public sealed partial class CollisionWorld
{
    private const int PenetrationPasses = 16;

    /// <summary>
    /// Sweeps a sphere from <paramref name="start"/> to <paramref name="end"/> and returns the first surface it touches,
    /// or null; see <see cref="SweepCapsule(Vector3D, Vector3D, Quaternion, double, double, PlacementFlags)"/>.
    /// </summary>
    /// <exception cref="ArgumentException">A position or the radius is not finite, the radius is negative or the mask is empty.</exception>
    public CollisionHit? SweepSphere(Vector3D start, Vector3D end, double radius, PlacementFlags mask) =>
        Sweep(start, end, CapsuleAxis(Quaternion.Identity, radius, radius), radius, mask);

    /// <summary>
    /// Sweeps an upright capsule (its axis along world Z); see
    /// <see cref="SweepCapsule(Vector3D, Vector3D, Quaternion, double, double, PlacementFlags)"/>.
    /// </summary>
    public CollisionHit? SweepCapsule(Vector3D start, Vector3D end, double radius, double halfHeight, PlacementFlags mask) =>
        SweepCapsule(start, end, Quaternion.Identity, radius, halfHeight, mask);

    /// <summary>
    /// Sweeps a capsule centred on <paramref name="start"/> to <paramref name="end"/> without turning it and returns
    /// the first surface it touches, or null. The hit's distance is how far the centre travels until the capsule
    /// touches; the point is the contact and the normal points from it towards the capsule. A capsule that starts
    /// penetrating a surface deeper than 0.001 cm hits it at distance 0 whatever the motion, with
    /// <see cref="CollisionHit.StartPenetrating"/> set, the push-out direction as the normal and the depth (the deepest
    /// of such surfaces wins). Otherwise a surface is hit only when the motion would take the capsule deeper than
    /// 0.001 cm into it, at the distance where they touch: resting or sliding contact (within that depth) and the
    /// seams between flat neighbouring shapes do not block, and a capsule that starts touching a surface hits it at
    /// distance 0 only when it moves into it. Masks and the collision seen are those of <see cref="Raycast"/>.
    /// </summary>
    /// <param name="start">World centre of the capsule at the start, in centimetres.</param>
    /// <param name="end">World centre of the capsule at the end.</param>
    /// <param name="rotation">Orientation of the capsule; its axis is the rotated Z axis.</param>
    /// <param name="radius">Capsule radius.</param>
    /// <param name="halfHeight">Half the capsule's full height, caps included; at least the radius.</param>
    /// <param name="mask">What the placements must block to be hit.</param>
    /// <exception cref="ArgumentException">
    /// A value is not finite, the radius is negative, the half height is below the radius, the rotation is zero or the
    /// mask is empty.
    /// </exception>
    public CollisionHit? SweepCapsule(Vector3D start, Vector3D end, Quaternion rotation, double radius, double halfHeight, PlacementFlags mask) =>
        Sweep(start, end, CapsuleAxis(rotation, radius, halfHeight), radius, mask);

    /// <summary>
    /// Whether a capsule penetrates any surface deeper than 0.001 cm (touching does not count); the parameters are
    /// those of <see cref="SweepCapsule(Vector3D, Vector3D, Quaternion, double, double, PlacementFlags)"/>.
    /// </summary>
    public bool OverlapCapsule(Vector3D center, Quaternion rotation, double radius, double halfHeight, PlacementFlags mask)
    {
        Double3 halfAxis = CapsuleAxis(rotation, radius, halfHeight);
        Double3 position = FinitePosition(center, nameof(center));
        ValidateMask(mask);
        OverlapTest test = new(position, halfAxis, radius);
        WorldBox bounds = Bounds(position, test.Query.Extent);
        PlacementVisitor<OverlapTest> placements = new(this, mask, test);
        _placementTree.Overlap(bounds, ref placements);
        if (placements.Test.Found)
        {
            return true;
        }

        TileVisitor<OverlapTest> tiles = new(this, placements.Test);
        _tileTree.Overlap(bounds, ref tiles);
        return tiles.Test.Found;
    }

    /// <summary>
    /// The translation that moves a penetrating capsule clear of every surface, or null when it penetrates none
    /// (deeper than 0.001 cm). The capsule is pushed out of the shape it penetrates deepest by the shortest way, then
    /// again for what it still penetrates, up to 16 times: exact for one shape or for flat neighbouring shapes. It can
    /// stay stuck where surfaces close in from opposite sides; <see cref="OverlapCapsule"/> at the moved position
    /// tells. The parameters are
    /// those of <see cref="SweepCapsule(Vector3D, Vector3D, Quaternion, double, double, PlacementFlags)"/>.
    /// </summary>
    public Vector3D? ComputePenetration(Vector3D center, Quaternion rotation, double radius, double halfHeight, PlacementFlags mask)
    {
        Double3 halfAxis = CapsuleAxis(rotation, radius, halfHeight);
        Double3 start = FinitePosition(center, nameof(center));
        ValidateMask(mask);
        Double3 position = start;
        bool moved = false;
        for (int pass = 0; pass < PenetrationPasses; pass++)
        {
            PenetrationTest test = new(position, halfAxis, radius);
            WorldBox bounds = Bounds(position, test.Query.Extent);
            PlacementVisitor<PenetrationTest> placements = new(this, mask, test);
            _placementTree.Overlap(bounds, ref placements);
            TileVisitor<PenetrationTest> tiles = new(this, placements.Test);
            _tileTree.Overlap(bounds, ref tiles);
            if (tiles.Test.Depth == 0)
            {
                break;
            }

            moved = true;
            position += tiles.Test.Direction * tiles.Test.Depth;
        }

        return moved ? (position - start).ToVector3D() : null;
    }

    private CollisionHit? Sweep(Vector3D start, Vector3D end, Double3 halfAxis, double radius, PlacementFlags mask)
    {
        Double3 from = FinitePosition(start, nameof(start));
        Double3 motion = FinitePosition(end, nameof(end)) - from;
        ValidateMask(mask);
        SweepTest test = new(from, halfAxis, radius, motion);
        Double3 extent = test.Query.Extent;
        PlacementVisitor<SweepTest> placements = new(this, mask, test);
        _placementTree.Sweep(from, motion, extent, 1, ref placements);
        TileVisitor<SweepTest> tiles = new(this, placements.Test);
        _tileTree.Sweep(from, motion, extent, tiles.Test.Limit, ref tiles);
        test = tiles.Test;
        if (!test.Found)
        {
            return null;
        }

        ShapeContact best = test.Best;
        return new CollisionHit(best.Time * motion.Length(), best.Point.ToVector3D(), best.Normal.ToVector3(), test.Kind, test.Index, best.StartPenetrating, best.Depth);
    }

    /// <summary>Validates a capsule and returns the half segment of its core in world space.</summary>
    private static Double3 CapsuleAxis(Quaternion rotation, double radius, double halfHeight)
    {
        if (!double.IsFinite(radius) || radius < 0)
        {
            throw new ArgumentException("The radius must be finite and not negative.", nameof(radius));
        }

        if (!double.IsFinite(halfHeight) || halfHeight < radius)
        {
            throw new ArgumentException("The half height must be finite and at least the radius.", nameof(halfHeight));
        }

        float length = rotation.Length();
        if (!float.IsFinite(length) || length == 0)
        {
            throw new ArgumentException("The rotation must be finite and not zero.", nameof(rotation));
        }

        return new Double3(0, 0, halfHeight - radius).Rotate(Quaternion.Normalize(rotation));
    }

    private static Double3 FinitePosition(Vector3D position, string name)
    {
        Double3 value = Double3.From(position);
        if (!double.IsFinite(value.X) || !double.IsFinite(value.Y) || !double.IsFinite(value.Z))
        {
            throw new ArgumentException("The position must be finite.", name);
        }

        return value;
    }

    private static WorldBox Bounds(Double3 center, Double3 extent) => new(center - extent, center + extent);

    /// <summary>Runs a test against every shape of a placement whose bounds the query reaches; false when the test stops.</summary>
    private bool TestPlacement<TTest>(int placementIndex, ref TTest test)
        where TTest : struct, IShapeTest
    {
        BodyPlacement placement = Map.Placements[placementIndex];
        CollisionBody body = Map.Bodies[placement.BodyIndex];
        Vector3 scale = placement.Scale;
        foreach (CollisionBox box in body.Boxes)
        {
            if (!TestBox(ConvexShapes.Box(box, scale), ref test))
            {
                return false;
            }
        }

        foreach (CollisionSphere sphere in body.Spheres)
        {
            SegmentShape shape = ConvexShapes.Sphere(sphere, scale, out double radius);
            Double3 reach = new(radius, radius, radius);
            if (test.Query.Reaches(shape.Center - reach, shape.Center + reach, test.Limit) && !test.Test(shape, radius))
            {
                return false;
            }
        }

        foreach (CollisionCapsule capsule in body.Capsules)
        {
            SegmentShape shape = ConvexShapes.Capsule(capsule, scale, out double radius, out _);
            Double3 reach = shape.HalfAxis.Abs() + new Double3(radius, radius, radius);
            if (test.Query.Reaches(shape.Center - reach, shape.Center + reach, test.Limit) && !test.Test(shape, radius))
            {
                return false;
            }
        }

        foreach (ConvexHull hull in body.ConvexHulls)
        {
            ScaledBounds(hull.Bounds, scale, out Double3 minimum, out Double3 maximum);
            if (test.Query.Reaches(minimum, maximum, test.Limit) && !test.Test(new HullShape(hull, scale), 0))
            {
                return false;
            }
        }

        foreach (TriangleMesh mesh in body.TriangleMeshes)
        {
            ScaledBounds(mesh.Bounds, scale, out Double3 minimum, out Double3 maximum);
            if (test.Query.Reaches(minimum, maximum, test.Limit) && !TestMesh(mesh, scale, ref test))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Runs a test against a box when the query's bounds reach it; false when the test stops.</summary>
    private static bool TestBox<TTest>(in OrientedBoxShape shape, ref TTest test)
        where TTest : struct, IShapeTest =>
        !test.Query.Reaches(shape.Minimum, shape.Maximum, test.Limit) || test.Test(shape, 0);

    private bool TestMesh<TTest>(TriangleMesh mesh, Vector3 scale, ref TTest test)
        where TTest : struct, IShapeTest
    {
        if (_meshTrees.TryGetValue(mesh, out BoundingVolumeHierarchy? tree))
        {
            // The mesh hierarchy is in body space, where the query's swept box divides by the scale.
            CapsuleQuery query = test.Query;
            MeshVisitor<TTest> visitor = new(mesh, scale, test);
            tree.Sweep(query.Center.DivideBy(scale), query.Motion.DivideBy(scale), query.Extent.DivideBy(Vector3.Abs(scale)), test.Limit, ref visitor);
            test = visitor.Test;
            return !visitor.Stopped;
        }

        for (int triangle = 0; triangle < mesh.TriangleCount; triangle++)
        {
            TriangleShape shape = MeshTriangle(mesh, triangle, scale);
            if (test.Query.Reaches(shape.Minimum, shape.Maximum, test.Limit) && !test.Test(shape, 0))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Runs a test against the triangles of a tile's cells that the query's bounds reach; false when the test stops.</summary>
    private bool TestTile<TTest>(int tileIndex, ref TTest test)
        where TTest : struct, IShapeTest
    {
        HeightFieldTile tile = Map.HeightFields[tileIndex];
        CapsuleQuery query = test.Query;
        Double3 end = query.Center + (query.Motion * test.Limit);
        Double3 low = Double3.Min(query.Center, end) - query.Extent;
        Double3 high = Double3.Max(query.Center, end) + query.Extent;
        int cellsX = tile.CountX - 1;
        int cellsY = tile.CountY - 1;
        if (high.X < 0 || high.Y < 0 || low.X > cellsX * (double)tile.SpacingX || low.Y > cellsY * (double)tile.SpacingY)
        {
            return true;
        }

        int firstX = Math.Clamp((int)Math.Floor(low.X / tile.SpacingX), 0, cellsX - 1);
        int lastX = Math.Clamp((int)Math.Floor(high.X / tile.SpacingX), 0, cellsX - 1);
        int firstY = Math.Clamp((int)Math.Floor(low.Y / tile.SpacingY), 0, cellsY - 1);
        int lastY = Math.Clamp((int)Math.Floor(high.Y / tile.SpacingY), 0, cellsY - 1);
        for (int y = firstY; y <= lastY; y++)
        {
            for (int x = firstX; x <= lastX; x++)
            {
                for (int half = 0; half < 2; half++)
                {
                    if (!HeightFieldRaycast.CellTriangle(tile, x, y, half == 1, out Double3 a, out Double3 b, out Double3 c))
                    {
                        continue;
                    }

                    TriangleShape shape = new(a, b, c);
                    if (test.Query.Reaches(shape.Minimum, shape.Maximum, test.Limit) && !test.Test(shape, 0))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static TriangleShape MeshTriangle(TriangleMesh mesh, int triangle, Vector3 scale)
    {
        ReadOnlySpan<Vector3> vertices = mesh.Vertices.AsSpan();
        ReadOnlySpan<int> indices = mesh.Indices.AsSpan();
        int first = triangle * 3;
        return new TriangleShape(
            Double3.From(vertices[indices[first]]).Scale(scale),
            Double3.From(vertices[indices[first + 1]]).Scale(scale),
            Double3.From(vertices[indices[first + 2]]).Scale(scale));
    }

    private static void ScaledBounds(BoundingBox bounds, Vector3 scale, out Double3 minimum, out Double3 maximum)
    {
        Double3 first = Double3.From(bounds.Minimum).Scale(scale);
        Double3 second = Double3.From(bounds.Maximum).Scale(scale);
        minimum = Double3.Min(first, second);
        maximum = Double3.Max(first, second);
    }

    /// <summary>One kind of capsule query, run shape by shape in each placement's or tile's frame.</summary>
    private interface IShapeTest
    {
        /// <summary>The query in the current frame.</summary>
        CapsuleQuery Query { get; }

        /// <summary>Latest sweep time still of interest; 0 for queries that do not move.</summary>
        double Limit { get; }

        /// <summary>Moves the query into the frame of a placement or tile.</summary>
        void Enter(CollisionHitKind kind, int index, Double3 translation, Quaternion rotation);

        /// <summary>Tests one shape of the current frame; false stops the query.</summary>
        bool Test<T>(in T shape, double radius)
            where T : struct, IConvexShape;

        /// <summary>Leaves the current frame.</summary>
        void Leave();
    }

    /// <summary>Keeps the first contact of a sweep, in world space.</summary>
    private struct SweepTest(Double3 start, Double3 halfAxis, double radius, Double3 motion) : IShapeTest
    {
        private Double3 _translation;
        private Quaternion _rotation;
        private CollisionHitKind _kind;
        private int _index;

        public CapsuleQuery Query { get; private set; } = new(start, halfAxis, radius, motion);

        public readonly double Limit => !Found ? 1 : Best.StartPenetrating ? 0 : Best.Time;

        public bool Found { get; private set; }

        public ShapeContact Best { get; private set; }

        public CollisionHitKind Kind { get; private set; }

        public int Index { get; private set; }

        public void Enter(CollisionHitKind kind, int index, Double3 translation, Quaternion rotation)
        {
            (_kind, _index, _translation, _rotation) = (kind, index, translation, rotation);
            Query = new CapsuleQuery((start - translation).RotateInverse(rotation), halfAxis.RotateInverse(rotation), radius, motion.RotateInverse(rotation));
        }

        public bool Test<T>(in T shape, double shapeRadius)
            where T : struct, IConvexShape
        {
            if (!ConvexQueries.Sweep(shape, shapeRadius, Query, Limit, out ShapeContact contact) || (Found && !contact.IsBetterThan(Best)))
            {
                return true;
            }

            Best = contact with { Point = _translation + contact.Point.Rotate(_rotation), Normal = contact.Normal.Rotate(_rotation) };
            Found = true;
            Kind = _kind;
            Index = _index;
            return true;
        }

        public readonly void Leave()
        {
        }
    }

    /// <summary>Finds whether a resting capsule overlaps any shape.</summary>
    private struct OverlapTest(Double3 center, Double3 halfAxis, double radius) : IShapeTest
    {
        public CapsuleQuery Query { get; private set; } = new(center, halfAxis, radius, default);

        public readonly double Limit => 0;

        public bool Found { get; private set; }

        public void Enter(CollisionHitKind kind, int index, Double3 translation, Quaternion rotation) =>
            Query = new CapsuleQuery((center - translation).RotateInverse(rotation), halfAxis.RotateInverse(rotation), radius, default);

        public bool Test<T>(in T shape, double shapeRadius)
            where T : struct, IConvexShape
        {
            Found = ConvexQueries.Overlaps(shape, shapeRadius, Query.Center, Query.HalfAxis, Query.Radius);
            return !Found;
        }

        public readonly void Leave()
        {
        }
    }

    /// <summary>Finds the shape a resting capsule penetrates deepest and the world push that clears it.</summary>
    private struct PenetrationTest(Double3 center, Double3 halfAxis, double radius) : IShapeTest
    {
        private Quaternion _rotation;

        public CapsuleQuery Query { get; private set; } = new(center, halfAxis, radius, default);

        public readonly double Limit => 0;

        /// <summary>Unit world direction of the deepest push found.</summary>
        public Double3 Direction { get; private set; }

        /// <summary>Length of the deepest push found; 0 when the capsule penetrates nothing.</summary>
        public double Depth { get; private set; }

        public void Enter(CollisionHitKind kind, int index, Double3 translation, Quaternion rotation)
        {
            _rotation = rotation;
            Query = new CapsuleQuery((center - translation).RotateInverse(rotation), halfAxis.RotateInverse(rotation), radius, default);
        }

        public bool Test<T>(in T shape, double shapeRadius)
            where T : struct, IConvexShape
        {
            CapsuleQuery query = Query;
            if (!ConvexQueries.Penetration(shape, shapeRadius, query.Center, query.HalfAxis, query.Radius, out Double3 direction, out double depth) || !(depth > Depth))
            {
                return true;
            }

            Depth = depth;
            Direction = direction.Rotate(_rotation);
            return true;
        }

        public readonly void Leave()
        {
        }
    }

    /// <summary>Runs a test on the placements a broad-phase query reaches.</summary>
    private struct PlacementVisitor<TTest>(CollisionWorld world, PlacementFlags mask, TTest test) : IRayVisitor, IOverlapVisitor
        where TTest : struct, IShapeTest
    {
        public TTest Test = test;

        public double Visit(int item, double maxDistance)
        {
            VisitPlacement(item);
            return Test.Limit;
        }

        public bool Visit(int item) => VisitPlacement(item);

        private bool VisitPlacement(int item)
        {
            int placementIndex = world._placements[item];
            BodyPlacement placement = world.Map.Placements[placementIndex];
            if ((placement.Flags & mask) == 0)
            {
                return true;
            }

            Test.Enter(CollisionHitKind.Placement, placementIndex, Double3.From(placement.Translation), placement.Rotation);
            bool goOn = world.TestPlacement(placementIndex, ref Test);
            Test.Leave();
            return goOn;
        }
    }

    /// <summary>Runs a test on the height-field tiles a broad-phase query reaches.</summary>
    private struct TileVisitor<TTest>(CollisionWorld world, TTest test) : IRayVisitor, IOverlapVisitor
        where TTest : struct, IShapeTest
    {
        public TTest Test = test;

        public double Visit(int item, double maxDistance)
        {
            VisitTile(item);
            return Test.Limit;
        }

        public bool Visit(int item) => VisitTile(item);

        private bool VisitTile(int item)
        {
            int tileIndex = world._tiles[item];
            Test.Enter(CollisionHitKind.HeightField, tileIndex, Double3.From(world.Map.HeightFields[tileIndex].Origin), Quaternion.Identity);
            bool goOn = world.TestTile(tileIndex, ref Test);
            Test.Leave();
            return goOn;
        }
    }

    /// <summary>Runs a test on the triangles of a mesh that its hierarchy reaches.</summary>
    private struct MeshVisitor<TTest>(TriangleMesh mesh, Vector3 scale, TTest test) : IRayVisitor
        where TTest : struct, IShapeTest
    {
        public TTest Test = test;

        public bool Stopped { get; private set; }

        public double Visit(int item, double maxDistance)
        {
            if (Stopped)
            {
                return -1;
            }

            TriangleShape shape = MeshTriangle(mesh, item, scale);
            if (!Test.Query.Reaches(shape.Minimum, shape.Maximum, Test.Limit) || Test.Test(shape, 0))
            {
                return Test.Limit;
            }

            Stopped = true;
            return -1;
        }
    }
}
