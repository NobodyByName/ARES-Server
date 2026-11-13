using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.World;

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
    /// <param name="socket">The socket.</param>
    /// <param name="meshes">The meshes <see cref="LootSocket.Mesh"/> indexes.</param>
    /// <returns>The candidate classes, or none when no container type has the socket's tag.</returns>
    public static IReadOnlyList<ArchetypeClass> Candidates(LootSocket socket, IReadOnlyList<LootSocketMesh> meshes)
    {
        LootContainerType? type = LootContainerTypes.Find(socket.ContainerTag);
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
    /// <param name="socket">The socket (<see cref="DamLootSockets.All"/>).</param>
    /// <returns>The candidate classes, or none when no container type has the socket's tag.</returns>
    public static IReadOnlyList<ArchetypeClass> Candidates(LootSocket socket)
    {
        return Candidates(socket, DamLootSockets.Meshes);
    }

    /// <summary>The candidate classes of a Dam socket that the server can serve.</summary>
    /// <param name="socket">The socket (<see cref="DamLootSockets.All"/>).</param>
    /// <returns>The candidate classes that have container facts in <see cref="LootContainerClasses"/>.</returns>
    public static IReadOnlyList<ArchetypeClass> Supported(LootSocket socket)
    {
        return [.. Candidates(socket).Where(candidate => LootContainerClasses.Find(candidate.Class) is not null)];
    }

    /// <summary>
    /// The socket tag of a container class: the tag of the first container type (in tag order) that lists the class,
    /// by default or in a colour variant.
    /// </summary>
    /// <param name="classPath">The container's class path.</param>
    /// <returns>The tag, or null when no container type lists the class.</returns>
    public static string? ContainerTag(string classPath)
    {
        ArgumentNullException.ThrowIfNull(classPath);
        return LootContainerTypes.All.FirstOrDefault(type => type.Classes.Concat(type.Variants.SelectMany(variant => variant.Classes))
            .Any(candidate => string.Equals(candidate.Class, classPath, StringComparison.Ordinal)))?.Tag;
    }
}
