using Ares.MapData.Components;
using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Landscape;

/// <summary>
/// The members of a <c>ULandscapeHeightfieldCollisionComponent</c> export that place its height field, read forwards
/// through its unversioned properties, and the cooked height field that follows them. Scene members the export does
/// not store keep the scene defaults (no offset, rotation or parent, unit scale).
/// </summary>
/// <param name="CollisionSizeQuads">Quads per side of the collision grid.</param>
/// <param name="CollisionScale">Distance between collision samples in the parent's units.</param>
/// <param name="AttachParent">Attach parent as an object reference (0 for none).</param>
/// <param name="AttachSocket">Attach socket name, when stored.</param>
/// <param name="Location">Relative location.</param>
/// <param name="Rotation">Relative rotation.</param>
/// <param name="Scale">Relative scale.</param>
/// <param name="Absolute">Which transform members ignore the parent's.</param>
/// <param name="CookedHeightField">The PhysX height-field stream; empty when the component stores no cooked collision.</param>
/// <param name="CookedPhysicalMaterials">The physical materials the height field's material indices refer to, in index order.</param>
internal sealed record LandscapeComponentValues(
    int CollisionSizeQuads,
    double CollisionScale,
    int AttachParent,
    string? AttachSocket,
    Vector3D Location,
    Rotator Rotation,
    Vector3D Scale,
    AbsoluteMembers Absolute,
    ReadOnlyMemory<byte> CookedHeightField,
    IReadOnlyList<StoredReference> CookedPhysicalMaterials)
{
    /// <summary>Reads <paramref name="export"/>'s members and cooked height field.</summary>
    /// <exception cref="InvalidDataException">The export stores a member this reader cannot step over, lacks the collision size or scale, or does not decode.</exception>
    public static LandscapeComponentValues Read(CookedPackage package, ZenExport export)
    {
        int end = CookedPackage.EndOf(export);
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
        int at = header.ValuesOffset;
        int? quads = null;
        double? collisionScale = null;
        int parent = 0;
        string? socket = null;
        Vector3D location = default;
        Rotator rotation = default;
        Vector3D scale = Vector3D.One;
        bool[] absolute = new bool[3];
        List<StoredReference> physicalMaterials = [];
        foreach ((int index, bool isNonZero) in header.Members)
        {
            if (!isNonZero)
            {
                switch (index)
                {
                    case LandscapeHeightfieldCollisionComponentMembers.CollisionSizeQuads:
                        quads = 0;
                        break;
                    case LandscapeHeightfieldCollisionComponentMembers.CollisionScale:
                        collisionScale = 0;
                        break;
                    case LandscapeHeightfieldCollisionComponentMembers.ScenePart + SceneComponentMembers.RelativeScale3D:
                        scale = default;
                        break;
                }

                continue;
            }

            if (index < LandscapeHeightfieldCollisionComponentMembers.ScenePart)
            {
                switch (index)
                {
                    case LandscapeHeightfieldCollisionComponentMembers.CollisionSizeQuads:
                        quads = package.ReadInt32(at);
                        break;
                    case LandscapeHeightfieldCollisionComponentMembers.CollisionScale:
                        collisionScale = package.ReadSingle(at);
                        break;
                    case LandscapeHeightfieldCollisionComponentMembers.CookedPhysicalMaterials:
                        physicalMaterials.AddRange(ReadReferences(package, at, end));
                        break;
                }

                at = LandscapeHeightfieldCollisionComponentMembers.IsReferenceArray(index)
                    ? SkipArray(package, at, end, LandscapeHeightfieldCollisionComponentMembers.ReferenceSize)
                    : at + (LandscapeHeightfieldCollisionComponentMembers.FixedValueSize(index)
                        ?? throw new InvalidDataException($"Landscape collision member {index} cannot be stepped over"));
                continue;
            }

            int offset = index - LandscapeHeightfieldCollisionComponentMembers.ScenePart;
            switch (offset)
            {
                case SceneComponentMembers.AttachParent:
                    parent = package.ReadInt32(at);
                    at += SceneComponentMembers.ReferenceSize;
                    break;
                case SceneComponentMembers.AttachSocketName:
                    socket = package.ReadName(at);
                    at += SceneComponentMembers.NameSize;
                    break;
                case SceneComponentMembers.RelativeLocation:
                    location = package.ReadVector(at);
                    at += SceneComponentMembers.VectorSize;
                    break;
                case SceneComponentMembers.RelativeRotation:
                    rotation = package.ReadRotator(at);
                    at += SceneComponentMembers.VectorSize;
                    break;
                case SceneComponentMembers.RelativeScale3D:
                    scale = package.ReadVector(at);
                    at += SceneComponentMembers.VectorSize;
                    break;
                case SceneComponentMembers.AttachChildren or SceneComponentMembers.ClientAttachedChildren:
                    at = SkipArray(package, at, end, SceneComponentMembers.ReferenceSize);
                    break;
                case SceneComponentMembers.ComponentTags:
                    at = SkipArray(package, at, end, SceneComponentMembers.NameSize);
                    break;
                default:
                    int size = SceneComponentMembers.FixedValueSize(offset)
                        ?? throw new InvalidDataException($"Scene member +{offset} cannot be stepped over");
                    if (offset is >= SceneComponentMembers.AbsoluteLocation and <= SceneComponentMembers.AbsoluteScale)
                    {
                        absolute[offset - SceneComponentMembers.AbsoluteLocation] = package.ReadByte(at) != 0;
                    }

                    at += size;
                    break;
            }
        }

        if (at > end)
        {
            throw new InvalidDataException("The landscape collision properties run past the export");
        }

        ReadOnlyMemory<byte> cooked = LandscapeCollision.ReadCookedHeightField(package.Data.AsMemory(at, end - at));
        return new LandscapeComponentValues(
            quads ?? throw new InvalidDataException("The landscape collision component stores no collision size"),
            collisionScale ?? throw new InvalidDataException("The landscape collision component stores no collision scale"),
            parent,
            socket,
            location,
            rotation,
            scale,
            new AbsoluteMembers(absolute[0], absolute[1], absolute[2]),
            cooked,
            physicalMaterials);
    }

    private static List<StoredReference> ReadReferences(CookedPackage package, int at, int end)
    {
        SkipArray(package, at, end, LandscapeHeightfieldCollisionComponentMembers.ReferenceSize);
        int count = package.ReadInt32(at);
        List<StoredReference> references = new(count);
        for (int i = 0; i < count; i++)
        {
            references.Add(new StoredReference(package, package.ReadInt32(at + sizeof(int) + (i * LandscapeHeightfieldCollisionComponentMembers.ReferenceSize))));
        }

        return references;
    }

    private static int SkipArray(CookedPackage package, int at, int end, int elementSize)
    {
        int count = package.ReadInt32(at);
        if (count < 0 || (long)count * elementSize > end - at - sizeof(int))
        {
            throw new InvalidDataException($"Invalid array count {count} at offset {at}");
        }

        return at + sizeof(int) + (count * elementSize);
    }
}
