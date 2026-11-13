using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Ares.Structs.Core;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Quaternion = System.Numerics.Quaternion;

namespace Ares.Common.Maps;

/// <summary>
/// Reads and writes <see cref="MapCollision"/> in the ARES map-collision format, version <see cref="FormatVersion"/>.
/// </summary>
/// <remarks>
/// <para>
/// All values are little-endian. Strings are an int32 byte count followed by UTF-8. A vector is three float32
/// (X, Y, Z), a double vector three float64, a quaternion four float32 (X, Y, Z, W), bounds a minimum then a maximum
/// vector. Counts are int32.
/// </para>
/// <para>
/// Header: the 8 ASCII bytes <c>ARESCOLL</c>, the format version (int32), the game build id (int32) and the map name.
/// Four sections follow in this order, each a 4-byte ASCII tag, the payload length (int64) and the payload:
/// </para>
/// <list type="bullet">
/// <item><c>CELL</c>: cell count; per cell its package name, data-layer count and data-layer names.</item>
/// <item><c>BODY</c>: body count; per body its name, then boxes (count; centre, rotation, half extents), spheres
/// (count; centre, radius), capsules (count; centre, rotation, radius, half length), convex hulls (count; per hull the
/// vertex count and vertices, the polygon count and per polygon its plane normal, plane distance, first index and index
/// count as int32, the index count and byte indices, the bounds) and triangle meshes (count; per mesh the vertex count
/// and vertices, the index count and int32 indices, the bounds).</item>
/// <item><c>PLAC</c>: placement count; per placement the body index, cell index, flags (uint32), translation (double
/// vector), rotation and scale. Placement bounds are not stored; they are computed from the body when reading.</item>
/// <item><c>HFLD</c>: tile count; per tile the cell index, origin (double vector), X spacing, Y spacing and height scale
/// (float32), sample counts along X and Y, the int16 heights and the byte cell flags, both X-fastest.</item>
/// </list>
/// <para>
/// Reading checks the magic, then the format version, then the game build, and every count, index and length; a
/// payload must be consumed exactly. Failures raise <see cref="MapCollisionFormatException"/>.
/// </para>
/// </remarks>
public static class MapCollisionFile
{
    /// <summary>The format version this code reads and writes.</summary>
    public const int FormatVersion = 1;

    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static ReadOnlySpan<byte> Magic => "ARESCOLL"u8;

    private static ReadOnlySpan<byte> CellsTag => "CELL"u8;

    private static ReadOnlySpan<byte> BodiesTag => "BODY"u8;

    private static ReadOnlySpan<byte> PlacementsTag => "PLAC"u8;

    private static ReadOnlySpan<byte> HeightFieldsTag => "HFLD"u8;

    /// <summary>Writes a map to a stream.</summary>
    public static void Write(Stream stream, MapCollision map)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(map);
        RequireLittleEndian();
        using BinaryWriter writer = new(stream, s_utf8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write(map.GameBuildId);
        WriteString(writer, map.MapName);
        WriteSection(writer, CellsTag, map, WriteCells);
        WriteSection(writer, BodiesTag, map, WriteBodies);
        WriteSection(writer, PlacementsTag, map, WritePlacements);
        WriteSection(writer, HeightFieldsTag, map, WriteHeightFields);
    }

    /// <summary>Reads a map written for the current game build (<see cref="GameBuild.SteamBuildId"/>).</summary>
    /// <exception cref="MapCollisionFormatException">The stream does not hold a valid map for this build.</exception>
    public static MapCollision Read(Stream stream) => Read(stream, GameBuild.SteamBuildId);

    /// <summary>Reads a map, requiring it to be written for a given game build.</summary>
    /// <exception cref="MapCollisionFormatException">The stream does not hold a valid map for that build.</exception>
    public static MapCollision Read(Stream stream, int expectedGameBuildId)
    {
        ArgumentNullException.ThrowIfNull(stream);
        RequireLittleEndian();
        using BinaryReader reader = new(stream, s_utf8, leaveOpen: true);
        byte[] magic = ReadExactly(reader, Magic.Length, "header");
        if (!magic.AsSpan().SequenceEqual(Magic))
        {
            throw new MapCollisionFormatException(MapCollisionFileError.NotAMapCollisionFile, "Not a map-collision file.");
        }

        int version = BinaryPrimitives.ReadInt32LittleEndian(ReadExactly(reader, 4, "header"));
        if (version != FormatVersion)
        {
            throw new MapCollisionFormatException(
                MapCollisionFileError.UnsupportedFormatVersion,
                $"Map-collision format version {version} is not supported (expected {FormatVersion}).");
        }

        int gameBuildId = BinaryPrimitives.ReadInt32LittleEndian(ReadExactly(reader, 4, "header"));
        if (gameBuildId != expectedGameBuildId)
        {
            throw new MapCollisionFormatException(
                MapCollisionFileError.GameBuildMismatch,
                $"Map-collision file is for game build {gameBuildId}, expected {expectedGameBuildId}.");
        }

        int nameLength = BinaryPrimitives.ReadInt32LittleEndian(ReadExactly(reader, 4, "header"));
        if (nameLength is <= 0 or > 4096)
        {
            throw Malformed("header", $"invalid map name length {nameLength}");
        }

        string mapName = Decode(ReadExactly(reader, nameLength, "header"), "header");

        ImmutableArray<MapCell> cells = ReadCells(ReadSection(reader, CellsTag, "cell"));
        ImmutableArray<CollisionBody> bodies = ReadBodies(ReadSection(reader, BodiesTag, "body"));
        ImmutableArray<BodyPlacement> placements = ReadPlacements(ReadSection(reader, PlacementsTag, "placement"), bodies, cells.Length);
        ImmutableArray<HeightFieldTile> heightFields = ReadHeightFields(ReadSection(reader, HeightFieldsTag, "height-field"), cells.Length);
        return new MapCollision(gameBuildId, mapName, cells, bodies, placements, heightFields);
    }

    private static void RequireLittleEndian()
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new PlatformNotSupportedException("Map-collision files need a little-endian machine.");
        }
    }

    private static void WriteSection(BinaryWriter writer, ReadOnlySpan<byte> tag, MapCollision map, Action<BinaryWriter, MapCollision> writePayload)
    {
        using MemoryStream payload = new();
        using (BinaryWriter payloadWriter = new(payload, s_utf8, leaveOpen: true))
        {
            writePayload(payloadWriter, map);
        }

        writer.Write(tag);
        writer.Write(payload.Length);
        writer.Write(payload.GetBuffer().AsSpan(0, (int)payload.Length));
    }

    private static void WriteCells(BinaryWriter writer, MapCollision map)
    {
        writer.Write(map.Cells.Length);
        foreach (MapCell cell in map.Cells)
        {
            WriteString(writer, cell.PackageName);
            writer.Write(cell.DataLayers.Length);
            foreach (string layer in cell.DataLayers)
            {
                WriteString(writer, layer);
            }
        }
    }

    private static void WriteBodies(BinaryWriter writer, MapCollision map)
    {
        writer.Write(map.Bodies.Length);
        foreach (CollisionBody body in map.Bodies)
        {
            WriteString(writer, body.Name);
            writer.Write(body.Boxes.Length);
            foreach (CollisionBox box in body.Boxes)
            {
                WriteVector(writer, box.Center);
                WriteQuaternion(writer, box.Rotation);
                WriteVector(writer, box.HalfExtents);
            }

            writer.Write(body.Spheres.Length);
            foreach (CollisionSphere sphere in body.Spheres)
            {
                WriteVector(writer, sphere.Center);
                writer.Write(sphere.Radius);
            }

            writer.Write(body.Capsules.Length);
            foreach (CollisionCapsule capsule in body.Capsules)
            {
                WriteVector(writer, capsule.Center);
                WriteQuaternion(writer, capsule.Rotation);
                writer.Write(capsule.Radius);
                writer.Write(capsule.HalfLength);
            }

            writer.Write(body.ConvexHulls.Length);
            foreach (ConvexHull hull in body.ConvexHulls)
            {
                WriteArray(writer, hull.Vertices.AsSpan());
                writer.Write(hull.Polygons.Length);
                foreach (HullPolygon polygon in hull.Polygons)
                {
                    WriteVector(writer, polygon.Plane.Normal);
                    writer.Write(polygon.Plane.D);
                    writer.Write(polygon.FirstIndex);
                    writer.Write(polygon.IndexCount);
                }

                WriteArray(writer, hull.PolygonVertexIndices.AsSpan());
                WriteBounds(writer, hull.Bounds);
            }

            writer.Write(body.TriangleMeshes.Length);
            foreach (TriangleMesh mesh in body.TriangleMeshes)
            {
                WriteArray(writer, mesh.Vertices.AsSpan());
                WriteArray(writer, mesh.Indices.AsSpan());
                WriteBounds(writer, mesh.Bounds);
            }
        }
    }

    private static void WritePlacements(BinaryWriter writer, MapCollision map)
    {
        writer.Write(map.Placements.Length);
        foreach (BodyPlacement placement in map.Placements)
        {
            writer.Write(placement.BodyIndex);
            writer.Write(placement.CellIndex);
            writer.Write((uint)placement.Flags);
            WriteVector(writer, placement.Translation);
            WriteQuaternion(writer, placement.Rotation);
            WriteVector(writer, placement.Scale);
        }
    }

    private static void WriteHeightFields(BinaryWriter writer, MapCollision map)
    {
        writer.Write(map.HeightFields.Length);
        foreach (HeightFieldTile tile in map.HeightFields)
        {
            writer.Write(tile.CellIndex);
            WriteVector(writer, tile.Origin);
            writer.Write(tile.SpacingX);
            writer.Write(tile.SpacingY);
            writer.Write(tile.HeightScale);
            writer.Write(tile.CountX);
            writer.Write(tile.CountY);
            writer.Write(MemoryMarshal.AsBytes(tile.Heights.AsSpan()));
            writer.Write(MemoryMarshal.AsBytes(tile.CellFlags.AsSpan()));
        }
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        byte[] bytes = s_utf8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteArray<T>(BinaryWriter writer, ReadOnlySpan<T> values)
        where T : unmanaged
    {
        writer.Write(values.Length);
        writer.Write(MemoryMarshal.AsBytes(values));
    }

    private static void WriteVector(BinaryWriter writer, Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    private static void WriteVector(BinaryWriter writer, Vector3D value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    private static void WriteQuaternion(BinaryWriter writer, Quaternion value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
        writer.Write(value.W);
    }

    private static void WriteBounds(BinaryWriter writer, BoundingBox bounds)
    {
        WriteVector(writer, bounds.Minimum);
        WriteVector(writer, bounds.Maximum);
    }

    private static byte[] ReadExactly(BinaryReader reader, int count, string part)
    {
        byte[] bytes = reader.ReadBytes(count);
        return bytes.Length == count
            ? bytes
            : throw new MapCollisionFormatException(MapCollisionFileError.Truncated, $"Map-collision file ends early in its {part}.");
    }

    private static byte[] ReadSection(BinaryReader reader, ReadOnlySpan<byte> tag, string section)
    {
        byte[] header = ReadExactly(reader, tag.Length + sizeof(long), section + " section header");
        if (!header.AsSpan(0, tag.Length).SequenceEqual(tag))
        {
            throw Malformed(section + " section", "unexpected section tag");
        }

        long length = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(tag.Length));
        if (length < 0 || length > Array.MaxLength)
        {
            throw Malformed(section + " section", $"invalid length {length}");
        }

        Stream stream = reader.BaseStream;
        if (stream.CanSeek && length > stream.Length - stream.Position)
        {
            throw new MapCollisionFormatException(MapCollisionFileError.Truncated, $"Map-collision file ends early in its {section} section.");
        }

        return ReadExactly(reader, (int)length, section + " section");
    }

    private static ImmutableArray<MapCell> ReadCells(byte[] payload)
    {
        PayloadReader reader = new(payload, "cell");
        MapCell[] cells = new MapCell[reader.ReadCount(8)];
        for (int i = 0; i < cells.Length; i++)
        {
            string packageName = reader.ReadString();
            string[] layers = new string[reader.ReadCount(4)];
            for (int layer = 0; layer < layers.Length; layer++)
            {
                layers[layer] = reader.ReadString();
            }

            cells[i] = new MapCell(packageName, ImmutableCollectionsMarshal.AsImmutableArray(layers));
        }

        reader.ExpectEnd();
        return ImmutableCollectionsMarshal.AsImmutableArray(cells);
    }

    private static ImmutableArray<CollisionBody> ReadBodies(byte[] payload)
    {
        PayloadReader reader = new(payload, "body");
        CollisionBody[] bodies = new CollisionBody[reader.ReadCount(24)];
        for (int i = 0; i < bodies.Length; i++)
        {
            string name = reader.ReadString();
            CollisionBox[] boxes = new CollisionBox[reader.ReadCount(40)];
            for (int box = 0; box < boxes.Length; box++)
            {
                boxes[box] = new CollisionBox(reader.ReadVector(), reader.ReadQuaternion(), reader.ReadVector());
            }

            CollisionSphere[] spheres = new CollisionSphere[reader.ReadCount(16)];
            for (int sphere = 0; sphere < spheres.Length; sphere++)
            {
                spheres[sphere] = new CollisionSphere(reader.ReadVector(), reader.ReadSingle());
            }

            CollisionCapsule[] capsules = new CollisionCapsule[reader.ReadCount(36)];
            for (int capsule = 0; capsule < capsules.Length; capsule++)
            {
                capsules[capsule] = new CollisionCapsule(reader.ReadVector(), reader.ReadQuaternion(), reader.ReadSingle(), reader.ReadSingle());
            }

            ConvexHull[] hulls = new ConvexHull[reader.ReadCount(36)];
            for (int hull = 0; hull < hulls.Length; hull++)
            {
                Vector3[] vertices = reader.ReadArray<Vector3>();
                HullPolygon[] polygons = new HullPolygon[reader.ReadCount(24)];
                for (int polygon = 0; polygon < polygons.Length; polygon++)
                {
                    polygons[polygon] = new HullPolygon(new Plane(reader.ReadVector(), reader.ReadSingle()), reader.ReadInt32(), reader.ReadInt32());
                }

                byte[] indices = reader.ReadArray<byte>();
                hulls[hull] = new ConvexHull(
                    ImmutableCollectionsMarshal.AsImmutableArray(vertices),
                    ImmutableCollectionsMarshal.AsImmutableArray(polygons),
                    ImmutableCollectionsMarshal.AsImmutableArray(indices),
                    reader.ReadBounds());
            }

            TriangleMesh[] meshes = new TriangleMesh[reader.ReadCount(32)];
            for (int mesh = 0; mesh < meshes.Length; mesh++)
            {
                Vector3[] vertices = reader.ReadArray<Vector3>();
                int[] indices = reader.ReadArray<int>();
                meshes[mesh] = new TriangleMesh(
                    ImmutableCollectionsMarshal.AsImmutableArray(vertices),
                    ImmutableCollectionsMarshal.AsImmutableArray(indices),
                    [],
                    reader.ReadBounds());
            }

            CollisionBody body = new(
                name,
                ImmutableCollectionsMarshal.AsImmutableArray(boxes),
                ImmutableCollectionsMarshal.AsImmutableArray(spheres),
                ImmutableCollectionsMarshal.AsImmutableArray(capsules),
                ImmutableCollectionsMarshal.AsImmutableArray(hulls),
                ImmutableCollectionsMarshal.AsImmutableArray(meshes));
            string? error = MapCollisionRules.FindBodyError(body);
            if (error is not null)
            {
                throw Malformed("body section", $"body {i} '{name}' {error}");
            }

            bodies[i] = body;
        }

        reader.ExpectEnd();
        return ImmutableCollectionsMarshal.AsImmutableArray(bodies);
    }

    private static ImmutableArray<BodyPlacement> ReadPlacements(byte[] payload, ImmutableArray<CollisionBody> bodies, int cellCount)
    {
        PayloadReader reader = new(payload, "placement");
        BodyPlacement[] placements = new BodyPlacement[reader.ReadCount(64)];
        for (int i = 0; i < placements.Length; i++)
        {
            int bodyIndex = reader.ReadInt32();
            int cellIndex = reader.ReadInt32();
            PlacementFlags flags = (PlacementFlags)reader.ReadUInt32();
            Vector3D translation = reader.ReadVector3D();
            Quaternion rotation = reader.ReadQuaternion();
            Vector3 scale = reader.ReadVector();
            BodyPlacement placement = new(bodyIndex, cellIndex, translation, rotation, scale, flags, default);
            string? error = MapCollisionRules.FindPlacementError(placement, bodies.Length, cellCount);
            if (error is not null)
            {
                throw Malformed("placement section", $"placement {i} {error}");
            }

            placements[i] = placement with { Bounds = MapCollisionRules.PlacedBounds(bodies[bodyIndex], rotation, scale) };
        }

        reader.ExpectEnd();
        return ImmutableCollectionsMarshal.AsImmutableArray(placements);
    }

    private static ImmutableArray<HeightFieldTile> ReadHeightFields(byte[] payload, int cellCount)
    {
        PayloadReader reader = new(payload, "height-field");
        HeightFieldTile[] tiles = new HeightFieldTile[reader.ReadCount(52)];
        for (int i = 0; i < tiles.Length; i++)
        {
            int cellIndex = reader.ReadInt32();
            Vector3D origin = reader.ReadVector3D();
            float spacingX = reader.ReadSingle();
            float spacingY = reader.ReadSingle();
            float heightScale = reader.ReadSingle();
            int countX = reader.ReadInt32();
            int countY = reader.ReadInt32();
            if (countX < 2 || countY < 2 || (long)countX * countY > reader.Remaining / sizeof(short))
            {
                throw Malformed("height-field section", $"tile {i} has an invalid size {countX} × {countY}");
            }

            short[] heights = reader.ReadArray<short>(countX * countY);
            HeightFieldCellFlags[] flags = reader.ReadArray<HeightFieldCellFlags>((countX - 1) * (countY - 1));
            HeightFieldTile tile = new(
                cellIndex,
                origin,
                spacingX,
                spacingY,
                heightScale,
                countX,
                countY,
                ImmutableCollectionsMarshal.AsImmutableArray(heights),
                ImmutableCollectionsMarshal.AsImmutableArray(flags));
            string? error = MapCollisionRules.FindTileError(tile, cellCount);
            if (error is not null)
            {
                throw Malformed("height-field section", $"tile {i} {error}");
            }

            tiles[i] = tile;
        }

        reader.ExpectEnd();
        return ImmutableCollectionsMarshal.AsImmutableArray(tiles);
    }

    private static string Decode(ReadOnlySpan<byte> bytes, string part)
    {
        try
        {
            return s_utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw Malformed(part, "invalid UTF-8 text");
        }
    }

    private static MapCollisionFormatException Malformed(string part, string problem) =>
        new(MapCollisionFileError.Malformed, $"Map-collision file is malformed in its {part}: {problem}.");

    /// <summary>Reads one section payload; running past its end is a malformed file.</summary>
    private ref struct PayloadReader(ReadOnlySpan<byte> data, string section)
    {
        private readonly ReadOnlySpan<byte> _data = data;
        private int _at;

        public readonly int Remaining => _data.Length - _at;

        public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(Take(sizeof(int)));

        public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(sizeof(uint)));

        public float ReadSingle() => BinaryPrimitives.ReadSingleLittleEndian(Take(sizeof(float)));

        public Vector3 ReadVector() => new(ReadSingle(), ReadSingle(), ReadSingle());

        public Quaternion ReadQuaternion() => new(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        public Vector3D ReadVector3D() =>
            new(
                BinaryPrimitives.ReadDoubleLittleEndian(Take(sizeof(double))),
                BinaryPrimitives.ReadDoubleLittleEndian(Take(sizeof(double))),
                BinaryPrimitives.ReadDoubleLittleEndian(Take(sizeof(double))));

        public BoundingBox ReadBounds() => new(ReadVector(), ReadVector());

        public string ReadString()
        {
            int length = ReadInt32();
            if (length <= 0 || length > Remaining)
            {
                throw Malformed(section + " section", $"invalid string length {length}");
            }

            return Decode(Take(length), section + " section");
        }

        /// <summary>Reads a count of elements that each take at least <paramref name="minimumSize"/> bytes.</summary>
        public int ReadCount(int minimumSize)
        {
            int count = ReadInt32();
            if (count < 0 || (long)count * minimumSize > Remaining)
            {
                throw Malformed(section + " section", $"invalid count {count}");
            }

            return count;
        }

        /// <summary>Reads a count followed by that many elements.</summary>
        public T[] ReadArray<T>()
            where T : unmanaged =>
            ReadArray<T>(ReadCount(Unsafe.SizeOf<T>()));

        public T[] ReadArray<T>(int count)
            where T : unmanaged =>
            [.. MemoryMarshal.Cast<byte, T>(Take(count * Unsafe.SizeOf<T>()))];

        public readonly void ExpectEnd()
        {
            if (_at != _data.Length)
            {
                throw Malformed(section + " section", $"{_data.Length - _at} bytes left over");
            }
        }

        private ReadOnlySpan<byte> Take(int count)
        {
            if (count < 0 || count > _data.Length - _at)
            {
                throw Malformed(section + " section", "data ends early");
            }

            ReadOnlySpan<byte> bytes = _data.Slice(_at, count);
            _at += count;
            return bytes;
        }
    }
}
