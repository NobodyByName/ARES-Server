namespace Ares.Raid.World.Collision;

/// <summary>
/// Distance between a segment (or point) and a convex shape with the Gilbert–Johnson–Keerthi algorithm: a simplex of
/// up to four points of the shapes' Minkowski difference is refined towards the origin, using the closest-feature
/// (Voronoi region) tests for segments, triangles and tetrahedra.
/// </summary>
internal static class Gjk
{
    private const int MaxIterations = 64;

    /// <summary>The search stops once a new support point improves the squared distance by less than this fraction.</summary>
    private const double RelativeTolerance = 1e-12;

    /// <summary>Squared distance below which the shapes count as intersecting.</summary>
    private const double IntersectionDistanceSquared = 1e-24;

    /// <summary>
    /// Returns the distance between the segment <c>queryCenter ± queryHalfAxis</c> and <paramref name="target"/>, 0 when
    /// they intersect, with the closest point of each. <paramref name="guess"/> is a hint for the direction from the
    /// target to the query (zero for none).
    /// </summary>
    public static double Distance<T>(Double3 queryCenter, Double3 queryHalfAxis, T target, Double3 guess, out Double3 queryPoint, out Double3 targetPoint)
        where T : struct, IConvexShape
    {
        Simplex simplex = default;
        Double3 v = guess.LengthSquared() > 0 ? guess : new Double3(1, 0, 0);
        Double3 firstQuery = QuerySupport(queryCenter, queryHalfAxis, -v);
        Double3 firstTarget = target.Support(v);
        simplex.Count = 1;
        simplex.Set(0, firstQuery - firstTarget, firstQuery, firstTarget, 1);
        v = firstQuery - firstTarget;
        double squared = v.LengthSquared();
        for (int iteration = 0; iteration < MaxIterations && squared > IntersectionDistanceSquared; iteration++)
        {
            Double3 supportQuery = QuerySupport(queryCenter, queryHalfAxis, -v);
            Double3 supportTarget = target.Support(v);
            Double3 w = supportQuery - supportTarget;
            if (squared - Double3.Dot(v, w) <= RelativeTolerance * squared || simplex.Contains(w))
            {
                break;
            }

            Simplex previous = simplex;
            simplex.Set(simplex.Count, w, supportQuery, supportTarget, 0);
            simplex.Count++;
            if (!simplex.Reduce(out Double3 next))
            {
                squared = 0;
                break;
            }

            double nextSquared = next.LengthSquared();
            if (nextSquared >= squared)
            {
                simplex = previous;
                break;
            }

            v = next;
            squared = nextSquared;
        }

        simplex.ClosestPoints(out queryPoint, out targetPoint);
        return squared <= IntersectionDistanceSquared ? 0 : Math.Sqrt(squared);
    }

    private static Double3 QuerySupport(Double3 center, Double3 halfAxis, Double3 direction) =>
        Double3.Dot(halfAxis, direction) >= 0 ? center + halfAxis : center - halfAxis;

    /// <summary>Up to four Minkowski-difference points with the query and target points they came from and their weights.</summary>
    private struct Simplex
    {
        private Double3 _w0, _w1, _w2, _w3;
        private Double3 _a0, _a1, _a2, _a3;
        private Double3 _b0, _b1, _b2, _b3;
        private double _l0, _l1, _l2, _l3;

        public int Count;

        public void Set(int index, Double3 w, Double3 a, Double3 b, double weight)
        {
            switch (index)
            {
                case 0:
                    (_w0, _a0, _b0, _l0) = (w, a, b, weight);
                    break;
                case 1:
                    (_w1, _a1, _b1, _l1) = (w, a, b, weight);
                    break;
                case 2:
                    (_w2, _a2, _b2, _l2) = (w, a, b, weight);
                    break;
                default:
                    (_w3, _a3, _b3, _l3) = (w, a, b, weight);
                    break;
            }
        }

        public readonly bool Contains(Double3 w) =>
            w == _w0 || (Count > 1 && w == _w1) || (Count > 2 && w == _w2) || (Count > 3 && w == _w3);

        public readonly void ClosestPoints(out Double3 query, out Double3 target)
        {
            query = _a0 * _l0;
            target = _b0 * _l0;
            if (Count > 1)
            {
                query += _a1 * _l1;
                target += _b1 * _l1;
            }

            if (Count > 2)
            {
                query += _a2 * _l2;
                target += _b2 * _l2;
            }

            if (Count <= 3)
            {
                return;
            }

            query += _a3 * _l3;
            target += _b3 * _l3;
        }

        /// <summary>
        /// Keeps the smallest sub-simplex holding the point closest to the origin, sets the weights and returns that
        /// point; false when the origin is inside the tetrahedron.
        /// </summary>
        public bool Reduce(out Double3 closest)
        {
            double l0;
            double l1;
            double l2;
            double l3 = 0;
            switch (Count)
            {
                case 2:
                    Segment(_w0, _w1, out l0, out l1);
                    l2 = 0;
                    break;
                case 3:
                    Triangle(_w0, _w1, _w2, out l0, out l1, out l2);
                    break;
                default:
                    if (!Tetrahedron(out l0, out l1, out l2, out l3))
                    {
                        _l0 = _l1 = _l2 = _l3 = 0.25;
                        closest = default;
                        return false;
                    }

                    break;
            }

            (_l0, _l1, _l2, _l3) = (l0, l1, l2, l3);
            Compact();
            closest = (_w0 * _l0) + (Count > 1 ? _w1 * _l1 : default) + (Count > 2 ? _w2 * _l2 : default);
            return true;
        }

        private bool Tetrahedron(out double l0, out double l1, out double l2, out double l3)
        {
            l0 = l1 = l2 = l3 = 0;
            double best = double.PositiveInfinity;
            bool outside = false;
            Face(_w0, _w1, _w2, _w3, 0, 1, 2, ref outside, ref best, ref l0, ref l1, ref l2, ref l3);
            Face(_w0, _w2, _w3, _w1, 0, 2, 3, ref outside, ref best, ref l0, ref l1, ref l2, ref l3);
            Face(_w0, _w3, _w1, _w2, 0, 3, 1, ref outside, ref best, ref l0, ref l1, ref l2, ref l3);
            Face(_w1, _w3, _w2, _w0, 1, 3, 2, ref outside, ref best, ref l0, ref l1, ref l2, ref l3);
            return outside;
        }

        /// <summary>Tests one face of the tetrahedron when the origin lies on its outer side (or the tetrahedron is flat).</summary>
        private static void Face(
            Double3 a,
            Double3 b,
            Double3 c,
            Double3 opposite,
            int ia,
            int ib,
            int ic,
            ref bool outside,
            ref double best,
            ref double l0,
            ref double l1,
            ref double l2,
            ref double l3)
        {
            Double3 normal = Double3.Cross(b - a, c - a);
            double originSide = -Double3.Dot(a, normal);
            double oppositeSide = Double3.Dot(opposite - a, normal);
            bool flat = oppositeSide * oppositeSide <= 1e-18 * normal.LengthSquared() * (opposite - a).LengthSquared();
            if (!flat && originSide * oppositeSide >= 0)
            {
                return;
            }

            outside = true;
            Triangle(a, b, c, out double la, out double lb, out double lc);
            double distance = ((a * la) + (b * lb) + (c * lc)).LengthSquared();
            if (distance >= best)
            {
                return;
            }

            best = distance;
            l0 = l1 = l2 = l3 = 0;
            Assign(ia, la, ref l0, ref l1, ref l2, ref l3);
            Assign(ib, lb, ref l0, ref l1, ref l2, ref l3);
            Assign(ic, lc, ref l0, ref l1, ref l2, ref l3);
        }

        private static void Assign(int index, double weight, ref double l0, ref double l1, ref double l2, ref double l3)
        {
            switch (index)
            {
                case 0:
                    l0 = weight;
                    break;
                case 1:
                    l1 = weight;
                    break;
                case 2:
                    l2 = weight;
                    break;
                default:
                    l3 = weight;
                    break;
            }
        }

        /// <summary>Drops the points with zero weight, keeping the order of the others.</summary>
        private void Compact()
        {
            Simplex source = this;
            int count = 0;
            for (int index = 0; index < source.Count; index++)
            {
                source.Get(index, out Double3 w, out Double3 a, out Double3 b, out double weight);
                if (weight > 0)
                {
                    Set(count++, w, a, b, weight);
                }
            }

            if (count == 0)
            {
                source.Get(0, out Double3 w, out Double3 a, out Double3 b, out _);
                Set(count++, w, a, b, 1);
            }

            Count = count;
        }

        private readonly void Get(int index, out Double3 w, out Double3 a, out Double3 b, out double weight)
        {
            (w, a, b, weight) = index switch
            {
                0 => (_w0, _a0, _b0, _l0),
                1 => (_w1, _a1, _b1, _l1),
                2 => (_w2, _a2, _b2, _l2),
                _ => (_w3, _a3, _b3, _l3),
            };
        }
    }

    /// <summary>Weights of the point of segment ab closest to the origin.</summary>
    private static void Segment(Double3 a, Double3 b, out double la, out double lb)
    {
        Double3 ab = b - a;
        double length = ab.LengthSquared();
        double t = length > 0 ? -Double3.Dot(a, ab) / length : 0;
        (la, lb) = t switch
        {
            <= 0 => (1, 0),
            >= 1 => (0, 1),
            _ => (1 - t, t),
        };
    }

    /// <summary>Weights of the point of triangle abc closest to the origin, by its Voronoi regions.</summary>
    private static void Triangle(Double3 a, Double3 b, Double3 c, out double la, out double lb, out double lc)
    {
        Double3 ab = b - a;
        Double3 ac = c - a;
        double d1 = -Double3.Dot(ab, a);
        double d2 = -Double3.Dot(ac, a);
        if (d1 <= 0 && d2 <= 0)
        {
            (la, lb, lc) = (1, 0, 0);
            return;
        }

        double d3 = -Double3.Dot(ab, b);
        double d4 = -Double3.Dot(ac, b);
        if (d3 >= 0 && d4 <= d3)
        {
            (la, lb, lc) = (0, 1, 0);
            return;
        }

        double vc = (d1 * d4) - (d3 * d2);
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
        {
            double t = d1 / (d1 - d3);
            (la, lb, lc) = (1 - t, t, 0);
            return;
        }

        double d5 = -Double3.Dot(ab, c);
        double d6 = -Double3.Dot(ac, c);
        if (d6 >= 0 && d5 <= d6)
        {
            (la, lb, lc) = (0, 0, 1);
            return;
        }

        double vb = (d5 * d2) - (d1 * d6);
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
        {
            double t = d2 / (d2 - d6);
            (la, lb, lc) = (1 - t, 0, t);
            return;
        }

        double va = (d3 * d6) - (d5 * d4);
        if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0)
        {
            double t = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            (la, lb, lc) = (0, 1 - t, t);
            return;
        }

        double sum = va + vb + vc;
        if (sum <= 0)
        {
            // A degenerate triangle: the nearest of its edges.
            DegenerateTriangle(a, b, c, out la, out lb, out lc);
            return;
        }

        double v = vb / sum;
        double w = vc / sum;
        (la, lb, lc) = (1 - v - w, v, w);
    }

    private static void DegenerateTriangle(Double3 a, Double3 b, Double3 c, out double la, out double lb, out double lc)
    {
        Segment(a, b, out double ab0, out double ab1);
        Segment(b, c, out double bc0, out double bc1);
        Segment(c, a, out double ca0, out double ca1);
        double onAb = ((a * ab0) + (b * ab1)).LengthSquared();
        double onBc = ((b * bc0) + (c * bc1)).LengthSquared();
        double onCa = ((c * ca0) + (a * ca1)).LengthSquared();
        if (onAb <= onBc && onAb <= onCa)
        {
            (la, lb, lc) = (ab0, ab1, 0);
        }
        else if (onBc <= onCa)
        {
            (la, lb, lc) = (0, bc0, bc1);
        }
        else
        {
            (la, lb, lc) = (ca1, 0, ca0);
        }
    }
}
