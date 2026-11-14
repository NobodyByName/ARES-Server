using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.MapData.Components;
using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Ares.MapData.Bodies;

/// <summary>A convex element of a body setup's simple shapes, as stored before cooking.</summary>
/// <param name="Vertices">Element vertices in element space.</param>
/// <param name="Transform">Element transform into body space, when the element stores one.</param>
internal sealed record ConvexElement(ImmutableArray<Vector3D> Vertices, Transform? Transform);

/// <summary>A body's override of the walkable floor slope for characters standing on it.</summary>
/// <param name="Behavior">The override's behaviour (0 for none).</param>
/// <param name="Angle">The angle the behaviour uses, in degrees.</param>
internal readonly record struct WalkableSlope(byte Behavior, float Angle);

/// <summary>The decoded contents of one body setup export.</summary>
/// <param name="Spheres">Sphere elements in body space.</param>
/// <param name="Boxes">Box elements in body space.</param>
/// <param name="Capsules">Capsule elements in body space.</param>
/// <param name="ConvexElements">Convex elements as stored; their cooked hulls are in <paramref name="Cooked"/>.</param>
/// <param name="WalkableSlope">The walkable slope override, or <see langword="null"/> when the body stores none.</param>
/// <param name="TraceFlag">Which collision the body offers to simple and complex queries.</param>
/// <param name="DefaultInstance">Collision settings of components that use the body's defaults, or <see langword="null"/>
/// when the body stores none (the class defaults apply).</param>
/// <param name="Cooked">Cooked convex hulls and triangle meshes, or <see langword="null"/> when the body stores none.</param>
/// <param name="PhysMaterial">Physical material of the simple shapes as stored, or <see langword="null"/> when the body stores none.</param>
internal sealed record BodySetupContents(
    ImmutableArray<CollisionSphere> Spheres,
    ImmutableArray<CollisionBox> Boxes,
    ImmutableArray<CollisionCapsule> Capsules,
    ImmutableArray<ConvexElement> ConvexElements,
    WalkableSlope? WalkableSlope,
    CollisionTraceFlag TraceFlag,
    BodyInstanceValues? DefaultInstance,
    CookedBodyGeometry? Cooked,
    StoredReference? PhysMaterial = null);

/// <summary>
/// Reads body setup exports: the unversioned properties (simple shapes, default collision, trace flag) front to back,
/// then the native data after them (see <see cref="BodySetupNativeData"/>), which must end exactly at the export's end.
/// </summary>
internal static class BodySetupReader
{
    private const int MaxElements = 4096;
    private const int MaxArrayLength = 1 << 20;

    /// <summary>Reads the body setup <paramref name="export"/> of <paramref name="package"/>.</summary>
    /// <exception cref="InvalidDataException">The export is malformed or stores members or a cooked format this reader does not know.</exception>
    public static BodySetupContents Read(CookedPackage package, ZenExport export)
    {
        int start = CookedPackage.StartOf(export);
        int end = CookedPackage.EndOf(export);
        UnversionedHeader header = UnversionedHeader.Read(package.Data, start);
        int at = header.ValuesOffset;
        AggregateGeometry geometry = AggregateGeometry.Empty;
        CollisionTraceFlag traceFlag = CollisionTraceFlag.UseDefault;
        BodyInstanceValues? defaultInstance = null;
        WalkableSlope? walkableSlope = null;
        StoredReference? physMaterial = null;
        foreach (UnversionedMember member in header.Members.Where(member => member.IsNonZero))
        {
            switch (member.Index)
            {
                case BodySetupMembers.AggregateGeometry:
                    geometry = ReadAggregateGeometry(package, ref at);
                    break;
                case BodySetupMembers.WalkableSlopeOverride:
                    walkableSlope = ReadWalkableSlope(package, ref at);
                    break;
                case BodySetupMembers.DefaultInstance:
                    defaultInstance = BodyInstanceReader.TryRead(package, at, out at)
                        ?? throw new InvalidDataException($"{package.Name}: the default body instance of {export.Name} cannot be read");
                    break;
                case BodySetupMembers.PhysMaterial:
                    physMaterial = new StoredReference(package, package.ReadInt32(at));
                    at += BodySetupMembers.FixedValueSize(member.Index) ?? 0;
                    break;
                case BodySetupMembers.CollisionTraceFlag:
                    byte flag = package.ReadByte(at);
                    traceFlag = flag <= (byte)CollisionTraceFlag.ComplexAsSimple
                        ? (CollisionTraceFlag)flag
                        : throw new InvalidDataException($"{package.Name}: {export.Name} has trace flag {flag}");
                    at++;
                    break;
                default:
                    at += BodySetupMembers.FixedValueSize(member.Index)
                        ?? throw new InvalidDataException($"{package.Name}: body setup member {member.Index} of {export.Name} has an unknown size");
                    break;
            }
        }

        if (at > end)
        {
            throw new InvalidDataException($"{package.Name}: the properties of {export.Name} run past the export");
        }

        BodySetupCookedFormat? format = BodySetupNativeData.Read(package.Data.AsMemory(at, end - at));
        CookedBodyGeometry? cooked = format is { } found ? ReadCookedGeometry(package, export, found) : null;
        return new BodySetupContents(geometry.Spheres, geometry.Boxes, geometry.Capsules, geometry.ConvexElements, walkableSlope, traceFlag, defaultInstance, cooked,
            physMaterial);
    }

    private static CookedBodyGeometry ReadCookedGeometry(CookedPackage package, ZenExport export, BodySetupCookedFormat format)
    {
        string formatName = format.FormatNameIndex >= 0 && format.FormatNameIndex < package.Header.Names.Count
            ? NameBatch.Display(package.Header.Names[format.FormatNameIndex], (uint)format.FormatNameNumber)
            : throw new InvalidDataException($"{package.Name}: the cooked format name of {export.Name} is outside the name map");
        return formatName == BodySetupNativeData.CookedFormatName
            ? PhysXCookedData.ReadBodyGeometry(format.Payload.Span)
            : throw new InvalidDataException($"{package.Name}: {export.Name} has cooked format {formatName}");
    }

    private static AggregateGeometry ReadAggregateGeometry(CookedPackage package, ref int at)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, at);
        at = header.ValuesOffset;
        ImmutableArray<CollisionSphere>.Builder spheres = ImmutableArray.CreateBuilder<CollisionSphere>();
        ImmutableArray<CollisionBox>.Builder boxes = ImmutableArray.CreateBuilder<CollisionBox>();
        ImmutableArray<CollisionCapsule>.Builder capsules = ImmutableArray.CreateBuilder<CollisionCapsule>();
        ImmutableArray<ConvexElement>.Builder convexElements = ImmutableArray.CreateBuilder<ConvexElement>();
        foreach (UnversionedMember member in header.Members.Where(member => member.IsNonZero))
        {
            int count = ReadCount(package, ref at, MaxElements);
            for (int i = 0; i < count; i++)
            {
                switch (member.Index)
                {
                    case AggregateGeometryMembers.SphereElements:
                        spheres.Add(ReadSphere(package, ref at));
                        break;
                    case AggregateGeometryMembers.BoxElements:
                        boxes.Add(ReadBox(package, ref at));
                        break;
                    case AggregateGeometryMembers.CapsuleElements:
                        capsules.Add(ReadCapsule(package, ref at));
                        break;
                    case AggregateGeometryMembers.ConvexElements:
                        convexElements.Add(ReadConvex(package, ref at));
                        break;
                    case AggregateGeometryMembers.TaperedCapsuleElements:
                        throw new InvalidDataException($"{package.Name}: tapered capsule elements are not supported");
                    default:
                        throw new InvalidDataException($"{package.Name}: aggregate geometry member {member.Index} is not known");
                }
            }
        }

        return new AggregateGeometry(spheres.ToImmutable(), boxes.ToImmutable(), capsules.ToImmutable(), convexElements.ToImmutable());
    }

    private static CollisionSphere ReadSphere(CookedPackage package, ref int at)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, at);
        at = header.ValuesOffset;
        Vector3D center = default;
        float radius = 0;
        foreach (UnversionedMember member in header.Members.Where(member => member.IsNonZero))
        {
            switch (member.Index)
            {
                case SphereElementMembers.Center:
                    center = ReadVector(package, ref at);
                    break;
                case SphereElementMembers.Radius:
                    radius = ReadSingle(package, ref at);
                    break;
                default:
                    SkipShapeElementMember(package, member.Index - SphereElementMembers.ShapeElementPart, ref at);
                    break;
            }
        }

        return new CollisionSphere(ToVector3(center), radius);
    }

    private static CollisionBox ReadBox(CookedPackage package, ref int at)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, at);
        at = header.ValuesOffset;
        Vector3D center = default;
        Rotator rotation = default;
        float x = 0;
        float y = 0;
        float z = 0;
        foreach (UnversionedMember member in header.Members.Where(member => member.IsNonZero))
        {
            switch (member.Index)
            {
                case BoxElementMembers.Center:
                    center = ReadVector(package, ref at);
                    break;
                case BoxElementMembers.Rotation:
                    rotation = ReadRotator(package, ref at);
                    break;
                case BoxElementMembers.X:
                    x = ReadSingle(package, ref at);
                    break;
                case BoxElementMembers.Y:
                    y = ReadSingle(package, ref at);
                    break;
                case BoxElementMembers.Z:
                    z = ReadSingle(package, ref at);
                    break;
                default:
                    SkipShapeElementMember(package, member.Index - BoxElementMembers.ShapeElementPart, ref at);
                    break;
            }
        }

        return new CollisionBox(ToVector3(center), ToQuaternion(rotation), new NumericsVector3(x / 2, y / 2, z / 2));
    }

    private static CollisionCapsule ReadCapsule(CookedPackage package, ref int at)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, at);
        at = header.ValuesOffset;
        Vector3D center = default;
        Rotator rotation = default;
        float radius = 0;
        float length = 0;
        foreach (UnversionedMember member in header.Members.Where(member => member.IsNonZero))
        {
            switch (member.Index)
            {
                case CapsuleElementMembers.Center:
                    center = ReadVector(package, ref at);
                    break;
                case CapsuleElementMembers.Rotation:
                    rotation = ReadRotator(package, ref at);
                    break;
                case CapsuleElementMembers.Radius:
                    radius = ReadSingle(package, ref at);
                    break;
                case CapsuleElementMembers.Length:
                    length = ReadSingle(package, ref at);
                    break;
                default:
                    SkipShapeElementMember(package, member.Index - CapsuleElementMembers.ShapeElementPart, ref at);
                    break;
            }
        }

        return new CollisionCapsule(ToVector3(center), ToQuaternion(rotation), radius, length / 2);
    }

    private static ConvexElement ReadConvex(CookedPackage package, ref int at)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, at);
        at = header.ValuesOffset;
        ImmutableArray<Vector3D> vertices = [];
        Transform? transform = null;
        foreach (UnversionedMember member in header.Members)
        {
            switch (member.Index)
            {
                case ConvexElementMembers.VertexData when member.IsNonZero:
                    int vertexCount = ReadCount(package, ref at, MaxArrayLength);
                    ImmutableArray<Vector3D>.Builder read = ImmutableArray.CreateBuilder<Vector3D>(vertexCount);
                    for (int i = 0; i < vertexCount; i++)
                    {
                        read.Add(ReadVector(package, ref at));
                    }

                    vertices = read.MoveToImmutable();
                    break;
                case ConvexElementMembers.IndexData when member.IsNonZero:
                    int indexCount = ReadCount(package, ref at, MaxArrayLength);
                    at += indexCount * sizeof(int);
                    break;
                case ConvexElementMembers.ElementBox when member.IsNonZero:
                    at += ConvexElementMembers.ElementBoxSize;
                    break;
                case ConvexElementMembers.Transform:
                    transform = member.IsNonZero ? ReadTransform(package, ref at) : new Transform(default, default, default);
                    break;
                default:
                    if (member.IsNonZero)
                    {
                        SkipShapeElementMember(package, member.Index - ConvexElementMembers.ShapeElementPart, ref at);
                    }

                    break;
            }
        }

        return new ConvexElement(vertices, transform);
    }

    private static WalkableSlope ReadWalkableSlope(CookedPackage package, ref int at)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, at);
        at = header.ValuesOffset;
        byte behavior = 0;
        float angle = 0;
        foreach (UnversionedMember member in header.Members.Where(member => member.IsNonZero))
        {
            switch (member.Index)
            {
                case WalkableSlopeOverrideMembers.WalkableSlopeBehavior:
                    behavior = package.ReadByte(at);
                    at++;
                    break;
                case WalkableSlopeOverrideMembers.WalkableSlopeAngle:
                    angle = ReadSingle(package, ref at);
                    break;
                default:
                    throw new InvalidDataException($"{package.Name}: walkable slope member {member.Index} is not known");
            }
        }

        return new WalkableSlope(behavior, angle);
    }

    /// <summary>An <c>FTransform</c>: members it does not list keep the identity transform's values; members stored as zero are zero.</summary>
    private static Transform ReadTransform(CookedPackage package, ref int at)
    {
        UnversionedHeader header = UnversionedHeader.Read(package.Data, at, TransformMembers.MemberCount);
        at = header.ValuesOffset;
        Quaternion rotation = Quaternion.Identity;
        Vector3D translation = default;
        Vector3D scale = Vector3D.One;
        foreach (UnversionedMember member in header.Members)
        {
            switch (member.Index)
            {
                case TransformMembers.Rotation:
                    rotation = member.IsNonZero
                        ? new Quaternion(ReadDouble(package, ref at), ReadDouble(package, ref at), ReadDouble(package, ref at), ReadDouble(package, ref at))
                        : default;
                    break;
                case TransformMembers.Translation:
                    translation = member.IsNonZero ? ReadVector(package, ref at) : default;
                    break;
                case TransformMembers.Scale3D:
                    scale = member.IsNonZero ? ReadVector(package, ref at) : default;
                    break;
                default:
                    throw new InvalidDataException($"{package.Name}: transform member {member.Index} is not known");
            }
        }

        return new Transform(rotation, translation, scale);
    }

    private static void SkipShapeElementMember(CookedPackage package, int member, ref int at)
    {
        at += member >= 0 ? ShapeElementMembers.FixedValueSize(member) ?? throw UnknownShapeMember() : throw UnknownShapeMember();
        return;

        InvalidDataException UnknownShapeMember() => new($"{package.Name}: shape element member {member} is not known");
    }

    private static int ReadCount(CookedPackage package, ref int at, int limit)
    {
        int count = package.ReadInt32(at);
        at += sizeof(int);
        return count >= 0 && count <= limit ? count : throw new InvalidDataException($"{package.Name}: array length {count} at offset {at - sizeof(int)} is out of range");
    }

    private static float ReadSingle(CookedPackage package, ref int at)
    {
        float value = package.ReadSingle(at);
        at += sizeof(float);
        return value;
    }

    private static double ReadDouble(CookedPackage package, ref int at)
    {
        double value = package.ReadDouble(at);
        at += sizeof(double);
        return value;
    }

    private static Vector3D ReadVector(CookedPackage package, ref int at)
    {
        Vector3D value = package.ReadVector(at);
        at += 3 * sizeof(double);
        return value;
    }

    private static Rotator ReadRotator(CookedPackage package, ref int at)
    {
        Rotator value = package.ReadRotator(at);
        at += 3 * sizeof(double);
        return value;
    }

    private static NumericsVector3 ToVector3(Vector3D value) => new((float)value.X, (float)value.Y, (float)value.Z);

    private static NumericsQuaternion ToQuaternion(Rotator rotation)
    {
        Quaternion q = TransformMath.ToQuaternion(rotation);
        return new NumericsQuaternion((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);
    }

    private sealed record AggregateGeometry(
        ImmutableArray<CollisionSphere> Spheres,
        ImmutableArray<CollisionBox> Boxes,
        ImmutableArray<CollisionCapsule> Capsules,
        ImmutableArray<ConvexElement> ConvexElements)
    {
        public static AggregateGeometry Empty { get; } = new([], [], [], []);
    }
}
