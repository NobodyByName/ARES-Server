using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.GameDataExtract.Bodies;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads a map's loot container sockets (<see cref="LootSocket"/>): the sockets tagged <c>Socket.LootContainer.*</c> of
/// the static meshes that placed static mesh components show (a placed actor's direct components). A socket's world
/// transform is its relative transform composed over the component's world transform. Locations are rounded to 2
/// decimal places, rotations (degrees) to 3 and scales to 4. Read every cell with <see cref="ReadCell"/>, then
/// <see cref="Build"/>.
/// </summary>
internal sealed class LootSocketReader
{
    /// <summary>Gameplay tag family of container sockets (gameplay tag names compare without case).</summary>
    private const string ContainerTagFamily = "Socket.LootContainer.";

    private const int LocationDigits = 2;
    private const int RotationDigits = 3;
    private const int ScaleDigits = 4;

    private static readonly PackageObjectIndex s_staticMeshComponent = PackageObjectIndex.FromScriptPath(StaticMeshComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_staticMesh = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.StaticMesh);

    private readonly ComponentReader _components;
    private readonly PackageCache _packages;
    private readonly string _mapPackage;
    private readonly Dictionary<string, MeshEntry> _meshes = new(StringComparer.Ordinal);
    private readonly List<(LootSocket Socket, string MeshPath)> _sockets = [];

    /// <summary>A reader for the cells of map package <paramref name="mapPackage"/>.</summary>
    public LootSocketReader(ComponentReader components, PackageCache packages, string mapPackage)
    {
        _components = components;
        _packages = packages;
        _mapPackage = mapPackage;
    }

    /// <summary>Reads the sockets of the components placed in <paramref name="cell"/>.</summary>
    /// <exception cref="InvalidDataException">A component's mesh, a mesh's sockets or materials, or a container component's transform does not decode.</exception>
    public void ReadCell(MapCell cell, CookedPackage package)
    {
        string cellName = MapCells.Name(cell, _mapPackage);
        string? dataLayer = MapCells.DataLayer(cell);
        HashSet<int> levels = PlacedActors.Levels(package);
        foreach (ZenExport export in package.Exports)
        {
            if (export.Class != s_staticMeshComponent || PlacedActors.ActorOf(package, export, levels) is not { } actor)
            {
                continue;
            }

            try
            {
                ReadComponent(package, export, actor, cellName, dataLayer);
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{package.Header.ExportPath(export.Index)}: {error.Message}", error);
            }
        }
    }

    /// <summary>
    /// The meshes that carry the sockets, ordered by path, and the sockets ordered by cell, actor, component and socket
    /// name (ordinal), each indexing its mesh.
    /// </summary>
    /// <exception cref="InvalidDataException">Two sockets have the same cell, actor, component and socket name.</exception>
    public (ImmutableArray<LootSocketMesh> Meshes, ImmutableArray<LootSocket> Sockets) Build()
    {
        string[] paths = [.. _sockets.Select(entry => entry.MeshPath).Distinct().Order(StringComparer.Ordinal)];
        Dictionary<string, int> index = paths.Select((path, i) => (path, i)).ToDictionary(entry => entry.path, entry => entry.i, StringComparer.Ordinal);
        ImmutableArray<LootSocket> sockets = [.. _sockets
            .Select(entry => entry.Socket with { Mesh = index[entry.MeshPath] })
            .OrderBy(socket => socket.Cell, StringComparer.Ordinal)
            .ThenBy(socket => socket.Actor, StringComparer.Ordinal)
            .ThenBy(socket => socket.Component, StringComparer.Ordinal)
            .ThenBy(socket => socket.Socket, StringComparer.Ordinal)];
        for (int i = 1; i < sockets.Length; i++)
        {
            if (Key(sockets[i - 1]) == Key(sockets[i]))
            {
                throw new InvalidDataException($"Two loot sockets are {Key(sockets[i])}");
            }
        }

        return ([.. paths.Select(path => new LootSocketMesh(path, _meshes[path].Materials))], sockets);
    }

    private static (string, string, string, string) Key(LootSocket socket) => (socket.Cell, socket.Actor, socket.Component, socket.Socket);

    private void ReadComponent(CookedPackage package, ZenExport export, ZenExport actor, string cellName, string? dataLayer)
    {
        if (_components.StaticMesh(package, export) is not { } mesh)
        {
            return;
        }

        if (mesh.Export.Class != s_staticMesh)
        {
            throw new InvalidDataException($"The component's static mesh {mesh.Path} is not a static mesh");
        }

        MeshEntry entry = Mesh(mesh);
        if (entry.Containers.IsEmpty)
        {
            return;
        }

        Transform world = _components.Scenes.WorldTransform(package, export);
        ImmutableArray<string?> overrides = [.. (_components.OverrideMaterials(package, export) ?? []).Select(PathOf)];
        foreach ((MeshSocket socket, string tag) in entry.Containers)
        {
            Transform placed = TransformMath.Compose(socket.Transform, world);
            LootSocket row = new(
                cellName,
                actor.Name,
                export.Name,
                socket.Name!,
                tag,
                DecimalRounding.Round(placed.Translation, LocationDigits),
                DecimalRounding.Round(TransformMath.ToRotator(placed.Rotation), RotationDigits),
                DecimalRounding.Round(placed.Scale3D, ScaleDigits),
                -1,
                overrides,
                dataLayer);
            _sockets.Add((row, entry.Path));
        }
    }

    private MeshEntry Mesh(ExportRef mesh)
    {
        string path = mesh.Path;
        if (_meshes.TryGetValue(path, out MeshEntry? entry))
        {
            return entry;
        }

        ImmutableArray<(MeshSocket, string)>.Builder containers = ImmutableArray.CreateBuilder<(MeshSocket, string)>();
        foreach (MeshSocket socket in SceneResolver.Sockets(mesh))
        {
            string[] tags = [.. socket.Tags.Where(tag => tag.StartsWith(ContainerTagFamily, StringComparison.OrdinalIgnoreCase))];
            if (tags.Length == 0)
            {
                continue;
            }

            if (tags.Length != 1 || string.IsNullOrEmpty(socket.Name))
            {
                throw new InvalidDataException($"{path}: socket {socket.Name} has {tags.Length} container tags");
            }

            containers.Add((socket, tags[0]));
        }

        ImmutableArray<string?> materials = containers.Count == 0 ? [] : MeshMaterials(mesh);
        entry = new MeshEntry(path, containers.ToImmutable(), materials);
        _meshes[path] = entry;
        return entry;
    }

    private ImmutableArray<string?> MeshMaterials(ExportRef mesh)
    {
        UnversionedHeader header = UnversionedHeader.Read(mesh.Package.Data, CookedPackage.StartOf(mesh.Export));
        (IReadOnlyList<int> materials, _) = StaticMeshValues.Materials(mesh.Package, mesh.Export, header);
        return [.. materials.Select(value => PathOf(new StoredReference(mesh.Package, value)))];
    }

    /// <summary>The object path a stored material reference names, or <see langword="null"/> for none.</summary>
    private string? PathOf(StoredReference reference)
    {
        return reference.Value == 0
            ? null
            : (_packages.Reference(reference) ?? throw new InvalidDataException($"Material reference {reference.Value} of {reference.Package.Name} is not an export")).Path;
    }

    private sealed record MeshEntry(string Path, ImmutableArray<(MeshSocket Socket, string Tag)> Containers, ImmutableArray<string?> Materials);
}
