namespace Ares.Structs.Net.Replication;

/// <summary>Lookup over a game build's replication schemas.</summary>
public sealed class ReplicationCatalog
{
    private const int MaxAncestry = 64;

    /// <summary>Creates a catalog. Paths must be unique.</summary>
    public ReplicationCatalog(
        IEnumerable<PropertyDefinition> properties,
        IEnumerable<ClassSchema> classes,
        IEnumerable<ReferenceClass> referenceClasses)
    {
        Properties = properties.ToDictionary(property => property.Path, StringComparer.Ordinal);
        Classes = classes.ToDictionary(schema => schema.Path, StringComparer.Ordinal);
        ReferenceClasses = referenceClasses.ToDictionary(reference => reference.Path, StringComparer.Ordinal);
        string? both = ReferenceClasses.Keys.FirstOrDefault(Classes.ContainsKey);
        if (both is not null)
        {
            throw new ArgumentException($"{both} is both a schema class and a reference class.", nameof(referenceClasses));
        }
    }

    /// <summary>All property definitions by path.</summary>
    public IReadOnlyDictionary<string, PropertyDefinition> Properties { get; }

    /// <summary>All classes with replication schemas by path.</summary>
    public IReadOnlyDictionary<string, ClassSchema> Classes { get; }

    /// <summary>Classes known only by ancestry, by path.</summary>
    public IReadOnlyDictionary<string, ReferenceClass> ReferenceClasses { get; }

    /// <summary>The schema of a class.</summary>
    /// <exception cref="KeyNotFoundException">The class has no replication schema.</exception>
    public ClassSchema Class(string path) =>
        Classes.TryGetValue(path, out ClassSchema? schema)
            ? schema
            : throw new KeyNotFoundException($"No replication schema for class {path}.");

    /// <summary>A property definition.</summary>
    /// <exception cref="KeyNotFoundException">The property is unknown.</exception>
    public PropertyDefinition Property(string path) =>
        Properties.TryGetValue(path, out PropertyDefinition? property)
            ? property
            : throw new KeyNotFoundException($"Unknown property {path}.");

    /// <summary>The parent class path of a schema or reference class.</summary>
    /// <exception cref="KeyNotFoundException">The class is unknown.</exception>
    public string? SuperOf(string path)
    {
        if (Classes.TryGetValue(path, out ClassSchema? schema))
        {
            return schema.Super;
        }

        return ReferenceClasses.TryGetValue(path, out ReferenceClass? reference)
            ? reference.Super
            : throw new KeyNotFoundException($"Class {path} has unknown ancestry.");
    }

    /// <summary>Whether a class is <paramref name="expected"/> or derives from it.</summary>
    /// <exception cref="KeyNotFoundException">The ancestry reaches an unknown class.</exception>
    public bool IsA(string path, string expected)
    {
        string? current = path;
        for (int depth = 0; current is not null; depth++)
        {
            if (depth >= MaxAncestry)
            {
                throw new InvalidOperationException($"Class ancestry of {path} is too deep or cyclic.");
            }

            if (current == expected)
            {
                return true;
            }

            current = SuperOf(current);
        }

        return false;
    }

    /// <summary>
    /// All replication records of a class, inherited ones first (the engine's <c>ClassReps</c>).
    /// </summary>
    public IReadOnlyList<ReplicationRecord> Records(string classPath)
    {
        List<ClassSchema> chain = [];
        for (ClassSchema? schema = Class(classPath); schema is not null; schema = schema.Super is null ? null : Class(schema.Super))
        {
            if (chain.Count >= MaxAncestry)
            {
                throw new InvalidOperationException($"Class ancestry of {classPath} is too deep or cyclic.");
            }

            chain.Add(schema);
        }

        List<ReplicationRecord> records = [];
        for (int index = chain.Count - 1; index >= 0; index--)
        {
            foreach (string path in chain[index].OwnProperties)
            {
                int count = Property(path).ArrayDim;
                for (int element = 0; element < count; element++)
                {
                    records.Add(new ReplicationRecord(path, element));
                }
            }
        }

        return records;
    }
}
