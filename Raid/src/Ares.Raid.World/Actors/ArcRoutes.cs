using Ares.Common.Game.Enemies;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Actors;

/// <summary>
/// The home points of a spawner's ARCs, spaced along its route (or the spawner itself when it has none), and the route
/// they lie on, which the ARCs fly along from one home point to the next.
/// </summary>
/// <param name="Points">The home points, in route order.</param>
/// <param name="Closed">Whether the route loops, so the last home point leads on to the first.</param>
public sealed record ArcHomePoints(IReadOnlyList<Vector3D> Points, bool Closed)
{
    /// <summary>The route as a line through its points (a loop ends at its first point again); the spawner alone without a route.</summary>
    public IReadOnlyList<Vector3D> Line { get; init; } = Points;

    /// <summary>Distance of each line point from the line's start, in centimetres.</summary>
    public IReadOnlyList<double> LineDistances { get; init; } = [.. Points.Select(_ => 0.0)];

    /// <summary>Distance of each home point along the line, in centimetres.</summary>
    public IReadOnlyList<double> Distances { get; init; } = [.. Points.Select(_ => 0.0)];

    /// <summary>Length of the line, in centimetres.</summary>
    public double Length => LineDistances.Count == 0 ? 0 : LineDistances[^1];

    /// <summary>The home point nearest a location.</summary>
    /// <param name="location">The location.</param>
    /// <returns>Its index.</returns>
    public int Nearest(Vector3D location)
    {
        int nearest = 0;
        double best = double.PositiveInfinity;
        for (int index = 0; index < Points.Count; index++)
        {
            double distance = ArcRoutes.Distance(Points[index], location);
            if (distance < best)
            {
                (nearest, best) = (index, distance);
            }
        }

        return nearest;
    }

    /// <summary>The point of the line at a distance along it; a loop wraps around, an open line stops at its ends.</summary>
    /// <param name="along">Distance from the line's start, in centimetres.</param>
    /// <returns>The point.</returns>
    public Vector3D PointAt(double along)
    {
        double length = Length;
        if (Line.Count == 1 || length <= 0)
        {
            return Line[0];
        }

        along = Closed ? ((along % length) + length) % length : Math.Clamp(along, 0, length);
        int segment = 1;
        while (segment < LineDistances.Count - 1 && LineDistances[segment] < along)
        {
            segment++;
        }

        Vector3D from = Line[segment - 1];
        Vector3D to = Line[segment];
        double span = LineDistances[segment] - LineDistances[segment - 1];
        double share = span > 0 ? (along - LineDistances[segment - 1]) / span : 0;
        return new Vector3D(from.X + ((to.X - from.X) * share), from.Y + ((to.Y - from.Y) * share), from.Z + ((to.Z - from.Z) * share));
    }
}

/// <summary>
/// Lays out the home points of a spawner's ARCs along a route. The ARCs fly the route as a smooth curve through its
/// points (centripetal Catmull-Rom, sampled every <see cref="SampleSpacing"/>, at most <see cref="MaxSamples"/> per
/// segment), not as straight lines between them.
/// </summary>
public static class ArcRoutes
{
    /// <summary>Distance between the curve's samples, in centimetres.</summary>
    public const double SampleSpacing = 300;

    /// <summary>Most samples per segment between two route points.</summary>
    private const int MaxSamples = 16;

    /// <summary>
    /// The home points along a route's curve, <paramref name="spacing"/> apart from its first point (and its closing
    /// segment for a loop), or the spawner's own location for a spawner without a route.
    /// </summary>
    /// <param name="spawner">The spawner.</param>
    /// <param name="route">Its route, or null.</param>
    /// <param name="spacing">Distance between home points, in centimetres.</param>
    /// <returns>The home points.</returns>
    public static ArcHomePoints HomePoints(ArcSpawner spawner, SplinePathPlacement? route, double spacing)
    {
        ArgumentNullException.ThrowIfNull(spawner);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spacing);
        if (route is null || route.Points.Length == 0)
        {
            return new ArcHomePoints([spawner.Location], false);
        }

        List<Vector3D> line = Curve(route.Points, route.Closed);

        List<double> lineDistances = [0];
        List<Vector3D> homes = [line[0]];
        List<double> distances = [0];
        double carried = 0;
        for (int index = 1; index < line.Count; index++)
        {
            Vector3D from = line[index - 1];
            Vector3D to = line[index];
            double length = Distance(from, to);
            double along = spacing - carried;
            while (along <= length)
            {
                double share = along / length;
                homes.Add(new Vector3D(from.X + ((to.X - from.X) * share), from.Y + ((to.Y - from.Y) * share), from.Z + ((to.Z - from.Z) * share)));
                distances.Add(lineDistances[^1] + along);
                along += spacing;
            }

            carried = length - (along - spacing);
            lineDistances.Add(lineDistances[^1] + length);
        }

        // A loop's last home point would sit on its first.
        if (!route.Closed || homes.Count <= 1 || Distance(homes[^1], homes[0]) >= spacing / 2)
        {
            return new ArcHomePoints(homes, route.Closed) { Line = line, LineDistances = lineDistances, Distances = distances };
        }

        homes.RemoveAt(homes.Count - 1);
        distances.RemoveAt(distances.Count - 1);
        return new ArcHomePoints(homes, route.Closed) { Line = line, LineDistances = lineDistances, Distances = distances };
    }

    /// <summary>
    /// A centripetal Catmull-Rom curve (alpha 0.5) through points, as a line of samples; a loop's line ends at its first
    /// point again. An open curve's ends continue straight from their neighbours.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <param name="closed">Whether the curve loops.</param>
    /// <returns>The samples.</returns>
    public static List<Vector3D> Curve(IReadOnlyList<Vector3D> points, bool closed)
    {
        ArgumentNullException.ThrowIfNull(points);
        int count = points.Count;
        if (count < 2)
        {
            return [.. points];
        }

        List<Vector3D> line = [];
        int segments = closed ? count : count - 1;
        for (int segment = 0; segment < segments; segment++)
        {
            Vector3D p1 = points[segment];
            Vector3D p2 = points[(segment + 1) % count];
            Vector3D p0 = closed || segment > 0 ? points[(segment - 1 + count) % count] : Mirror(p1, p2);
            Vector3D p3 = closed || segment + 2 < count ? points[(segment + 2) % count] : Mirror(p2, p1);
            int samples = Math.Clamp((int)Math.Ceiling(Distance(p1, p2) / SampleSpacing), 1, MaxSamples);
            for (int sample = 0; sample < samples; sample++)
            {
                line.Add(CatmullRom(p0, p1, p2, p3, (double)sample / samples));
            }
        }

        line.Add(closed ? points[0] : points[^1]);
        return line;
    }

    // A point of the centripetal Catmull-Rom segment from p1 to p2 (Barry-Goldman), share 0 at p1 and 1 at p2.
    private static Vector3D CatmullRom(Vector3D p0, Vector3D p1, Vector3D p2, Vector3D p3, double share)
    {
        double t1 = Math.Sqrt(Distance(p0, p1));
        double t2 = t1 + Math.Sqrt(Distance(p1, p2));
        double t3 = t2 + Math.Sqrt(Distance(p2, p3));
        if (t1 <= 0 || t2 <= t1 || t3 <= t2)
        {
            return Lerp(p1, p2, share);
        }

        double t = t1 + ((t2 - t1) * share);
        Vector3D a1 = Lerp(p0, p1, t / t1);
        Vector3D a2 = Lerp(p1, p2, (t - t1) / (t2 - t1));
        Vector3D a3 = Lerp(p2, p3, (t - t2) / (t3 - t2));
        Vector3D b1 = Lerp(a1, a2, t / t2);
        Vector3D b2 = Lerp(a2, a3, (t - t1) / (t3 - t1));
        return Lerp(b1, b2, (t - t1) / (t2 - t1));
    }

    private static Vector3D Lerp(Vector3D from, Vector3D to, double share) =>
        new(from.X + ((to.X - from.X) * share), from.Y + ((to.Y - from.Y) * share), from.Z + ((to.Z - from.Z) * share));

    // The point as far beyond a point as another lies before it.
    private static Vector3D Mirror(Vector3D at, Vector3D other) => new((2 * at.X) - other.X, (2 * at.Y) - other.Y, (2 * at.Z) - other.Z);

    /// <summary>Distance between two points, in centimetres.</summary>
    public static double Distance(Vector3D a, Vector3D b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)) + ((a.Z - b.Z) * (a.Z - b.Z)));
}
