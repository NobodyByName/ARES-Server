namespace Ares.Raid.World.Collision;

/// <summary>An axis-aligned box in world space.</summary>
internal readonly record struct WorldBox(Double3 Minimum, Double3 Maximum)
{
    public Double3 Center => (Minimum + Maximum) * 0.5;

    public static WorldBox Union(WorldBox left, WorldBox right) => new(
        new Double3(Math.Min(left.Minimum.X, right.Minimum.X), Math.Min(left.Minimum.Y, right.Minimum.Y), Math.Min(left.Minimum.Z, right.Minimum.Z)),
        new Double3(Math.Max(left.Maximum.X, right.Maximum.X), Math.Max(left.Maximum.Y, right.Maximum.Y), Math.Max(left.Maximum.Z, right.Maximum.Z)));

    /// <summary>The box grown by <paramref name="halfExtent"/> on each side.</summary>
    public WorldBox Inflated(Double3 halfExtent) => new(Minimum - halfExtent, Maximum + halfExtent);

    /// <summary>Whether the boxes share a point.</summary>
    public bool Overlaps(WorldBox other) =>
        Minimum.X <= other.Maximum.X && Maximum.X >= other.Minimum.X &&
        Minimum.Y <= other.Maximum.Y && Maximum.Y >= other.Minimum.Y &&
        Minimum.Z <= other.Maximum.Z && Maximum.Z >= other.Minimum.Z;

    /// <summary>Half the surface area, the cost weight of the surface area heuristic.</summary>
    public double HalfArea()
    {
        Double3 size = Maximum - Minimum;
        return (size.X * size.Y) + (size.Y * size.Z) + (size.Z * size.X);
    }

    /// <summary>
    /// Where a ray enters the box, clipped to [0, <paramref name="maxDistance"/>], or <see cref="double.PositiveInfinity"/>
    /// when it misses. <paramref name="inverseDirection"/> holds the reciprocal of each direction component, with
    /// zero components replaced by a huge value of either sign.
    /// </summary>
    public double Entry(Double3 origin, Double3 inverseDirection, double maxDistance) =>
        Clip(origin, inverseDirection, maxDistance, out double near, out _) ? near : double.PositiveInfinity;

    /// <summary>The part of a ray inside the box, within [0, <paramref name="maxDistance"/>]; see <see cref="Entry"/>.</summary>
    public bool Clip(Double3 origin, Double3 inverseDirection, double maxDistance, out double near, out double far)
    {
        double x0 = (Minimum.X - origin.X) * inverseDirection.X;
        double x1 = (Maximum.X - origin.X) * inverseDirection.X;
        double y0 = (Minimum.Y - origin.Y) * inverseDirection.Y;
        double y1 = (Maximum.Y - origin.Y) * inverseDirection.Y;
        double z0 = (Minimum.Z - origin.Z) * inverseDirection.Z;
        double z1 = (Maximum.Z - origin.Z) * inverseDirection.Z;
        near = Math.Max(Math.Max(Math.Min(x0, x1), Math.Min(y0, y1)), Math.Max(Math.Min(z0, z1), 0));
        far = Math.Min(Math.Min(Math.Max(x0, x1), Math.Max(y0, y1)), Math.Min(Math.Max(z0, z1), maxDistance));
        return near <= far;
    }

    /// <summary>The reciprocal of each direction component, with zero components replaced by a huge value.</summary>
    public static Double3 InverseDirection(Double3 direction) => new(Reciprocal(direction.X), Reciprocal(direction.Y), Reciprocal(direction.Z));

    private static double Reciprocal(double value) => value == 0 ? 1e300 : 1 / value;
}

/// <summary>Receives the items a ray reaches, nearest node first.</summary>
internal interface IRayVisitor
{
    /// <summary>Tests one item and returns the distance the ray may still travel (the closest hit so far).</summary>
    double Visit(int item, double maxDistance);
}

/// <summary>Receives the items whose boxes overlap a query box.</summary>
internal interface IOverlapVisitor
{
    /// <summary>Tests one item and returns whether the query should go on.</summary>
    bool Visit(int item);
}

/// <summary>
/// A static bounding volume hierarchy over item boxes, built once with a binned surface area heuristic. Leaves hold
/// item indices; nodes are stored depth first, so a node's first child directly follows it.
/// </summary>
internal sealed class BoundingVolumeHierarchy
{
    private const int LeafSize = 2;
    private const int MaxLeafSize = 8;
    private const int BinCount = 16;
    private const int SahDepthLimit = 40;
    private const int StackSize = 64;

    private readonly Node[] _nodes;
    private readonly int[] _items;
    private readonly int _stackSize;

    private BoundingVolumeHierarchy(Node[] nodes, int[] items, int depth)
    {
        _nodes = nodes;
        _items = items;

        // A traversal holds at most one pending sibling per level above the current node, plus its two children.
        _stackSize = depth + 2;
    }

    /// <summary>Builds the hierarchy over item boxes; item i is reported as index i.</summary>
    public static BoundingVolumeHierarchy Build(ReadOnlySpan<WorldBox> boxes)
    {
        int[] items = new int[boxes.Length];
        Double3[] centers = new Double3[boxes.Length];
        for (int index = 0; index < items.Length; index++)
        {
            items[index] = index;
            centers[index] = boxes[index].Center;
        }

        List<Node> nodes = new(Math.Max(1, boxes.Length));
        Builder builder = new([.. boxes], centers, items, nodes);
        if (boxes.Length > 0)
        {
            builder.BuildNode(0, items.Length, 0);
        }

        return new BoundingVolumeHierarchy([.. nodes], items, builder.Depth);
    }

    /// <summary>
    /// Visits the items whose boxes a ray reaches within <paramref name="maxDistance"/>, skipping nodes beyond the
    /// distance the visitor returns. Distances count in lengths of <paramref name="direction"/>, which need not be a unit vector.
    /// </summary>
    public void Raycast<TVisitor>(Double3 origin, Double3 direction, double maxDistance, ref TVisitor visitor)
        where TVisitor : struct, IRayVisitor =>
        Sweep(origin, direction, default, maxDistance, ref visitor);

    /// <summary>
    /// Visits the items whose boxes a box of half size <paramref name="halfExtent"/> touches while its centre moves
    /// along a ray, nearest first, as <see cref="Raycast"/> does for a point.
    /// </summary>
    public void Sweep<TVisitor>(Double3 origin, Double3 direction, Double3 halfExtent, double maxDistance, ref TVisitor visitor)
        where TVisitor : struct, IRayVisitor
    {
        if (_nodes.Length == 0)
        {
            return;
        }

        Double3 inverse = WorldBox.InverseDirection(direction);
        if (double.IsPositiveInfinity(_nodes[0].Bounds.Inflated(halfExtent).Entry(origin, inverse, maxDistance)))
        {
            return;
        }

        Span<int> stack = _stackSize <= StackSize ? stackalloc int[StackSize] : new int[_stackSize];
        Span<double> entries = _stackSize <= StackSize ? stackalloc double[StackSize] : new double[_stackSize];
        int depth = 0;
        stack[depth] = 0;
        entries[depth++] = 0;
        while (depth > 0)
        {
            depth--;
            if (entries[depth] > maxDistance)
            {
                continue;
            }

            int nodeIndex = stack[depth];
            ref readonly Node node = ref _nodes[nodeIndex];
            if (node.Count > 0)
            {
                for (int index = node.Start; index < node.Start + node.Count; index++)
                {
                    maxDistance = visitor.Visit(_items[index], maxDistance);
                }

                continue;
            }

            int first = nodeIndex + 1;
            int second = node.Start;
            double firstEntry = _nodes[first].Bounds.Inflated(halfExtent).Entry(origin, inverse, maxDistance);
            double secondEntry = _nodes[second].Bounds.Inflated(halfExtent).Entry(origin, inverse, maxDistance);
            if (firstEntry > secondEntry)
            {
                (first, second) = (second, first);
                (firstEntry, secondEntry) = (secondEntry, firstEntry);
            }

            if (!double.IsPositiveInfinity(secondEntry))
            {
                stack[depth] = second;
                entries[depth++] = secondEntry;
            }

            if (double.IsPositiveInfinity(firstEntry))
            {
                continue;
            }

            stack[depth] = first;
            entries[depth++] = firstEntry;
        }
    }

    /// <summary>Visits the items whose boxes overlap <paramref name="box"/> until the visitor asks to stop.</summary>
    public void Overlap<TVisitor>(WorldBox box, ref TVisitor visitor)
        where TVisitor : struct, IOverlapVisitor
    {
        if (_nodes.Length == 0 || !_nodes[0].Bounds.Overlaps(box))
        {
            return;
        }

        Span<int> stack = _stackSize <= StackSize ? stackalloc int[StackSize] : new int[_stackSize];
        int depth = 0;
        stack[depth++] = 0;
        while (depth > 0)
        {
            int nodeIndex = stack[--depth];
            ref readonly Node node = ref _nodes[nodeIndex];
            if (node.Count > 0)
            {
                for (int index = node.Start; index < node.Start + node.Count; index++)
                {
                    if (!visitor.Visit(_items[index]))
                    {
                        return;
                    }
                }

                continue;
            }

            if (_nodes[node.Start].Bounds.Overlaps(box))
            {
                stack[depth++] = node.Start;
            }

            if (_nodes[nodeIndex + 1].Bounds.Overlaps(box))
            {
                stack[depth++] = nodeIndex + 1;
            }
        }
    }

    /// <summary>A leaf (<see cref="Count"/> items from <see cref="Start"/>) or an inner node (second child at <see cref="Start"/>).</summary>
    private readonly record struct Node(WorldBox Bounds, int Start, int Count);

    private sealed class Builder(WorldBox[] boxes, Double3[] centers, int[] items, List<Node> nodes)
    {
        /// <summary>Depth of the deepest node built so far (the root is at 0).</summary>
        public int Depth { get; private set; }

        public void BuildNode(int start, int count, int depth)
        {
            Depth = Math.Max(Depth, depth);
            int nodeIndex = nodes.Count;
            nodes.Add(default);
            WorldBox bounds = boxes[items[start]];
            Double3 centerMinimum = centers[items[start]];
            Double3 centerMaximum = centerMinimum;
            for (int index = start + 1; index < start + count; index++)
            {
                bounds = WorldBox.Union(bounds, boxes[items[index]]);
                Double3 center = centers[items[index]];
                centerMinimum = new Double3(Math.Min(centerMinimum.X, center.X), Math.Min(centerMinimum.Y, center.Y), Math.Min(centerMinimum.Z, center.Z));
                centerMaximum = new Double3(Math.Max(centerMaximum.X, center.X), Math.Max(centerMaximum.Y, center.Y), Math.Max(centerMaximum.Z, center.Z));
            }

            Double3 extent = centerMaximum - centerMinimum;
            int axis = extent.X >= extent.Y && extent.X >= extent.Z ? 0 : extent.Y >= extent.Z ? 1 : 2;
            double axisMinimum = Component(centerMinimum, axis);
            double axisExtent = Component(extent, axis);
            if (count <= LeafSize || axisExtent <= 0)
            {
                nodes[nodeIndex] = new Node(bounds, start, count);
                return;
            }

            int split = depth < SahDepthLimit ? SahSplit(start, count, axis, axisMinimum, axisExtent, bounds) : -1;
            switch (split)
            {
                case 0:
                    nodes[nodeIndex] = new Node(bounds, start, count);
                    return;
                case < 0:
                    Array.Sort(items, start, count, Comparer<int>.Create((left, right) => Component(centers[left], axis).CompareTo(Component(centers[right], axis))));
                    split = count / 2;
                    break;
            }

            BuildNode(start, split, depth + 1);
            int second = nodes.Count;
            BuildNode(start + split, count - split, depth + 1);
            nodes[nodeIndex] = new Node(bounds, second, 0);
        }

        /// <summary>
        /// Partitions the range by the cheapest bin boundary and returns the size of the first part, 0 when a leaf is
        /// cheaper, or −1 when no boundary separates the items.
        /// </summary>
        private int SahSplit(int start, int count, int axis, double axisMinimum, double axisExtent, WorldBox bounds)
        {
            Span<int> binCounts = stackalloc int[BinCount];
            WorldBox[] binBounds = new WorldBox[BinCount];
            double scale = BinCount / axisExtent;
            for (int index = start; index < start + count; index++)
            {
                int bin = Bin(items[index], axis, axisMinimum, scale);
                binBounds[bin] = binCounts[bin] == 0 ? boxes[items[index]] : WorldBox.Union(binBounds[bin], boxes[items[index]]);
                binCounts[bin]++;
            }

            Span<double> leftCosts = stackalloc double[BinCount - 1];
            WorldBox accumulated = default;
            int accumulatedCount = 0;
            for (int bin = 0; bin < BinCount - 1; bin++)
            {
                if (binCounts[bin] > 0)
                {
                    accumulated = accumulatedCount == 0 ? binBounds[bin] : WorldBox.Union(accumulated, binBounds[bin]);
                    accumulatedCount += binCounts[bin];
                }

                leftCosts[bin] = accumulatedCount == 0 ? double.PositiveInfinity : accumulated.HalfArea() * accumulatedCount;
            }

            double bestCost = double.PositiveInfinity;
            int bestBin = -1;
            accumulatedCount = 0;
            for (int bin = BinCount - 1; bin > 0; bin--)
            {
                if (binCounts[bin] > 0)
                {
                    accumulated = accumulatedCount == 0 ? binBounds[bin] : WorldBox.Union(accumulated, binBounds[bin]);
                    accumulatedCount += binCounts[bin];
                }

                if (accumulatedCount == 0 || accumulatedCount == count)
                {
                    continue;
                }

                double cost = leftCosts[bin - 1] + (accumulated.HalfArea() * accumulatedCount);
                if (!(cost < bestCost))
                {
                    continue;
                }

                bestCost = cost;
                bestBin = bin;
            }

            if (bestBin < 0)
            {
                return -1;
            }

            double area = bounds.HalfArea();
            if (count <= MaxLeafSize && area > 0 && (bestCost / area) + 1 >= count)
            {
                return 0;
            }

            int low = start;
            int high = start + count - 1;
            while (low <= high)
            {
                if (Bin(items[low], axis, axisMinimum, scale) < bestBin)
                {
                    low++;
                }
                else
                {
                    (items[low], items[high]) = (items[high], items[low]);
                    high--;
                }
            }

            int split = low - start;
            return split == 0 || split == count ? -1 : split;
        }

        private int Bin(int item, int axis, double axisMinimum, double scale) =>
            Math.Clamp((int)((Component(centers[item], axis) - axisMinimum) * scale), 0, BinCount - 1);

        private static double Component(Double3 value, int axis) => axis switch
        {
            0 => value.X,
            1 => value.Y,
            _ => value.Z,
        };
    }
}
