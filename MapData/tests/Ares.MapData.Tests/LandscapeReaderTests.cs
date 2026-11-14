using Ares.Common.Maps;
using Ares.MapData.Components;
using Ares.MapData.Landscape;
using Ares.MapData.Materials;
using Ares.MapData.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Tests;

public sealed class LandscapeReaderTests
{
    private const string CellPath = "/Game/Maps/Test/_Generated_/CELL";
    private const int Quads = 2;
    private const int Samples = Quads + 1;

    private static readonly PackageObjectIndex s_proxy = PackageObjectIndex.FromScriptPath(LandscapeStreamingProxyMembers.ClassPath);
    private static readonly PackageObjectIndex s_scene = PackageObjectIndex.FromScriptPath(SceneComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_collision = PackageObjectIndex.FromScriptPath(LandscapeHeightfieldCollisionComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_physicalMaterial = PackageObjectIndex.FromScriptPath(PhysicalMaterialMembers.ClassPath);

    [Fact]
    public void A_proxy_component_becomes_a_world_space_tile()
    {
        HeightFieldTile tile = Assert.Single(Read(Cell(Collision(yaw: 0))));

        Assert.Equal(4, tile.CellIndex);
        Assert.Equal(new Vector3D(1200, 2300, 300), tile.Origin);
        Assert.Equal(100f, tile.SpacingX);
        Assert.Equal(100f, tile.SpacingY);
        Assert.Equal(250f / 128f, tile.HeightScale);
        Assert.Equal(Samples, tile.CountX);
        Assert.Equal(Samples, tile.CountY);
        for (int y = 0; y < Samples; y++)
        {
            for (int x = 0; x < Samples; x++)
            {
                Assert.Equal(HeightAt(x, y), tile.Height(x, y));
            }
        }

        Assert.Equal(new Vector3D(1300, 2500, 300 + (HeightAt(1, 2) * 250.0 / 128.0)), tile.SamplePosition(1, 2));
    }

    [Fact]
    public void Cell_flags_follow_the_field_diagonal_and_holes()
    {
        HeightFieldTile tile = Assert.Single(Read(Cell(Collision(yaw: 0))));

        Assert.Equal(HeightFieldCellFlags.DiagonalFromLowCorner | HeightFieldCellFlags.LowYTriangleHole, tile.Cell(0, 0));
        Assert.Equal(HeightFieldCellFlags.DiagonalFromLowCorner | HeightFieldCellFlags.HighYTriangleHole, tile.Cell(1, 1));
        Assert.Equal(HeightFieldCellFlags.DiagonalFromLowCorner, tile.Cell(1, 0));
        Assert.Equal(HeightFieldCellFlags.DiagonalFromLowCorner, tile.Cell(0, 1));
    }

    [Fact]
    public void Cells_take_the_surface_type_of_their_cooked_physical_material()
    {
        HeightFieldTile tile = Assert.Single(Read(Cell(Collision(yaw: 0, physicalMaterial: true), withPhysicalMaterial: true)));

        Assert.Equal(PhysicalSurfaceType.SaltFlat, tile.Surface(0, 0));
        Assert.Equal(PhysicalSurfaceType.SaltFlat, tile.Surface(1, 1));
    }

    [Fact]
    public void Cells_without_cooked_physical_materials_have_the_default_surface_type()
    {
        HeightFieldTile tile = Assert.Single(Read(Cell(Collision(yaw: 0))));

        Assert.Equal(PhysicalSurfaceType.Default, tile.Surface(1, 0));
    }

    [Fact]
    public void Rotated_components_are_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => Read(Cell(Collision(yaw: 90))));

        Assert.Contains("Rotated", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Components_without_cooked_collision_give_no_tile()
    {
        Assert.Empty(Read(Cell(Collision(yaw: 0, cooked: false))));
    }

    private static CookedPackage Cell(byte[] collision, bool withPhysicalMaterial = false)
    {
        List<(string Name, PackageObjectIndex Outer, PackageObjectIndex Class, ulong PublicHash, byte[] Payload)> exports = ProxyExports(collision);
        if (withPhysicalMaterial)
        {
            exports.Add(("PM_SaltFlat", PackageObjectIndex.Null, s_physicalMaterial, 0,
                [.. PropertyBlocks.Header(PhysicalMaterialMembers.SurfaceType), (byte)PhysicalSurfaceType.SaltFlat]));
        }

        return CookedPackage.Parse(SyntheticArchives.Package(CellPath, exports));
    }

    private static IReadOnlyList<HeightFieldTile> Read(CookedPackage cell)
    {
        PackageCache packages = new(path => path == CellPath ? cell : null, _ => null);
        return new LandscapeReader(new ComponentReader(packages), new SurfaceLibrary(packages)).ReadCell(cell, 4);
    }

    /// <summary>
    /// A landscape streaming proxy with its root component and <paramref name="collision"/>: the first three exports of a
    /// package. Its tile has origin (1200, 2300, <paramref name="height"/>), 100 cm spacing and the heights of <see cref="HeightAt"/>.
    /// </summary>
    internal static List<(string Name, PackageObjectIndex Outer, PackageObjectIndex Class, ulong PublicHash, byte[] Payload)> ProxyExports(
        byte[] collision, double height = 300)
    {
        byte[] root =
        [
            .. PropertyBlocks.Header(SceneComponentMembers.RelativeLocation, SceneComponentMembers.RelativeScale3D),
            .. PropertyBlocks.Vector(1000, 2000, height),
            .. PropertyBlocks.Vector(100, 100, 250),
            .. new byte[ComponentNativeData.HeadSize],
        ];
        return
        [
            ("LandscapeStreamingProxy_0", PackageObjectIndex.Null, s_proxy, 0, []),
            ("RootComponent0", PackageObjectIndex.FromExport(0), s_scene, 0, root),
            ("CollisionComponent_0", PackageObjectIndex.FromExport(0), s_collision, 0, collision),
        ];
    }

    /// <summary>Landscape sample (x, y) has height 10·x + y.</summary>
    internal static short HeightAt(int x, int y) => (short)((10 * x) + y);

    /// <summary>
    /// A landscape collision component; with <paramref name="physicalMaterial"/> its cooked physical materials name the
    /// export after the proxy's three (see <see cref="Cell"/>).
    /// </summary>
    internal static byte[] Collision(double yaw, bool cooked = true, bool physicalMaterial = false)
    {
        const int ScenePart = LandscapeHeightfieldCollisionComponentMembers.ScenePart;
        List<int> members =
        [
            LandscapeHeightfieldCollisionComponentMembers.ComponentLayerInfos,
            LandscapeHeightfieldCollisionComponentMembers.CollisionSizeQuads,
            LandscapeHeightfieldCollisionComponentMembers.CollisionScale,
            LandscapeHeightfieldCollisionComponentMembers.CachedLocalBox,
        ];
        if (physicalMaterial)
        {
            members.Add(LandscapeHeightfieldCollisionComponentMembers.CookedPhysicalMaterials);
        }

        members.AddRange(
        [
            ScenePart + SceneComponentMembers.AttachParent,
            ScenePart + SceneComponentMembers.RelativeLocation,
        ]);
        if (yaw != 0)
        {
            members.Add(ScenePart + SceneComponentMembers.RelativeRotation);
        }

        byte[] field = cooked ? HeightField() : [];
        List<byte> bytes =
        [
            .. PropertyBlocks.Header([.. members]),
            .. BitConverter.GetBytes(1),
            .. BitConverter.GetBytes(-1),
            .. BitConverter.GetBytes(Quads),
            .. BitConverter.GetBytes(1f),
            .. new byte[49],
            .. (physicalMaterial ? [.. BitConverter.GetBytes(1), .. BitConverter.GetBytes(4)] : Array.Empty<byte>()),
            .. BitConverter.GetBytes(2),
            .. PropertyBlocks.Vector(2, 3, 0),
        ];
        if (yaw != 0)
        {
            bytes.AddRange(PropertyBlocks.Vector(0, yaw, 0));
        }

        bytes.AddRange(BitConverter.GetBytes(0));
        bytes.AddRange(BitConverter.GetBytes(0));
        bytes.AddRange(BitConverter.GetBytes(cooked ? 1 : 0));
        if (!cooked)
        {
            return [.. bytes];
        }

        bytes.AddRange(BitConverter.GetBytes(1));
        bytes.AddRange(BitConverter.GetBytes(field.Length));
        bytes.AddRange(field);
        return [.. bytes];
    }

    /// <summary>
    /// A PhysX height field whose row r, column c is landscape sample (2 − r, c). Field cell (1, 0) has a hole first
    /// triangle; field cell (0, 1) has a hole second triangle whose material also carries the reserved bit.
    /// </summary>
    private static byte[] HeightField()
    {
        List<byte> bytes =
        [
            .. "NXS"u8,
            1,
            .. "HFHF"u8,
            .. BitConverter.GetBytes(1u),
            .. BitConverter.GetBytes((uint)Samples),
            .. BitConverter.GetBytes((uint)Samples),
            .. BitConverter.GetBytes((float)(Samples - 1)),
            .. BitConverter.GetBytes((float)(Samples - 1)),
            .. BitConverter.GetBytes((float)Samples),
            .. BitConverter.GetBytes(-1f),
            .. BitConverter.GetBytes(0f),
            .. BitConverter.GetBytes((ushort)0),
            .. BitConverter.GetBytes(1u),
        ];
        foreach (float value in new[] { 0f, 0f, 0f, Samples - 1, 22f, Samples - 1 })
        {
            bytes.AddRange(BitConverter.GetBytes(value));
        }

        bytes.AddRange(BitConverter.GetBytes(4u));
        bytes.AddRange(BitConverter.GetBytes((uint)(Samples * Samples)));
        bytes.AddRange(BitConverter.GetBytes(0f));
        bytes.AddRange(BitConverter.GetBytes(22f));
        for (int row = 0; row < Samples; row++)
        {
            for (int column = 0; column < Samples; column++)
            {
                bytes.AddRange(BitConverter.GetBytes(HeightAt(Samples - 1 - row, column)));
                bytes.Add(row == 1 && column == 0 ? HeightFieldSample.HoleMaterial : (byte)0);
                bytes.Add(row == 0 && column == 1 ? (byte)(0x80 | HeightFieldSample.HoleMaterial) : (byte)0);
            }
        }

        return [.. bytes];
    }
}
