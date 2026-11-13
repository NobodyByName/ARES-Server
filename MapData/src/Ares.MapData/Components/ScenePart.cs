using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Components;

/// <summary>Which native words close a component export after its properties (see <see cref="ComponentNativeData"/>).</summary>
internal enum NativeTail
{
    /// <summary>The zero word and entries only.</summary>
    None,

    /// <summary>The zero word and entries, then the LOD info array and <see cref="ComponentNativeData.StaticMeshTrailer"/>.</summary>
    StaticMesh,

    /// <summary><see cref="None"/>, or <see cref="StaticMesh"/> when the block does not decode without it.</summary>
    NoneOrStaticMesh,
}

/// <summary>
/// The scene members one component export stores itself (members it does not store are <see langword="null"/>).
/// </summary>
/// <param name="Header">The export's unversioned header.</param>
/// <param name="ScenePartOffset">Offset of the first scene part value; the primitive members (and the body instance) end there.</param>
/// <param name="Location">Relative location.</param>
/// <param name="Rotation">Relative rotation.</param>
/// <param name="Scale">Relative scale.</param>
/// <param name="Parent">Attach parent as an object reference (0 for none).</param>
/// <param name="Socket">Attach socket name.</param>
/// <param name="AbsoluteLocation">Whether the location ignores the parent's.</param>
/// <param name="AbsoluteRotation">Whether the rotation ignores the parent's.</param>
/// <param name="AbsoluteScale">Whether the scale ignores the parent's.</param>
internal sealed record SceneMembers(
    UnversionedHeader Header,
    int ScenePartOffset,
    Vector3D? Location,
    Rotator? Rotation,
    Vector3D? Scale,
    int? Parent,
    string? Socket,
    bool? AbsoluteLocation,
    bool? AbsoluteRotation,
    bool? AbsoluteScale);

/// <summary>
/// Reads the scene part of a component export backwards from the export's end: the native data closing the export is
/// stepped over, then each stored scene member from the last one, so the members before the scene part need not be
/// understood.
/// </summary>
internal static class ScenePart
{
    private const int MaxArrayCount = 63;
    private const int SmallStructMinSize = 3;
    private const int SmallStructMaxSize = 23;
    private const int SmallStructMemberLimit = 16;

    /// <summary>Reads the scene members of <paramref name="export"/>, whose scene part starts at chain index <paramref name="scenePart"/>.</summary>
    /// <exception cref="InvalidDataException">The block holds a member this reader cannot step over, or does not decode.</exception>
    public static SceneMembers Read(CookedPackage package, ZenExport export, int scenePart, NativeTail tail)
    {
        if (tail != NativeTail.NoneOrStaticMesh)
        {
            return Read(package, export, scenePart, tail == NativeTail.StaticMesh);
        }

        try
        {
            return Read(package, export, scenePart, staticMeshTail: false);
        }
        catch (InvalidDataException)
        {
            return Read(package, export, scenePart, staticMeshTail: true);
        }
    }

    /// <summary>
    /// Bytes of the native data closing a component export that ends at <paramref name="end"/>: the largest reading of a
    /// zero word, a count and that many entries (each starting with an import reference), followed when
    /// <paramref name="staticMeshTail"/> is set by a LOD info array (see <see cref="StaticMeshLodInfoNativeData"/>) and
    /// the static mesh trailer.
    /// </summary>
    /// <exception cref="InvalidDataException">No reading fits.</exception>
    private static int NativeDataSize(CookedPackage package, int start, int end, bool staticMeshTail)
    {
        ReadOnlySpan<byte> trailer = staticMeshTail ? ComponentNativeData.StaticMeshTrailer : [];
        if (end - trailer.Length < start || !package.Data.AsSpan(end - trailer.Length, trailer.Length).SequenceEqual(trailer))
        {
            throw new InvalidDataException("The component's native data differs");
        }

        int lodEnd = end - trailer.Length;
        int size = -1;
        for (int head = lodEnd - ComponentNativeData.HeadSize - (staticMeshTail ? ComponentNativeData.LodCountSize : 0); head >= start; head--)
        {
            uint entries = package.ReadUInt32(head + 4);
            if (package.ReadUInt32(head) != 0 || entries > ComponentNativeData.MaxEntries)
            {
                continue;
            }

            int count = (int)entries;
            int at = head + ComponentNativeData.HeadSize + (ComponentNativeData.EntrySize * count);
            if (at > lodEnd || !EntriesStartWithImports(package, head, count))
            {
                continue;
            }

            if ((staticMeshTail ? SkipLodInfos(package, at, lodEnd) : at) == lodEnd)
            {
                size = end - head;
            }
        }

        return size >= 0 ? size : throw new InvalidDataException("The component's native data is not recognised");
    }

    /// <summary>
    /// Start of the non-empty array that ends at <paramref name="end"/>: the one count n ≥ 1 framing n elements of
    /// <paramref name="elementSize"/> bytes that all pass <paramref name="isElement"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">No count, or more than one, fits.</exception>
    private static int ArrayStart(CookedPackage package, int end, int elementSize, Func<int, bool> isElement)
    {
        int found = -1;
        int readings = 0;
        for (int count = 1; count <= MaxArrayCount; count++)
        {
            int at = end - (elementSize * count) - sizeof(int);
            if (at < 0)
            {
                break;
            }

            if (package.ReadInt32(at) != count || !Enumerable.Range(0, count).All(i => isElement(at + sizeof(int) + (elementSize * i))))
            {
                continue;
            }

            found = at;
            readings++;
        }

        return readings == 1 ? found : throw new InvalidDataException($"The array before offset {end} is not recognised ({readings} readings)");
    }

    private static SceneMembers Read(CookedPackage package, ZenExport export, int scenePart, bool staticMeshTail)
    {
        int start = CookedPackage.StartOf(export);
        UnversionedHeader header = UnversionedHeader.Read(package.Data, start);
        int end = CookedPackage.EndOf(export);
        end -= NativeDataSize(package, header.ValuesOffset, end, staticMeshTail);

        Vector3D? location = null;
        Rotator? rotation = null;
        Vector3D? scale = null;
        int? parent = null;
        string? socket = null;
        bool?[] absolute = new bool?[3];
        for (int i = header.Members.Count - 1; i >= 0; i--)
        {
            UnversionedMember member = header.Members[i];
            int offset = member.Index - scenePart;
            if (offset < 0)
            {
                break;
            }

            if (!member.IsNonZero)
            {
                switch (offset)
                {
                    case SceneComponentMembers.RelativeLocation:
                        location = default(Vector3D);
                        break;
                    case SceneComponentMembers.RelativeRotation:
                        rotation = default(Rotator);
                        break;
                    case SceneComponentMembers.RelativeScale3D:
                        scale = default(Vector3D);
                        break;
                    case SceneComponentMembers.AttachParent:
                        parent = 0;
                        break;
                    case >= SceneComponentMembers.AbsoluteLocation and <= SceneComponentMembers.AbsoluteScale:
                        absolute[offset - SceneComponentMembers.AbsoluteLocation] = false;
                        break;
                }

                continue;
            }

            switch (offset)
            {
                case SceneComponentMembers.RelativeLocation:
                    end -= SceneComponentMembers.VectorSize;
                    location = package.ReadVector(end);
                    break;
                case SceneComponentMembers.RelativeRotation:
                    end -= SceneComponentMembers.VectorSize;
                    rotation = package.ReadRotator(end);
                    break;
                case SceneComponentMembers.RelativeScale3D:
                    end -= SceneComponentMembers.VectorSize;
                    scale = package.ReadVector(end);
                    break;
                case SceneComponentMembers.AttachParent:
                    end -= SceneComponentMembers.ReferenceSize;
                    parent = package.ReadInt32(end);
                    break;
                case SceneComponentMembers.AttachSocketName:
                    end -= SceneComponentMembers.NameSize;
                    socket = package.ReadName(end);
                    break;
                case SceneComponentMembers.AttachChildren or SceneComponentMembers.ClientAttachedChildren:
                    end = ArrayStart(package, end, SceneComponentMembers.ReferenceSize, package.IsExportReference);
                    break;
                case SceneComponentMembers.ComponentTags:
                    end = ArrayStart(package, end, SceneComponentMembers.NameSize, package.IsName);
                    break;
                case SceneComponentMembers.Unknown32:
                    end -= SmallStructSize(package, end);
                    break;
                default:
                    end -= SceneComponentMembers.FixedValueSize(offset)
                        ?? throw new InvalidDataException($"Scene member +{offset} cannot be stepped over");
                    if (offset is >= SceneComponentMembers.AbsoluteLocation and <= SceneComponentMembers.AbsoluteScale)
                    {
                        absolute[offset - SceneComponentMembers.AbsoluteLocation] = package.ReadByte(end) != 0;
                    }

                    break;
            }
        }

        return end >= header.ValuesOffset
            ? new SceneMembers(header, end, location, rotation, scale, parent, socket, absolute[0], absolute[1], absolute[2])
            : throw new InvalidDataException("The scene part overlaps the members before it");
    }

    /// <summary>Bytes of a small struct of one-byte members that ends at <paramref name="end"/>: the one size whose header frames exactly the rest.</summary>
    private static int SmallStructSize(CookedPackage package, int end)
    {
        int found = -1;
        int readings = 0;
        for (int size = SmallStructMinSize; size <= SmallStructMaxSize; size++)
        {
            if (UnversionedHeader.TryReadStrict(package.Data, end - size, SmallStructMemberLimit) is not { } header
                || header.ValuesOffset + header.NonZeroCount != end)
            {
                continue;
            }

            found = size;
            readings++;
        }

        return readings == 1 ? found : throw new InvalidDataException($"Scene member +{SceneComponentMembers.Unknown32} has {readings} readings");
    }

    /// <summary>End of the LOD info array at <paramref name="at"/>, or −1 when it does not read within <paramref name="limit"/>.</summary>
    private static int SkipLodInfos(CookedPackage package, int at, int limit)
    {
        if (at + ComponentNativeData.LodCountSize > limit)
        {
            return -1;
        }

        uint lods = package.ReadUInt32(at);
        if (lods > ComponentNativeData.MaxLodInfos)
        {
            return -1;
        }

        at += ComponentNativeData.LodCountSize;
        for (uint i = 0; i < lods && at >= 0; i++)
        {
            at = SkipLodInfo(package, at, limit);
        }

        return at;
    }

    /// <summary>End of the LOD info entry at <paramref name="at"/>, or −1 when it does not read within <paramref name="limit"/>.</summary>
    private static int SkipLodInfo(CookedPackage package, int at, int limit)
    {
        if (at + StaticMeshLodInfoNativeData.StripFlagsSize + StaticMeshLodInfoNativeData.FixedSize > limit)
        {
            return -1;
        }

        byte global = package.ReadByte(at);
        byte classFlags = package.ReadByte(at + 1);
        if ((global & StaticMeshLodInfoNativeData.EditorDataStripped) == 0 || (classFlags & ~StaticMeshLodInfoNativeData.OverrideColorsStripped) != 0)
        {
            return -1;
        }

        at += StaticMeshLodInfoNativeData.StripFlagsSize + StaticMeshLodInfoNativeData.FixedSize;
        if ((classFlags & StaticMeshLodInfoNativeData.OverrideColorsStripped) != 0)
        {
            return at;
        }

        if (at + 1 > limit || package.ReadByte(at) > 1)
        {
            return -1;
        }

        if (package.ReadByte(at++) == 0)
        {
            return at;
        }

        if (at + StaticMeshLodInfoNativeData.ColorBufferHeaderSize > limit)
        {
            return -1;
        }

        int stride = package.ReadInt32(at + StaticMeshLodInfoNativeData.StripFlagsSize);
        int vertices = package.ReadInt32(at + StaticMeshLodInfoNativeData.StripFlagsSize + sizeof(int));
        at += StaticMeshLodInfoNativeData.ColorBufferHeaderSize;
        if (stride != StaticMeshLodInfoNativeData.ColorSize || vertices < 0)
        {
            return -1;
        }

        if (vertices == 0)
        {
            return at;
        }

        if (at + StaticMeshLodInfoNativeData.ColorArrayHeaderSize > limit
            || package.ReadInt32(at) != stride
            || package.ReadInt32(at + sizeof(int)) != vertices)
        {
            return -1;
        }

        long colorsEnd = at + StaticMeshLodInfoNativeData.ColorArrayHeaderSize + ((long)vertices * StaticMeshLodInfoNativeData.ColorSize);
        return colorsEnd <= limit ? (int)colorsEnd : -1;
    }

    private static bool EntriesStartWithImports(CookedPackage package, int head, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!package.IsImportReference(package.ReadInt32(head + ComponentNativeData.HeadSize + (ComponentNativeData.EntrySize * i))))
            {
                return false;
            }
        }

        return true;
    }
}
