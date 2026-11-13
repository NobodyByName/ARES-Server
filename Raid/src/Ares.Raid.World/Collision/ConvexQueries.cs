namespace Ares.Raid.World.Collision;

/// <summary>
/// A capsule query in one shape's frame: the segment <c>Center ± HalfAxis</c> rounded by <c>Radius</c>, moving by
/// <c>Motion</c> over the sweep time 0 to 1. A sphere has a zero half axis, a ray also a zero radius.
/// </summary>
internal readonly record struct CapsuleQuery(Double3 Center, Double3 HalfAxis, double Radius, Double3 Motion)
{
    /// <summary>Half size of the query's axis-aligned bounds, widened by the contact tolerance.</summary>
    public Double3 Extent => HalfAxis.Abs() + new Double3(Radius, Radius, Radius) + new Double3(ConvexQueries.ContactTolerance, ConvexQueries.ContactTolerance, ConvexQueries.ContactTolerance);

    /// <summary>
    /// Whether the query's swept bounds reach the box from <paramref name="minimum"/> to <paramref name="maximum"/>
    /// before time <paramref name="maxTime"/>.
    /// </summary>
    public bool Reaches(Double3 minimum, Double3 maximum, double maxTime) =>
        new WorldBox(minimum, maximum).Inflated(Extent).Clip(Center, WorldBox.InverseDirection(Motion), maxTime, out _, out _);
}

/// <summary>
/// The first contact of a swept query with one shape, in the shape's frame.
/// </summary>
/// <param name="Time">Sweep time of the contact, 0 to 1.</param>
/// <param name="Point">Contact point on the shape's surface.</param>
/// <param name="Normal">Unit contact normal, from the shape towards the query.</param>
/// <param name="Depth">How deep a query that starts inside the shape is; 0 otherwise.</param>
/// <param name="StartPenetrating">Whether the query starts inside the shape.</param>
internal readonly record struct ShapeContact(double Time, Double3 Point, Double3 Normal, double Depth, bool StartPenetrating)
{
    /// <summary>
    /// Whether this contact should replace <paramref name="other"/> as a sweep's result: a starting overlap beats any
    /// later contact and a deeper one beats a shallower one; otherwise the earlier contact wins.
    /// </summary>
    public bool IsBetterThan(in ShapeContact other) =>
        StartPenetrating ? !other.StartPenetrating || Depth > other.Depth : !other.StartPenetrating && Time < other.Time;
}

/// <summary>
/// Sweep, overlap and penetration tests of a capsule (or sphere, or point) against one convex shape whose core
/// <see cref="IConvexShape"/> may be rounded by a radius. Distances come from <see cref="Gjk"/>. Penetration depth is
/// exact: the overlap of the radii when the cores are apart, otherwise the smallest push along the facet normals of
/// the cores' Minkowski difference. Only penetration deeper than <see cref="ContactTolerance"/> counts, so resting
/// and sliding contacts, and the seams between neighbouring shapes, do not block. Sweeps use conservative
/// advancement: the query moves forward by its clearance divided by its approach speed, which never passes the first
/// contact of convex shapes, until it is within <see cref="ConvergenceTolerance"/> of that counting depth; the
/// reported time is then extrapolated back to the touching point.
/// </summary>
internal static class ConvexQueries
{
    /// <summary>Penetration in centimetres that counts as overlap; less is touching.</summary>
    public const double ContactTolerance = 1e-3;

    /// <summary>How close, in centimetres, a sweep's advancement gets to the contact before it stops.</summary>
    private const double ConvergenceTolerance = 1e-4;

    /// <summary>Core distance below which the closest points no longer give a reliable normal.</summary>
    private const double SeparationEpsilon = 1e-6;

    private const int MaxAdvanceSteps = 64;

    /// <summary>
    /// Sweeps the query against a shape and returns its first contact before <paramref name="maxTime"/>: the time it
    /// touches the shape on a motion that goes on to penetrate it deeper than the tolerance. A starting penetration
    /// deeper than the tolerance is always reported, whatever the motion.
    /// </summary>
    public static bool Sweep<T>(in T target, double targetRadius, in CapsuleQuery query, double maxTime, out ShapeContact contact)
        where T : struct, IConvexShape
    {
        contact = default;
        double radius = query.Radius + targetRadius;
        double distance = Gjk.Distance(query.Center, query.HalfAxis, target, default, out Double3 queryPoint, out Double3 targetPoint);
        double depth = Depth(target, query.Center, query.HalfAxis, radius, distance, queryPoint, targetPoint, out Double3 normal);
        if (depth > ContactTolerance)
        {
            Double3 deepest = distance > SeparationEpsilon ? queryPoint
                : query.Center + (Double3.Dot(query.HalfAxis, normal) <= 0 ? query.HalfAxis : -query.HalfAxis);
            contact = new ShapeContact(0, deepest - (normal * query.Radius), normal, depth, true);
            return true;
        }

        // The depth that counts as a hit; a radius thinner than the tolerance counts on touching.
        double counting = Math.Min(ContactTolerance, radius);
        double time = 0;
        for (int step = 0; ; step++)
        {
            double approach = -Double3.Dot(query.Motion, normal);
            double clearance = counting - depth;
            if (clearance <= ConvergenceTolerance)
            {
                if (approach <= 0)
                {
                    return false;
                }

                double contactTime = Math.Max(0, time - (depth / approach));
                if (contactTime >= maxTime)
                {
                    return false;
                }

                Double3 surface = queryPoint + (query.Motion * (contactTime - time)) - (normal * query.Radius);
                contact = new ShapeContact(contactTime, surface, normal, 0, false);
                return true;
            }

            if (approach <= 0)
            {
                return false;
            }

            time += (clearance - (ConvergenceTolerance * 0.5)) / approach;
            if (time >= maxTime)
            {
                return false;
            }

            if (step == MaxAdvanceSteps)
            {
                // Not converged (a long grazing approach): report the contact where the query still is clear.
                contact = new ShapeContact(time, targetPoint + (normal * targetRadius), normal, 0, false);
                return true;
            }

            Double3 center = query.Center + (query.Motion * time);
            distance = Gjk.Distance(center, query.HalfAxis, target, normal, out queryPoint, out targetPoint);
            depth = Depth(target, center, query.HalfAxis, radius, distance, queryPoint, targetPoint, out normal);
        }
    }

    /// <summary>Whether the query at its start penetrates the shape deeper than the tolerance.</summary>
    public static bool Overlaps<T>(in T target, double targetRadius, Double3 center, Double3 halfAxis, double radius)
        where T : struct, IConvexShape
    {
        double combined = radius + targetRadius;
        double distance = Gjk.Distance(center, halfAxis, target, default, out _, out _);
        return combined - distance > ContactTolerance
            || (distance <= SeparationEpsilon && SeparatingAxisDepth(target, center, halfAxis, combined, out _) > ContactTolerance);
    }

    /// <summary>
    /// When the query penetrates the shape deeper than the tolerance, returns true with the shortest push that
    /// separates them: a unit <paramref name="direction"/> and its <paramref name="depth"/>.
    /// </summary>
    public static bool Penetration<T>(in T target, double targetRadius, Double3 center, Double3 halfAxis, double radius, out Double3 direction, out double depth)
        where T : struct, IConvexShape
    {
        double combined = radius + targetRadius;
        double distance = Gjk.Distance(center, halfAxis, target, default, out Double3 queryPoint, out Double3 targetPoint);
        if (combined - distance <= ContactTolerance && distance > SeparationEpsilon)
        {
            direction = default;
            depth = 0;
            return false;
        }

        depth = Depth(target, center, halfAxis, combined, distance, queryPoint, targetPoint, out direction);
        return depth > ContactTolerance;
    }

    /// <summary>
    /// Penetration depth of the rounded query into the rounded shape (negative: the gap between them) and the unit
    /// direction that pushes the query out, from the shape towards the query.
    /// </summary>
    private static double Depth<T>(in T target, Double3 center, Double3 halfAxis, double radius, double distance, Double3 queryPoint, Double3 targetPoint, out Double3 direction)
        where T : struct, IConvexShape
    {
        if (!(distance > SeparationEpsilon))
        {
            return SeparatingAxisDepth(target, center, halfAxis, radius, out direction);
        }

        direction = (queryPoint - targetPoint) * (1 / distance);
        return radius - distance;
    }

    /// <summary>
    /// The smallest push, over the candidate axes of the shape against the query segment, that separates the query
    /// rounded by <paramref name="radius"/> from the shape. Each candidate gives a push that does separate them; the
    /// candidates include the facet normals of the cores' Minkowski difference, so the smallest is exact when the
    /// cores intersect.
    /// </summary>
    private static double SeparatingAxisDepth<T>(T target, Double3 center, Double3 halfAxis, double radius, out Double3 direction)
        where T : struct, IConvexShape
    {
        int capacity = target.PenetrationAxisCount;
        Span<Double3> axes = capacity <= 64 ? stackalloc Double3[64] : new Double3[capacity];
        int count = target.PenetrationAxes(halfAxis, axes);
        double best = double.PositiveInfinity;
        direction = new Double3(0, 0, 1);
        for (int index = 0; index < count; index++)
        {
            double length = axes[index].Length();
            if (!(length > 1e-15))
            {
                continue;
            }

            Double3 axis = axes[index] * (1 / length);
            double reach = Math.Abs(Double3.Dot(halfAxis, axis)) + radius;
            double offset = Double3.Dot(center, axis);
            double forward = Double3.Dot(target.Support(axis), axis) - offset + reach;
            double backward = -Double3.Dot(target.Support(-axis), axis) + offset + reach;
            if (forward < best)
            {
                best = forward;
                direction = axis;
            }

            if (!(backward < best))
            {
                continue;
            }

            best = backward;
            direction = -axis;
        }

        return double.IsPositiveInfinity(best) ? radius : best;
    }
}
