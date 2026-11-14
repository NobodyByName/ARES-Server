using Ares.Common.Maps;
using Ares.MapData.Components;
using Ares.MapData.Materials;
using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Landscape;

/// <summary>
/// Reads the landscape collision of cell packages: every <c>ULandscapeHeightfieldCollisionComponent</c> of a
/// landscape streaming proxy becomes one <see cref="HeightFieldTile"/> in world space. A tile's grid is axis-aligned,
/// so components whose world transform rotates them are rejected.
/// </summary>
internal sealed class LandscapeReader
{
    private const double RotationTolerance = 1e-9;

    private static readonly PackageObjectIndex s_proxy = PackageObjectIndex.FromScriptPath(LandscapeStreamingProxyMembers.ClassPath);
    private static readonly PackageObjectIndex s_collision = PackageObjectIndex.FromScriptPath(LandscapeHeightfieldCollisionComponentMembers.ClassPath);

    private readonly ComponentReader _components;
    private readonly SurfaceLibrary _surfaces;

    /// <summary>
    /// A reader that places components with <paramref name="components"/> (which reads the proxies' root components) and
    /// gives their cells the surface types of their cooked physical materials from <paramref name="surfaces"/>.
    /// </summary>
    public LandscapeReader(ComponentReader components, SurfaceLibrary surfaces)
    {
        _components = components;
        _surfaces = surfaces;
    }

    /// <summary>
    /// The height-field tiles of <paramref name="cell"/>, in export order. Components without cooked collision give
    /// no tile.
    /// </summary>
    /// <param name="cell">The cell package.</param>
    /// <param name="cellIndex">Index of the cell in the map file, stored in each tile.</param>
    /// <exception cref="InvalidDataException">A landscape collision component does not decode or cannot be placed.</exception>
    public IReadOnlyList<HeightFieldTile> ReadCell(CookedPackage cell, int cellIndex)
    {
        List<HeightFieldTile> tiles = [];
        foreach (ZenExport export in cell.Exports)
        {
            if (export.Class != s_collision)
            {
                continue;
            }

            try
            {
                if (ReadTile(cell, export, cellIndex) is { } tile)
                {
                    tiles.Add(tile);
                }
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{cell.Header.ExportPath(export.Index)}: {error.Message}", error);
            }
        }

        return tiles;
    }

    private HeightFieldTile? ReadTile(CookedPackage cell, ZenExport export, int cellIndex)
    {
        if (export.Outer.Kind != PackageObjectKind.Export || cell.Exports[export.Outer.ExportIndex].Class != s_proxy)
        {
            throw new InvalidDataException("The landscape collision component is not owned by a landscape streaming proxy");
        }

        if (export.Template.Kind is not (PackageObjectKind.Null or PackageObjectKind.ScriptImport))
        {
            throw new InvalidDataException("Landscape collision components created from a template are not supported");
        }

        LandscapeComponentValues values = LandscapeComponentValues.Read(cell, export);
        if (values.CookedHeightField.IsEmpty)
        {
            return null;
        }

        HeightField field = PhysXCookedData.ReadHeightField(values.CookedHeightField.Span);
        int samples = values.CollisionSizeQuads + 1;
        if (field.Rows != samples || field.Columns != samples)
        {
            throw new InvalidDataException($"The height field has {field.Rows} × {field.Columns} samples for {values.CollisionSizeQuads} quads");
        }

        if (!(values.CollisionScale > 0))
        {
            throw new InvalidDataException($"Invalid collision scale {values.CollisionScale}");
        }

        (Quaternion rotation, Vector3D translation, Vector3D scale) = WorldTransform(cell, export, values);
        if (Math.Abs(rotation.X) > RotationTolerance || Math.Abs(rotation.Y) > RotationTolerance || Math.Abs(rotation.Z) > RotationTolerance)
        {
            throw new InvalidDataException($"Rotated landscape collision is not supported (rotation {rotation})");
        }

        if (scale is not { X: > 0, Y: > 0, Z: > 0 })
        {
            throw new InvalidDataException($"Landscape collision with a non-positive scale {scale} is not supported");
        }

        PhysicalSurfaceType[] surfaces = [.. values.CookedPhysicalMaterials.Select(material => _surfaces.OfPhysicalMaterial(material) ?? PhysicalSurfaceType.Default)];
        try
        {
            return HeightFieldTile.FromLandscape(
                cellIndex,
                field,
                translation,
                (float)(scale.X * values.CollisionScale),
                (float)(scale.Y * values.CollisionScale),
                (float)(scale.Z * LandscapeCollision.HeightScale),
                surfaces);
        }
        catch (ArgumentException error)
        {
            throw new InvalidDataException(error.Message, error);
        }
    }

    /// <summary>The component's world transform: its own relative transform on the world transform of its attach parent, the proxy's root.</summary>
    private Transform WorldTransform(CookedPackage cell, ZenExport export, LandscapeComponentValues values)
    {
        int parent = values.AttachParent;
        if (parent <= 0 || parent > cell.Exports.Count)
        {
            throw new InvalidDataException("The landscape collision component has no attach parent in its cell");
        }

        ZenExport root = cell.Exports[parent - 1];
        if (root.Outer != export.Outer)
        {
            throw new InvalidDataException("The landscape collision component is attached outside its proxy");
        }

        if (values.AttachSocket is { } socket && socket != CookedPackage.NoneName)
        {
            throw new InvalidDataException($"The landscape collision component is attached to socket {socket}");
        }

        Transform rootWorld = _components.WorldTransform(cell, root);
        Transform own = TransformMath.FromParts(values.Location, values.Rotation, values.Scale);
        return TransformMath.Compose(own, rootWorld, values.Absolute);
    }
}
