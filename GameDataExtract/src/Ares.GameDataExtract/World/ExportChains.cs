using Ares.GameDataExtract.Packages;

namespace Ares.GameDataExtract.World;

/// <summary>Outer chains of exports: the names from the package's top-level export down to the export itself.</summary>
internal static class ExportChains
{
    /// <summary>Name of the persistent level in a map package's world.</summary>
    public const string PersistentLevel = "PersistentLevel";

    private const int MaxDepth = 64;

    /// <summary>The names from the top-level export down to <paramref name="export"/>.</summary>
    /// <exception cref="InvalidDataException">The chain does not end.</exception>
    public static IReadOnlyList<string> Chain(CookedPackage package, ZenExport export)
    {
        List<string> names = [export.Name];
        ZenExport current = export;
        while (current.Outer.Kind == PackageObjectKind.Export)
        {
            if (names.Count > MaxDepth)
            {
                throw new InvalidDataException($"{package.Name}: the outer chain of {export.Name} does not end");
            }

            current = package.Exports[current.Outer.ExportIndex];
            names.Add(current.Name);
        }

        names.Reverse();
        return names;
    }

    /// <summary>
    /// The actor name of <paramref name="export"/> when it is an object directly in the package's persistent level
    /// (<c>World.PersistentLevel.Actor</c>), otherwise <see langword="null"/>.
    /// </summary>
    public static string? PlacedActorName(CookedPackage package, ZenExport export)
    {
        IReadOnlyList<string> chain = Chain(package, export);
        return chain is [_, PersistentLevel, { } actor] ? actor : null;
    }
}
