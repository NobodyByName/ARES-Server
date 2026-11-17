using System.Buffers.Binary;
using Ares.GameDataExtract.Archives;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Assets;
using Ares.Structs.Core.Geometry;

namespace Ares.GameDataExtract.Properties;

/// <summary>
/// Decodes unversioned property blocks of a cooked package against a <see cref="StructSchema"/>: the header (see
/// <see cref="UnversionedHeader"/>) lists the chain indices stored, then the values follow in chain order. Native
/// structs decode as <see cref="Vector3D"/> (vectors, float vectors too), <see cref="Rotator"/>,
/// <see cref="Quaternion"/>, <c>double[]</c> (2D vectors), <c>float[]</c> (linear colors), <c>uint[]</c> (GUIDs),
/// <see cref="SoftObjectPath"/> and <c>string[]</c> (gameplay tag containers).
/// </summary>
internal static class PropertyReader
{
    private const int MaxDepth = 32;
    private const int MaxElements = 1 << 20;
    private const int MaxTags = 4096;
    private const int GuidWords = 4;

    /// <summary>Reads the block at <paramref name="offset"/>; <paramref name="end"/> is the offset after its last value.</summary>
    /// <exception cref="InvalidDataException">The block is malformed, stores a value for an unknown member, or holds a value the schema cannot decode.</exception>
    public static PropertyBag Read(CookedPackage package, int offset, StructSchema schema, out int end)
    {
        ByteReader reader = new(package.Data, offset);
        PropertyBag bag = ReadBlock(package, ref reader, schema, 0, prefix: false, out _);
        end = reader.Position;
        return bag;
    }

    /// <summary>
    /// Reads the block at <paramref name="offset"/> up to the first stored value of an unknown member: the members
    /// before it, or all of them when every stored member is known (<paramref name="complete"/>).
    /// </summary>
    /// <exception cref="InvalidDataException">The header or a value before the unknown member is malformed.</exception>
    public static PropertyBag ReadPrefix(CookedPackage package, int offset, StructSchema schema, out bool complete)
    {
        ByteReader reader = new(package.Data, offset);
        return ReadBlock(package, ref reader, schema, 0, prefix: true, out complete);
    }

    /// <summary>Reads the properties of <paramref name="export"/>, which must end exactly <paramref name="trailer"/> bytes before the export does.</summary>
    /// <exception cref="InvalidDataException">The block cannot be read, or ends elsewhere.</exception>
    public static PropertyBag ReadExport(CookedPackage package, ZenExport export, StructSchema schema, int trailer)
    {
        PropertyBag bag = Read(package, CookedPackage.StartOf(export), schema, out int end);
        int left = CookedPackage.EndOf(export) - end;
        return left == trailer
            ? bag
            : throw new InvalidDataException($"{package.Header.ExportPath(export.Index)}: {left} bytes follow the {schema.Name} properties, expected {trailer}");
    }

    private static PropertyBag ReadBlock(CookedPackage package, ref ByteReader reader, StructSchema schema, int depth, bool prefix, out bool complete)
    {
        if (depth > MaxDepth)
        {
            throw new InvalidDataException($"{package.Name}: properties nest too deeply");
        }

        int start = reader.Position;
        UnversionedHeader header;
        try
        {
            header = UnversionedHeader.Read(package.Data, start);
        }
        catch (InvalidDataException error)
        {
            throw new InvalidDataException($"{package.Name}: {schema.Name} at offset {start}: {error.Message}", error);
        }

        reader.Skip(header.ValuesOffset - start);
        Dictionary<string, object> values = new(StringComparer.Ordinal);
        complete = true;
        foreach (UnversionedMember member in header.Members)
        {
            (PropertyField? field, int element) = schema.At(member.Index);
            if (field is null)
            {
                if (!member.IsNonZero)
                {
                    continue;
                }

                if (prefix)
                {
                    complete = false;
                    break;
                }

                string where = member.Index < schema.ChainLength ? "an unknown member" : $"a member past its chain of {schema.ChainLength}";
                throw new InvalidDataException($"{package.Name}: {schema.Name} at offset {start} stores {where} (index {member.Index})");
            }

            string key = field.ArrayDim == 1 ? field.Name : $"{field.Name}[{element}]";
            try
            {
                values[key] = member.IsNonZero ? ReadValue(package, ref reader, field.Type, depth) : Zero(field.Type);
            }
            catch (InvalidDataException error) when (depth == 0)
            {
                throw new InvalidDataException($"{package.Name}: {schema.Name}.{key} at offset {start}: {error.Message}", error);
            }
        }

        return new PropertyBag(schema, values);
    }

    private static object ReadValue(CookedPackage package, ref ByteReader reader, PropertyType type, int depth)
    {
        switch (type.Kind)
        {
            case PropertyKind.Bool:
                return reader.ReadByte() != 0;
            case PropertyKind.Byte:
                return reader.ReadByte();
            case PropertyKind.Int8:
                return (sbyte)reader.ReadByte();
            case PropertyKind.Int16:
                return BinaryPrimitives.ReadInt16LittleEndian(reader.ReadBytes(sizeof(short)));
            case PropertyKind.UInt16:
                return BinaryPrimitives.ReadUInt16LittleEndian(reader.ReadBytes(sizeof(ushort)));
            case PropertyKind.Int32:
                return reader.ReadInt32();
            case PropertyKind.UInt32:
                return reader.ReadUInt32();
            case PropertyKind.Int64:
                return (long)reader.ReadUInt64();
            case PropertyKind.UInt64:
                return reader.ReadUInt64();
            case PropertyKind.Float:
                return ReadFloat(ref reader);
            case PropertyKind.Double:
                return ReadDouble(ref reader);
            case PropertyKind.Name:
                return ReadName(package, ref reader);
            case PropertyKind.String:
                return reader.ReadString();
            case PropertyKind.Text:
                return ReadText(package, ref reader);
            case PropertyKind.Object:
                return new ObjectReference(reader.ReadInt32());
            case PropertyKind.SoftObject:
                return ReadSoftObjectPath(package, ref reader);
            case PropertyKind.Enum:
                return ReadValue(package, ref reader, Inner(type), depth + 1);
            case PropertyKind.Array:
                return ReadElements(package, ref reader, Inner(type), reader.ReadCount(MaxElements), depth);
            case PropertyKind.Set:
                ReadNoRemovals(ref reader);
                return ReadElements(package, ref reader, Inner(type), reader.ReadCount(MaxElements), depth);
            case PropertyKind.Map:
                ReadNoRemovals(ref reader);
                KeyValuePair<object, object>[] pairs = new KeyValuePair<object, object>[reader.ReadCount(MaxElements)];
                for (int i = 0; i < pairs.Length; i++)
                {
                    object key = ReadValue(package, ref reader, Inner(type), depth + 1);
                    pairs[i] = new KeyValuePair<object, object>(key, ReadValue(package, ref reader, type.Value ?? throw MissingPart(type), depth + 1));
                }

                return pairs;
            case PropertyKind.Struct:
                StructSchema schema = type.Struct ?? throw MissingPart(type);
                return schema.Native == NativeLayout.None
                    ? ReadBlock(package, ref reader, schema, depth + 1, prefix: false, out _)
                    : ReadNative(package, ref reader, schema);
            case PropertyKind.Delegate:
                return ReadDelegate(package, ref reader);
            case PropertyKind.MulticastDelegate:
                object[] delegates = new object[reader.ReadCount(MaxElements)];
                for (int i = 0; i < delegates.Length; i++)
                {
                    delegates[i] = ReadDelegate(package, ref reader);
                }

                return delegates;
            case PropertyKind.Opaque:
                return reader.ReadBytes(type.Size).ToArray();
            default:
                throw new InvalidDataException($"Property kind {type.Kind} is not supported");
        }
    }

    private static object[] ReadElements(CookedPackage package, ref ByteReader reader, PropertyType element, int count, int depth)
    {
        object[] elements = new object[count];
        for (int i = 0; i < count; i++)
        {
            elements[i] = ReadValue(package, ref reader, element, depth + 1);
        }

        return elements;
    }

    private static object ReadNative(CookedPackage package, ref ByteReader reader, StructSchema schema)
    {
        switch (schema.Native)
        {
            case NativeLayout.Vector:
                return new Vector3D(ReadDouble(ref reader), ReadDouble(ref reader), ReadDouble(ref reader));
            case NativeLayout.Vector3f:
                return new Vector3D(ReadFloat(ref reader), ReadFloat(ref reader), ReadFloat(ref reader));
            case NativeLayout.Vector2D:
                return new[] { ReadDouble(ref reader), ReadDouble(ref reader) };
            case NativeLayout.Rotator:
                return new Rotator(ReadDouble(ref reader), ReadDouble(ref reader), ReadDouble(ref reader));
            case NativeLayout.Quat:
                return new Quaternion(ReadDouble(ref reader), ReadDouble(ref reader), ReadDouble(ref reader), ReadDouble(ref reader));
            case NativeLayout.LinearColor:
                return new[] { ReadFloat(ref reader), ReadFloat(ref reader), ReadFloat(ref reader), ReadFloat(ref reader) };
            case NativeLayout.Guid:
                uint[] words = new uint[GuidWords];
                for (int i = 0; i < words.Length; i++)
                {
                    words[i] = reader.ReadUInt32();
                }

                return words;
            case NativeLayout.SoftObjectPath:
                return ReadSoftObjectPath(package, ref reader);
            case NativeLayout.GameplayTagContainer:
                string[] tags = new string[reader.ReadCount(MaxTags)];
                for (int i = 0; i < tags.Length; i++)
                {
                    tags[i] = ReadName(package, ref reader);
                }

                return tags;
            case NativeLayout.Unknown:
                throw new InvalidDataException($"Struct {schema.Name} has no known layout");
            case NativeLayout.None:
            default:
                throw new InvalidDataException($"Struct {schema.Name} has no native layout");
        }
    }

    /// <summary>The zero value of <paramref name="type"/>: what a member the block marks as zero holds.</summary>
    private static object Zero(PropertyType type)
    {
        return type.Kind switch
        {
            PropertyKind.Bool => false,
            PropertyKind.Byte => (byte)0,
            PropertyKind.Int8 => (sbyte)0,
            PropertyKind.Int16 => (short)0,
            PropertyKind.UInt16 => (ushort)0,
            PropertyKind.Int32 => 0,
            PropertyKind.UInt32 => 0u,
            PropertyKind.Int64 => 0L,
            PropertyKind.UInt64 => 0UL,
            PropertyKind.Float => 0f,
            PropertyKind.Double => 0d,
            PropertyKind.Name => CookedPackage.NoneName,
            PropertyKind.String => "",
            PropertyKind.Text => TextValue.Empty,
            PropertyKind.Object => new ObjectReference(0),
            PropertyKind.SoftObject => SoftObjectPath.Null,
            PropertyKind.Enum => Zero(Inner(type)),
            PropertyKind.Array or PropertyKind.Set or PropertyKind.MulticastDelegate => Array.Empty<object>(),
            PropertyKind.Map => Array.Empty<KeyValuePair<object, object>>(),
            PropertyKind.Struct => ZeroStruct(type.Struct ?? throw MissingPart(type)),
            PropertyKind.Delegate => new DelegateValue(new ObjectReference(0), CookedPackage.NoneName),
            PropertyKind.Opaque => new byte[type.Size],
            _ => throw new InvalidDataException($"Property kind {type.Kind} is not supported"),
        };
    }

    private static object ZeroStruct(StructSchema schema)
    {
        return schema.Native switch
        {
            NativeLayout.None => new PropertyBag(schema, new Dictionary<string, object>(StringComparer.Ordinal), isZero: true),
            NativeLayout.Vector or NativeLayout.Vector3f => default(Vector3D),
            NativeLayout.Vector2D => new double[2],
            NativeLayout.Rotator => default(Rotator),
            NativeLayout.Quat => default(Quaternion),
            NativeLayout.LinearColor => new float[4],
            NativeLayout.Guid => new uint[GuidWords],
            NativeLayout.SoftObjectPath => SoftObjectPath.Null,
            NativeLayout.GameplayTagContainer => Array.Empty<string>(),
            _ => throw new InvalidDataException($"Struct {schema.Name} has no known layout"),
        };
    }

    private static TextValue ReadText(CookedPackage package, ref ByteReader reader)
    {
        reader.ReadUInt32();
        sbyte history = (sbyte)reader.ReadByte();
        switch ((TextHistory)history)
        {
            case TextHistory.None:
                return reader.ReadUInt32() != 0 ? TextValue.Empty with { Source = reader.ReadString() } : TextValue.Empty;
            case TextHistory.Base:
                string space = reader.ReadString();
                string key = reader.ReadString();
                return new TextValue(TextHistory.Base, space, key, reader.ReadString());
            case TextHistory.StringTableEntry:
                string table = ReadName(package, ref reader);
                return new TextValue(TextHistory.StringTableEntry, table, reader.ReadString(), "");
            default:
                throw new InvalidDataException($"Text history {history} is not supported");
        }
    }

    private static SoftObjectPath ReadSoftObjectPath(CookedPackage package, ref ByteReader reader)
    {
        string packageName = ReadName(package, ref reader);
        string assetName = ReadName(package, ref reader);
        return new SoftObjectPath(packageName, assetName, reader.ReadString());
    }

    private static DelegateValue ReadDelegate(CookedPackage package, ref ByteReader reader)
    {
        ObjectReference target = new(reader.ReadInt32());
        return new DelegateValue(target, ReadName(package, ref reader));
    }

    private static string ReadName(CookedPackage package, ref ByteReader reader)
    {
        string name = package.ReadName(reader.Position);
        reader.Skip(2 * sizeof(uint));
        return name;
    }

    private static float ReadFloat(ref ByteReader reader) => BinaryPrimitives.ReadSingleLittleEndian(reader.ReadBytes(sizeof(float)));

    private static double ReadDouble(ref ByteReader reader) => BinaryPrimitives.ReadDoubleLittleEndian(reader.ReadBytes(sizeof(double)));

    private static void ReadNoRemovals(ref ByteReader reader)
    {
        int removed = reader.ReadInt32();
        if (removed != 0)
        {
            throw new InvalidDataException($"A container removes {removed} archetype elements, which is not supported");
        }
    }

    private static PropertyType Inner(PropertyType type) => type.Inner ?? throw MissingPart(type);

    private static InvalidDataException MissingPart(PropertyType type) => new($"A {type.Kind} property type lacks its element, value or struct type");
}
