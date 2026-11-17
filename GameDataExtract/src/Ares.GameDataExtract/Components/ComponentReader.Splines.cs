using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.GameDataExtract.Components;

/// <summary>The segment a spline mesh component bends its mesh along (<c>FSplineMeshParams</c>), in component space.</summary>
/// <param name="StartPosition">Start of the segment.</param>
/// <param name="StartTangent">Tangent at the start.</param>
/// <param name="StartScale">Scale of the mesh's two cross axes at the start.</param>
/// <param name="StartRoll">Roll around the segment at the start, in radians.</param>
/// <param name="StartOffset">Offset of the mesh from the segment along the frame's cross axes at the start.</param>
/// <param name="EndPosition">End of the segment.</param>
/// <param name="EndTangent">Tangent at the end.</param>
/// <param name="EndScale">Scale of the cross axes at the end.</param>
/// <param name="EndRoll">Roll at the end, in radians.</param>
/// <param name="EndOffset">Offset at the end.</param>
internal sealed record SplineMeshParams(
    Vector3D StartPosition,
    Vector3D StartTangent,
    (double X, double Y) StartScale,
    double StartRoll,
    (double X, double Y) StartOffset,
    Vector3D EndPosition,
    Vector3D EndTangent,
    (double X, double Y) EndScale,
    double EndRoll,
    (double X, double Y) EndOffset);

/// <summary>
/// Spline mesh components: their own members lead the chain (see <see cref="SplineMeshComponentMembers"/>); the static
/// mesh component's follow.
/// </summary>
internal sealed partial class ComponentReader
{
    /// <summary>
    /// The segment of the spline mesh component <paramref name="export"/>, which must be created from the class default:
    /// members of the segment it does not store keep the class default's. Its other own members must be ones whose
    /// meaning is known and that do not change the bend (the up direction, forward axis and boundaries keep the class
    /// default's).
    /// </summary>
    /// <exception cref="InvalidDataException">The segment cannot be decoded.</exception>
    public static SplineMeshParams SplineParams(CookedPackage package, ZenExport export)
    {
        if (export.Template.Kind is not (PackageObjectKind.Null or PackageObjectKind.ScriptImport))
        {
            throw new InvalidDataException("The spline mesh component has a template, whose segment is not read");
        }

        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(export));
        _ = SplineOwnMembersEnd(package, export, header);
        if (!header.IsNonZero(SplineMeshComponentMembers.SplineParams))
        {
            throw new InvalidDataException("The spline mesh component stores no segment");
        }

        int at = header.ValuesOffset;
        UnversionedHeader values = UnversionedHeader.Read(package.Data, at, SplineMeshParamsMembers.MemberCount);
        double[][] members = new double[SplineMeshParamsMembers.MemberCount][];
        at = values.ValuesOffset;
        for (int member = 0; member < SplineMeshParamsMembers.MemberCount; member++)
        {
            int size = SplineMeshParamsMembers.ValueSize(member);
            bool isFloat = size == sizeof(float);
            int count = isFloat ? 1 : size / sizeof(double);
            if (!values.Contains(member))
            {
                members[member] = [.. SplineMeshParamsMembers.DefaultValue(member)];
                continue;
            }

            members[member] = new double[count];
            if (!values.IsNonZero(member))
            {
                continue;
            }

            for (int part = 0; part < count; part++)
            {
                members[member][part] = isFloat ? package.ReadSingle(at) : package.ReadDouble(at + (part * sizeof(double)));
            }

            at += size;
        }

        if (at > CookedPackage.EndOf(export))
        {
            throw new InvalidDataException("The spline mesh segment runs past the export");
        }

        return new SplineMeshParams(
            Vector(members[SplineMeshParamsMembers.StartPosition]),
            Vector(members[SplineMeshParamsMembers.StartTangent]),
            Pair(members[SplineMeshParamsMembers.StartScale]),
            members[SplineMeshParamsMembers.StartRoll][0],
            Pair(members[SplineMeshParamsMembers.StartOffset]),
            Vector(members[SplineMeshParamsMembers.EndPosition]),
            Vector(members[SplineMeshParamsMembers.EndTangent]),
            Pair(members[SplineMeshParamsMembers.EndScale]),
            members[SplineMeshParamsMembers.EndRoll][0],
            Pair(members[SplineMeshParamsMembers.EndOffset]));

        static Vector3D Vector(double[] values) => new(values[0], values[1], values[2]);

        static (double X, double Y) Pair(double[] values) => (values[0], values[1]);
    }

    /// <summary>Offset right after the spline mesh component's own stored members.</summary>
    /// <exception cref="InvalidDataException">An own member is not decoded or the values run past the export.</exception>
    private static int SplineOwnMembersEnd(CookedPackage package, ZenExport export, UnversionedHeader header)
    {
        int at = header.ValuesOffset;
        foreach (UnversionedMember member in header.Members.Where(member => member.Index < SplineMeshComponentMembers.StaticMeshPart))
        {
            if (member.Index != SplineMeshComponentMembers.SplineParams && SplineMeshComponentMembers.FixedValueSize(member.Index) is null)
            {
                throw new InvalidDataException($"The spline mesh component stores member {member.Index}, which is not decoded");
            }

            if (!member.IsNonZero)
            {
                continue;
            }

            if (member.Index == SplineMeshComponentMembers.SplineParams)
            {
                UnversionedHeader values = UnversionedHeader.Read(package.Data, at, SplineMeshParamsMembers.MemberCount);
                at = values.ValuesOffset + values.Members.Where(value => value.IsNonZero).Sum(value => SplineMeshParamsMembers.ValueSize(value.Index));
            }
            else
            {
                at += SplineMeshComponentMembers.FixedValueSize(member.Index)!.Value;
            }
        }

        return at <= CookedPackage.EndOf(export) ? at : throw new InvalidDataException("The spline mesh component's own members run past the export");
    }
}
