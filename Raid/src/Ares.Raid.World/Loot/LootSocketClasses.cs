using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Loot;

/// <summary>
/// Which container classes the server may spawn at a loot socket: the container type registered for the socket's
/// tag, narrowed to the colour variant of the prop the socket sits on.
/// </summary>
public static class LootSocketClasses
{
    // The prop's first material slot is the one the variant table is keyed by.
    private const int MaterialSlot = 0;

    /// <summary>
    /// The material that selects the colour variant: the placed component's override for the first material
    /// slot, else the mesh's own material in that slot.
    /// </summary>
    /// <param name="socket">The socket.</param>
    /// <param name="meshes">The meshes <see cref="LootSocket.Mesh"/> indexes.</param>
    /// <returns>The material object path, or null when the slot has none.</returns>
    public static string? Material(LootSocket socket, IReadOnlyList<LootSocketMesh> meshes)
    {
        if (socket.OverrideMaterials.Count > MaterialSlot && !string.IsNullOrEmpty(socket.OverrideMaterials[MaterialSlot]))
        {
            return socket.OverrideMaterials[MaterialSlot];
        }

        IReadOnlyList<string?> materials = meshes[socket.Mesh].Materials;
        return materials.Count > MaterialSlot ? materials[MaterialSlot] : null;
    }

    /// <summary>
    /// The classes the game picks from at a socket: the variant listed for the prop's material, else the type's
    /// default classes.
    /// </summary>
    /// <param name="loot">The loot definitions: the container types.</param>
    /// <param name="socket">The socket.</param>
    /// <param name="meshes">The meshes <see cref="LootSocket.Mesh"/> indexes.</param>
    /// <returns>The candidate classes, or none when no container type has the socket's tag.</returns>
    public static IReadOnlyList<ArchetypeClass> Candidates(LootData loot, LootSocket socket, IReadOnlyList<LootSocketMesh> meshes)
    {
        ArgumentNullException.ThrowIfNull(loot);
        LootContainerType? type = loot.FindContainerType(socket.ContainerTag);
        if (type is null)
        {
            return [];
        }

        string? material = Material(socket, meshes);
        LootContainerVariant? variant = material is null
            ? null
            : type.Variants.FirstOrDefault(candidate => string.Equals(candidate.Material, material, StringComparison.OrdinalIgnoreCase));
        return variant?.Classes ?? type.Classes;
    }

    /// <summary>The candidate classes of a Dam socket.</summary>
    /// <param name="data">The game data: the loot definitions and the Dam's socket meshes.</param>
    /// <param name="socket">The socket (<see cref="MapData.LootSockets"/> of the Dam).</param>
    /// <returns>The candidate classes, or none when no container type has the socket's tag.</returns>
    public static IReadOnlyList<ArchetypeClass> Candidates(GameDataSet data, LootSocket socket)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Candidates(data.Loot, socket, data.Names.Dam.LootSocketMeshes);
    }

    /// <summary>The candidate classes of a Dam socket that the server can serve.</summary>
    /// <param name="data">The game data: the loot definitions and the Dam's socket meshes.</param>
    /// <param name="socket">The socket (<see cref="MapData.LootSockets"/> of the Dam).</param>
    /// <returns>The candidate classes the server serves (<see cref="Served"/>).</returns>
    public static IReadOnlyList<ArchetypeClass> Supported(GameDataSet data, LootSocket socket)
    {
        ArgumentNullException.ThrowIfNull(data);
        return [.. Candidates(data, socket).Where(candidate => Served(data.Loot, candidate.Class) is not null)];
    }

    /// <summary>
    /// The container class with a class path that the server serves: one whose container facts decode and that ARES
    /// supports (<see cref="LootContainerSupport"/>).
    /// </summary>
    /// <param name="loot">The loot definitions.</param>
    /// <param name="classPath">The container's class path.</param>
    /// <returns>The class, or null when the server does not serve it.</returns>
    public static LootContainerClass? Served(LootData loot, string classPath)
    {
        ArgumentNullException.ThrowIfNull(loot);
        return LootContainerSupport.IsSupported(classPath) ? loot.FindContainerClass(classPath) : null;
    }

    /// <summary>Every container class the server serves (<see cref="Served"/>), ordered by class path.</summary>
    /// <param name="loot">The loot definitions.</param>
    /// <returns>The classes.</returns>
    public static IReadOnlyList<LootContainerClass> ServedClasses(LootData loot)
    {
        ArgumentNullException.ThrowIfNull(loot);
        return [.. loot.ContainerClasses.Where(type => LootContainerSupport.IsSupported(type.Container.Class))];
    }

    /// <summary>
    /// The socket tag of a container class: the tag of the first container type (in tag order) that lists the class,
    /// by default or in a colour variant.
    /// </summary>
    /// <param name="loot">The loot definitions: the container types.</param>
    /// <param name="classPath">The container's class path.</param>
    /// <returns>The tag, or null when no container type lists the class.</returns>
    public static string? ContainerTag(LootData loot, string classPath)
    {
        ArgumentNullException.ThrowIfNull(loot);
        ArgumentNullException.ThrowIfNull(classPath);
        return loot.ContainerTypes.FirstOrDefault(type => type.Classes.Concat(type.Variants.SelectMany(variant => variant.Classes))
            .Any(candidate => string.Equals(candidate.Class, classPath, StringComparison.Ordinal)))?.Tag;
    }
}
