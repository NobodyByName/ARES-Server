namespace Ares.MapData.Packages;

/// <summary>An export of a loaded package.</summary>
internal readonly record struct ExportRef(CookedPackage Package, ZenExport Export)
{
    /// <summary>The export's object path, for example <c>/Game/Meshes/Rock.Rock</c>.</summary>
    public string Path => Package.Header.ExportPath(Export.Index);
}

/// <summary>
/// Loads cooked packages by path once each (headers on first use, chunks when their bytes are first read) and resolves
/// object references between them.
/// </summary>
internal sealed class PackageCache
{
    private readonly Func<string, CookedPackage?> _load;
    private readonly Func<PackageObjectIndex, string?> _scriptPath;
    private readonly Dictionary<string, CookedPackage?> _packages = new(StringComparer.Ordinal);

    /// <summary>A cache over <paramref name="load"/> (a package by path, or null when absent) and <paramref name="scriptPath"/> (script object paths by id).</summary>
    public PackageCache(Func<string, CookedPackage?> load, Func<PackageObjectIndex, string?> scriptPath)
    {
        _load = load;
        _scriptPath = scriptPath;
    }

    /// <summary>A cache over the packages of <paramref name="archive"/>.</summary>
    public static PackageCache FromArchive(GameArchive archive)
    {
        return new PackageCache(
            path => archive.FindPackage(path) is { } location
                ? new CookedPackage(GameArchive.ReadPackage(location), () => location.Container.ReadChunk(location.Entry))
                : null,
            archive.ScriptObjects.PathOf);
    }

    /// <summary>The package at <paramref name="path"/>, or <see langword="null"/> when no container has it.</summary>
    public CookedPackage? Load(string path)
    {
        if (_packages.TryGetValue(path, out CookedPackage? package))
        {
            return package;
        }

        package = _load(path);
        _packages[path] = package;
        return package;
    }

    /// <summary>The object path of script object <paramref name="index"/>, if known.</summary>
    public string? ScriptPath(PackageObjectIndex index) => _scriptPath(index);

    /// <summary>
    /// The export <paramref name="index"/> refers to from <paramref name="package"/>: an export of the same package, or
    /// the public export of another package. <see langword="null"/> for null and script references.
    /// </summary>
    /// <exception cref="InvalidDataException">The reference is out of range or the imported export does not exist.</exception>
    public ExportRef? Resolve(CookedPackage package, PackageObjectIndex index)
    {
        switch (index.Kind)
        {
            case PackageObjectKind.Export:
                return index.ExportIndex < package.Exports.Count
                    ? new ExportRef(package, package.Exports[index.ExportIndex])
                    : throw new InvalidDataException($"{package.Name}: export reference {index.ExportIndex} is out of range");
            case PackageObjectKind.PackageImport:
                ZenPackage header = package.Header;
                if (index.ImportedPackageIndex >= header.ImportedPackageNames.Count
                    || index.ImportedPublicExportHashIndex >= header.ImportedPublicExportHashes.Count)
                {
                    throw new InvalidDataException($"{package.Name}: import {index} is out of range");
                }

                string path = header.ImportedPackageNames[index.ImportedPackageIndex];
                ulong hash = header.ImportedPublicExportHashes[index.ImportedPublicExportHashIndex];
                CookedPackage other = Load(path) ?? throw new InvalidDataException($"{path} is not in the game archives");
                ZenExport export = other.PublicExport(hash) ?? throw new InvalidDataException($"{path} has no public export {hash:x}");
                return new ExportRef(other, export);
            case PackageObjectKind.ScriptImport:
            case PackageObjectKind.Null:
            default:
                return null;
        }
    }

    /// <summary>
    /// The export a property's object reference <paramref name="value"/> refers to: 0 none, a positive value export
    /// <c>value − 1</c>, a negative value import <c>−value − 1</c>. <see langword="null"/> for none and script objects.
    /// </summary>
    /// <exception cref="InvalidDataException">The reference is out of range or cannot be resolved.</exception>
    public ExportRef? Reference(CookedPackage package, int value)
    {
        return value switch
        {
            0 => null,
            > 0 => value <= package.Exports.Count
                ? new ExportRef(package, package.Exports[value - 1])
                : throw new InvalidDataException($"{package.Name}: export reference {value} is out of range"),
            _ => package.IsImportReference(value)
                ? Resolve(package, package.Header.Imports[-value - 1])
                : throw new InvalidDataException($"{package.Name}: import reference {value} is out of range"),
        };
    }
}
