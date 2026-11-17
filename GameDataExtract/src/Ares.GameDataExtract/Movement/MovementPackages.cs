using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Movement;

/// <summary>Package lookups the movement readers share; each fails when the package is not shaped as expected.</summary>
internal static class MovementPackages
{
    /// <summary>Bytes after a data asset's property block: four zero bytes.</summary>
    public const int Trailer = 4;

    /// <summary>The package at <paramref name="path"/>.</summary>
    /// <exception cref="InvalidDataException">The package is not in the game archives.</exception>
    public static CookedPackage Load(PackageCache packages, string path) =>
        packages.Load(path) ?? throw new InvalidDataException($"{path} is not in the game archives");

    /// <summary>The package's only export.</summary>
    /// <exception cref="InvalidDataException">The package has another number of exports.</exception>
    public static ZenExport OnlyExport(CookedPackage package) =>
        package.Exports.Count == 1 ? package.Exports[0] : throw new InvalidDataException($"{package.Name}: {package.Exports.Count} exports, expected one");

    /// <summary>The package's one export named <paramref name="name"/>.</summary>
    /// <exception cref="InvalidDataException">No export or several have that name.</exception>
    public static ZenExport Named(CookedPackage package, string name)
    {
        ZenExport[] found = [.. package.Exports.Where(export => export.Name == name)];
        return found.Length == 1 ? found[0] : throw new InvalidDataException($"{package.Name}: {found.Length} exports named {name}, expected one");
    }

    /// <summary>The serialized bytes of <paramref name="export"/>.</summary>
    public static ReadOnlySpan<byte> Bytes(CookedPackage package, ZenExport export) => package.Data.AsSpan(CookedPackage.StartOf(export), (int)export.SerialSize);

    /// <summary>
    /// Reads a data asset's properties: its header must list exactly <paramref name="members"/>, each with a stored value,
    /// and the block must end with the zero <see cref="Trailer"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The block lists other members, does not decode or ends otherwise.</exception>
    public static PropertyBag ReadAsset(CookedPackage package, ZenExport export, StructSchema schema, IReadOnlyList<int> members)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
        if (header.Members.Any(member => !member.IsNonZero) || !header.Members.Select(member => member.Index).SequenceEqual(members))
        {
            throw new InvalidDataException($"{package.Name}: unexpected {schema.Name} layout");
        }

        PropertyBag bag = PropertyReader.ReadExport(package, export, schema, Trailer);
        return package.Data.AsSpan(CookedPackage.EndOf(export) - Trailer, Trailer).ContainsAnyExcept((byte)0)
            ? throw new InvalidDataException($"{package.Name}: unexpected {schema.Name} trailer")
            : bag;
    }

    /// <summary>
    /// The package path of the object a stored reference imports, or <see langword="null"/> for a null reference.
    /// </summary>
    /// <exception cref="InvalidDataException">The reference is not null and not an import of another package's export.</exception>
    public static string? ImportedPackage(CookedPackage package, ObjectReference reference, string what)
    {
        if (reference.IsNull)
        {
            return null;
        }

        if (!package.IsImportReference(reference.Value) || package.Header.Imports[-reference.Value - 1] is not { Kind: PackageObjectKind.PackageImport } import
            || import.ImportedPackageIndex >= package.Header.ImportedPackageNames.Count
            || import.ImportedPublicExportHashIndex >= package.Header.ImportedPublicExportHashes.Count)
        {
            throw new InvalidDataException($"{package.Name}: {what} is not an import of another package");
        }

        return package.Header.ImportedPackageNames[import.ImportedPackageIndex];
    }

    /// <summary>The last segment of a package path.</summary>
    public static string ShortName(string path) => path[(path.LastIndexOf('/') + 1)..];
}
