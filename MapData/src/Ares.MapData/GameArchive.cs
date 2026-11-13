using Ares.Common.Archives;
using Ares.MapData.Archives;
using Ares.MapData.Packages;
using Ares.Structs.Core;

namespace Ares.MapData;

/// <summary>Where a cooked package's chunk is: the path it was looked up by, its container and table entry.</summary>
internal sealed record PackageLocation(string PackagePath, IoStoreContainer Container, int Entry);

/// <summary>
/// Read-only view of an installed game's I/O store containers: every container, the package chunks by package id,
/// the package paths their directory indexes name and the global script object table. Never writes to the game's files.
/// </summary>
internal sealed class GameArchive : IDisposable
{
    private const string ContentFolder = "Content";
    private const string PaksFolder = "Paks";
    private const string ProjectMountRoot = "/Game/";
    private const string AssetExtension = ".uasset";
    private const string MapExtension = ".umap";

    private readonly IoStoreContainer[] _containers;
    private readonly Dictionary<ulong, (IoStoreContainer Container, int Entry)> _packages;
    private readonly string[] _paths;

    private GameArchive(IoStoreContainer[] containers, Dictionary<ulong, (IoStoreContainer Container, int Entry)> packages, string[] paths,
        ScriptObjectTable scriptObjects)
    {
        _containers = containers;
        _packages = packages;
        _paths = paths;
        ScriptObjects = scriptObjects;
    }

    /// <summary>All opened containers, ordered by file name.</summary>
    public IReadOnlyList<IoStoreContainer> Containers => _containers;

    /// <summary>Number of distinct packages (package chunks by package id).</summary>
    public int PackageCount => _packages.Count;

    /// <summary>
    /// Package paths derived from the directory indexes whose package id matches a package chunk, in ordinal order.
    /// Packages mounted under a name other than their folder's are found by path but not listed.
    /// </summary>
    public IReadOnlyList<string> PackagePaths => _paths;

    public ScriptObjectTable ScriptObjects { get; }

    /// <summary>The folder holding the containers of the game installed at <paramref name="gameDirectory"/>.</summary>
    public static string ContainerDirectory(string gameDirectory) => Path.Combine(gameDirectory, GameBuild.ProjectName, ContentFolder, PaksFolder);

    /// <summary>Opens every container of the game installed at <paramref name="gameDirectory"/>.</summary>
    /// <exception cref="DirectoryNotFoundException">The game has no container folder.</exception>
    /// <exception cref="InvalidDataException">A container cannot be read; the message names it.</exception>
    /// <exception cref="WrongTocKeyException"><paramref name="tocKey"/> does not decrypt a table of contents.</exception>
    public static GameArchive Open(string gameDirectory, ArchiveKeys keys, TocKey tocKey)
    {
        string directory = ContainerDirectory(gameDirectory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"No game archives at {directory}");
        }

        string[] tocPaths = [.. Directory.EnumerateFiles(directory, "*" + IoStoreContainer.TocExtension).Order(StringComparer.Ordinal)];
        if (tocPaths.Length == 0)
        {
            throw new DirectoryNotFoundException($"No {IoStoreContainer.TocExtension} files in {directory}");
        }

        List<IoStoreContainer> containers = [];
        try
        {
            foreach (string tocPath in tocPaths)
            {
                try
                {
                    containers.Add(IoStoreContainer.Open(tocPath, keys, tocKey));
                }
                catch (InvalidDataException error)
                {
                    throw new InvalidDataException($"{Path.GetFileName(tocPath)}: {error.Message}", error);
                }
            }

            Dictionary<ulong, (IoStoreContainer Container, int Entry)> packages = [];
            foreach (IoStoreContainer container in containers)
            {
                for (int entry = 0; entry < container.Toc.ChunkIds.Count; entry++)
                {
                    IoChunkId id = container.Toc.ChunkIds[entry];
                    if (id is { Type: IoChunkType.ExportBundleData, Index: 0 })
                    {
                        packages.TryAdd(id.Id, (container, entry));
                    }
                }
            }

            HashSet<string> paths = new(StringComparer.Ordinal);
            foreach (IoStoreContainer container in containers)
            {
                foreach ((string file, int entry) in container.Toc.Files)
                {
                    if (PackagePathOf(file) is { } packagePath && container.Toc.ChunkIds[entry].Id == PackageId.FromPath(packagePath))
                    {
                        paths.Add(packagePath);
                    }
                }
            }

            return new GameArchive([.. containers], packages, [.. paths.Order(StringComparer.Ordinal)], ReadScriptObjects(containers));
        }
        catch
        {
            foreach (IoStoreContainer container in containers)
            {
                container.Dispose();
            }

            throw;
        }
    }

    /// <summary>
    /// The package path of a container file path, for example <c>../../../Project/Content/Maps/Example.umap</c> →
    /// <c>/Game/Maps/Example</c> and <c>../../../Engine/Plugins/Example/Content/Asset.uasset</c> → <c>/Example/Asset</c>;
    /// <see langword="null"/> for files that are not packages.
    /// </summary>
    public static string? PackagePathOf(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        if (extension != AssetExtension && extension != MapExtension)
        {
            return null;
        }

        const string Marker = "/" + ContentFolder + "/";
        int content = filePath.IndexOf(Marker, StringComparison.Ordinal);
        if (content < 0)
        {
            return null;
        }

        string root = filePath[..content];
        string mountName = root[(root.LastIndexOf('/') + 1)..];
        string mount = mountName == GameBuild.ProjectName ? ProjectMountRoot : "/" + mountName + "/";
        return mount + filePath[(content + Marker.Length)..^extension.Length];
    }

    /// <summary>The package at <paramref name="packagePath"/> (found by its package id), if any container has it.</summary>
    public PackageLocation? FindPackage(string packagePath)
    {
        return _packages.TryGetValue(PackageId.FromPath(packagePath), out (IoStoreContainer Container, int Entry) found)
            ? new PackageLocation(packagePath, found.Container, found.Entry)
            : null;
    }

    /// <summary>Paths of the listed packages under the folder <paramref name="directory"/> and its subfolders, in ordinal order.</summary>
    public IReadOnlyList<string> PackagePathsUnder(string directory)
    {
        string prefix = directory.TrimEnd('/') + "/";
        return [.. _paths.Where(path => path.StartsWith(prefix, StringComparison.Ordinal))];
    }

    /// <summary>Reads and parses the header of the package at <paramref name="location"/>.</summary>
    public static ZenPackage ReadPackage(PackageLocation location)
    {
        byte[] summary = new byte[ZenPackage.SummarySize];
        location.Container.ReadChunk(location.Entry, 0, summary);
        byte[] header = new byte[ZenPackage.ReadHeaderSize(summary)];
        location.Container.ReadChunk(location.Entry, 0, header);
        ZenPackage package = ZenPackage.Parse(header);
        return package.Name.Equals(location.PackagePath, StringComparison.OrdinalIgnoreCase)
            ? package
            : throw new InvalidDataException($"Package {location.PackagePath} names itself {package.Name}");
    }

    /// <summary>Reads the serialized bytes of <paramref name="export"/> of the package at <paramref name="location"/>.</summary>
    public static byte[] ReadExport(PackageLocation location, ZenExport export)
    {
        if (export.SerialSize > Array.MaxLength)
        {
            throw new InvalidDataException($"Export {export.Name} of {location.PackagePath} is too large ({export.SerialSize} bytes)");
        }

        byte[] data = new byte[export.SerialSize];
        location.Container.ReadChunk(location.Entry, export.SerialOffset, data);
        return data;
    }

    /// <summary>
    /// The object path <paramref name="index"/> refers to from <paramref name="package"/>: a script path for script
    /// imports (for example <c>/Script/Engine.StaticMeshComponent</c>), the export's path for local exports and, for
    /// imports of another package, the path of that package's export with the imported public export hash.
    /// <see langword="null"/> for null references and references that cannot be resolved.
    /// </summary>
    public string? ResolveObjectPath(ZenPackage package, PackageObjectIndex index)
    {
        switch (index.Kind)
        {
            case PackageObjectKind.ScriptImport:
                return ScriptObjects.PathOf(index);
            case PackageObjectKind.Export:
                return index.ExportIndex < package.Exports.Count ? package.ExportPath(index.ExportIndex) : null;
            case PackageObjectKind.PackageImport:
                if (index.ImportedPackageIndex >= package.ImportedPackageNames.Count
                    || index.ImportedPublicExportHashIndex >= package.ImportedPublicExportHashes.Count
                    || FindPackage(package.ImportedPackageNames[index.ImportedPackageIndex]) is not { } location)
                {
                    return null;
                }

                ulong hash = package.ImportedPublicExportHashes[index.ImportedPublicExportHashIndex];
                ZenPackage imported = ReadPackage(location);
                ZenExport? export = imported.Exports.FirstOrDefault(candidate => candidate.PublicExportHash == hash);
                return export is null ? null : imported.ExportPath(export.Index);
            case PackageObjectKind.Null:
            default:
                return null;
        }
    }

    public void Dispose()
    {
        foreach (IoStoreContainer container in _containers)
        {
            container.Dispose();
        }
    }

    private static ScriptObjectTable ReadScriptObjects(List<IoStoreContainer> containers)
    {
        foreach (IoStoreContainer container in containers)
        {
            for (int entry = 0; entry < container.Toc.ChunkIds.Count; entry++)
            {
                if (container.Toc.ChunkIds[entry].Type != IoChunkType.ScriptObjects)
                {
                    continue;
                }

                try
                {
                    return ScriptObjectTable.Parse(container.ReadChunk(entry));
                }
                catch (InvalidDataException error)
                {
                    throw new InvalidDataException($"{container.Name}{IoStoreContainer.TocExtension}: {error.Message}", error);
                }
            }
        }

        throw new InvalidDataException("No container holds the script objects");
    }
}
