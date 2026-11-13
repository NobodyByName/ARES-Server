using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Components;

/// <summary>
/// Shape components (box, sphere and capsule components). Their size members lead the class's own chain; each comes
/// from the component or the nearest template that stores it, else from the engine class default.
/// </summary>
internal sealed partial class ComponentReader
{
    private const int ShapeFloatSize = sizeof(float);

    /// <summary>The shape of the box, sphere or capsule component <paramref name="export"/>.</summary>
    /// <exception cref="InvalidDataException">
    /// The component or a template cannot be decoded, or its template chain ends at a class default whose sizes are not known.
    /// </exception>
    public ComponentShape Shape(CookedPackage package, ZenExport export, CollisionComponentKind kind)
    {
        Vector3D? extent = null;
        double? radius = null;
        double? halfHeight = null;
        CookedPackage current = package;
        ZenExport level = export;
        for (int depth = 0; ; depth++)
        {
            UnversionedHeader header = UnversionedHeader.Read(current.Data, CookedPackage.StartOf(level));
            switch (kind)
            {
                case CollisionComponentKind.Box:
                    extent ??= header.Contains(BoxComponentMembers.BoxExtent)
                        ? header.IsNonZero(BoxComponentMembers.BoxExtent) ? current.ReadVector(ShapeMemberOffset(header, BoxComponentMembers.BoxExtent)) : default
                        : null;
                    break;
                case CollisionComponentKind.Sphere:
                    radius ??= ReadShapeFloat(current, header, SphereComponentMembers.SphereRadius);
                    break;
                case CollisionComponentKind.Capsule:
                    halfHeight ??= ReadShapeFloat(current, header, CapsuleComponentMembers.CapsuleHalfHeight);
                    radius ??= ReadShapeFloat(current, header, CapsuleComponentMembers.CapsuleRadius);
                    break;
                case CollisionComponentKind.StaticMesh:
                case CollisionComponentKind.SplineMesh:
                case CollisionComponentKind.Brush:
                default:
                    throw new InvalidDataException($"A {kind} component has no shape");
            }

            PackageObjectIndex template = level.Template;
            if (template.Kind == PackageObjectKind.ScriptImport)
            {
                string defaultObject = packages.ScriptPath(template) ?? template.ToString();
                return ShapeWithDefaults(kind, defaultObject, extent, radius, halfHeight);
            }

            if (template.IsNull)
            {
                throw new InvalidDataException("The component has no template");
            }

            if (depth + 1 > MaxTemplateDepth)
            {
                throw new InvalidDataException("The component's template chain is too long");
            }

            ExportRef next = packages.Resolve(current, template) ?? throw new InvalidDataException("The component's template cannot be resolved");
            if (next.Export.Class != level.Class)
            {
                throw new InvalidDataException("The component's template has another class");
            }

            (current, level) = (next.Package, next.Export);
        }
    }

    /// <summary>
    /// The float member <paramref name="index"/> of a shape's own chain when the header stores it (0 when stored as
    /// zero), else <see langword="null"/>.
    /// </summary>
    private static double? ReadShapeFloat(CookedPackage package, UnversionedHeader header, int index)
    {
        if (!header.Contains(index))
        {
            return null;
        }

        return header.IsNonZero(index) ? package.ReadSingle(ShapeMemberOffset(header, index)) : 0;
    }

    /// <summary>Offset of a shape size member's value; the members before it are the shape's 4-byte size members.</summary>
    private static int ShapeMemberOffset(UnversionedHeader header, int index) => ValueOffset(header, index, _ => ShapeFloatSize);

    private static ComponentShape ShapeWithDefaults(CollisionComponentKind kind, string defaultObject, Vector3D? extent, double? radius, double? halfHeight)
    {
        const double BoxDefault = BoxComponentMembers.DefaultBoxExtent;
        (string knownDefault, bool complete, ComponentShape shape) = kind switch
        {
            CollisionComponentKind.Box => (BoxComponentMembers.DefaultObject, extent is not null,
                new ComponentShape(extent ?? new Vector3D(BoxDefault, BoxDefault, BoxDefault), 0, 0)),
            CollisionComponentKind.Sphere => (SphereComponentMembers.DefaultObject, radius is not null,
                new ComponentShape(default, radius ?? SphereComponentMembers.DefaultSphereRadius, 0)),
            _ => (CapsuleComponentMembers.DefaultObject, radius is not null && halfHeight is not null,
                new ComponentShape(default, radius ?? CapsuleComponentMembers.DefaultCapsuleRadius, halfHeight ?? CapsuleComponentMembers.DefaultCapsuleHalfHeight)),
        };
        return complete || defaultObject == knownDefault
            ? shape
            : throw new InvalidDataException($"The shape sizes of class default {defaultObject} are not known");
    }
}
