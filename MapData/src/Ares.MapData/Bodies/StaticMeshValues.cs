using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Bodies;

/// <summary>Stored members of a static mesh export: its material slots and its bounds.</summary>
internal static class StaticMeshValues
{
    private const int MaxMaterialSlots = 256;

    /// <summary>
    /// The stored material reference of each of the mesh's slots (its <c>StaticMaterials</c>; 0 for a slot without a
    /// material) and the offset right after them; no slots and the values' start when the mesh stores none.
    /// </summary>
    /// <exception cref="InvalidDataException">A member before the slots or a slot member has an unknown size, or the slots run past the export.</exception>
    public static (IReadOnlyList<int> Materials, int End) Materials(CookedPackage package, ZenExport mesh, UnversionedHeader header)
    {
        if (!header.IsNonZero(StaticMeshMembers.StaticMaterials))
        {
            return ([], header.ValuesOffset);
        }

        int at = header.ValuesOffset + header.Members
            .TakeWhile(member => member.Index < StaticMeshMembers.StaticMaterials)
            .Where(member => member.IsNonZero)
            .Sum(member => StaticMeshMembers.FixedValueSize(member.Index)
                ?? throw new InvalidDataException($"{package.Name}: member {member.Index} of {mesh.Name} has an unknown size"));
        int end = CookedPackage.EndOf(mesh);
        int count = package.ReadInt32(at);
        if (count is < 0 or > MaxMaterialSlots)
        {
            throw new InvalidDataException($"{package.Name}: {mesh.Name} has {count} material slots");
        }

        at += sizeof(int);
        int[] materials = new int[count];
        for (int slot = 0; slot < count; slot++)
        {
            UnversionedHeader element = UnversionedHeader.Read(package.Data, at, StaticMaterialMembers.MemberCount);
            at = element.ValuesOffset;
            foreach (UnversionedMember member in element.Members.Where(member => member.IsNonZero))
            {
                switch (member.Index)
                {
                    case StaticMaterialMembers.MaterialInterface:
                        materials[slot] = package.ReadInt32(at);
                        break;
                    case StaticMaterialMembers.UvChannelData:
                        UnversionedHeader channels = UnversionedHeader.Read(package.Data, at, MeshUvChannelInfoMembers.MemberCount);
                        at = channels.ValuesOffset + channels.Members.Where(value => value.IsNonZero).Sum(value => MeshUvChannelInfoMembers.ValueSize(value.Index));
                        continue;
                }

                at += StaticMaterialMembers.FixedValueSize(member.Index)
                    ?? throw new InvalidDataException($"{package.Name}: static material member {member.Index} of {mesh.Name} has an unknown size");
            }

            if (at > end)
            {
                throw new InvalidDataException($"{package.Name}: the material slots of {mesh.Name} run past the export");
            }
        }

        return (materials, at);
    }

    /// <summary>The mesh's <c>ExtendedBounds</c> box as its centre and half size.</summary>
    /// <exception cref="InvalidDataException">The mesh stores no bounds, or a member before them has an unknown size.</exception>
    public static (Vector3D Origin, Vector3D Extent) ExtendedBounds(CookedPackage package, ZenExport mesh)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(mesh));
        if (!header.IsNonZero(StaticMeshMembers.ExtendedBounds))
        {
            throw new InvalidDataException($"{package.Name}: {mesh.Name} stores no bounds");
        }

        int at = Materials(package, mesh, header).End + header.Members
            .Where(member => member is { IsNonZero: true, Index: > StaticMeshMembers.StaticMaterials and < StaticMeshMembers.ExtendedBounds })
            .Sum(member => StaticMeshMembers.FixedValueSize(member.Index)
                ?? throw new InvalidDataException($"{package.Name}: member {member.Index} of {mesh.Name} before its bounds has an unknown size"));

        UnversionedHeader bounds = UnversionedHeader.Read(package.Data, at, BoxSphereBoundsMembers.MemberCount);
        if (!bounds.Contains(BoxSphereBoundsMembers.Origin) || !bounds.Contains(BoxSphereBoundsMembers.BoxExtent))
        {
            throw new InvalidDataException($"{package.Name}: the bounds of {mesh.Name} do not store their box");
        }

        at = bounds.ValuesOffset;
        Vector3D origin = default;
        if (bounds.IsNonZero(BoxSphereBoundsMembers.Origin))
        {
            origin = package.ReadVector(at);
            at += BoxSphereBoundsMembers.ValueSize(BoxSphereBoundsMembers.Origin);
        }

        Vector3D extent = bounds.IsNonZero(BoxSphereBoundsMembers.BoxExtent) ? package.ReadVector(at) : default;
        return at <= CookedPackage.EndOf(mesh) ? (origin, extent) : throw new InvalidDataException($"{package.Name}: the bounds of {mesh.Name} run past the export");
    }
}
