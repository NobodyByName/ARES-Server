using Ares.GameDataExtract.Archives;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Properties;

/// <summary>
/// Reads the members a cooked Blueprint class declares from its class export. The export stores the class object's own
/// properties, then its super struct (a package index), its children (a count and package indices) and its child
/// properties: a count, then for each one its type name, name, object flags, array dimension, element size, property
/// flags, replication index, replication notify function and replication condition, followed by what its type adds
/// (referenced classes, structs and enums, and the inner properties of containers and enums).
/// </summary>
internal static class BlueprintClassReader
{
    /// <summary>Most bytes before the super struct that are searched for it.</summary>
    private const int MaxPrefix = 1024;

    private const int MaxChildren = 4096;
    private const int MaxDepth = 8;
    private const int BoolDetailSize = 6;

    /// <summary>
    /// The members <paramref name="classExport"/> declares, in chain order, typed with <paramref name="resolveStruct"/>
    /// (a struct by the reference a member stores; <see langword="null"/> when unknown, so a stored value of it cannot be read).
    /// </summary>
    /// <exception cref="InvalidDataException">The export holds no readable member list after its super struct.</exception>
    public static IReadOnlyList<PropertyField> DeclaredMembers(CookedPackage package, ZenExport classExport, Func<int, StructSchema?> resolveStruct)
    {
        int super = SerializedReference(package, classExport.Super);
        int start = CookedPackage.StartOf(classExport);
        int end = CookedPackage.EndOf(classExport);
        for (int at = start; at <= Math.Min(end - (3 * sizeof(int)), start + MaxPrefix); at++)
        {
            if (package.ReadInt32(at) == super && TryRead(package, at + sizeof(int), end, resolveStruct) is { } members)
            {
                return members;
            }
        }

        throw new InvalidDataException($"{package.Header.ExportPath(classExport.Index)}: no member list follows the super class");
    }

    /// <summary>The package index a property stores for <paramref name="index"/>: export + 1, or −(import + 1).</summary>
    private static int SerializedReference(CookedPackage package, PackageObjectIndex index)
    {
        if (index.Kind == PackageObjectKind.Export)
        {
            return index.ExportIndex + 1;
        }

        for (int i = 0; i < package.Header.Imports.Count; i++)
        {
            if (package.Header.Imports[i] == index)
            {
                return -(i + 1);
            }
        }

        throw new InvalidDataException($"{package.Name}: the class's super struct is not imported");
    }

    private static List<PropertyField>? TryRead(CookedPackage package, int at, int end, Func<int, StructSchema?> resolveStruct)
    {
        try
        {
            ByteReader reader = new(package.Data.AsSpan(0, end), at);
            int children = reader.ReadCount(MaxChildren);
            for (int i = 0; i < children; i++)
            {
                if (!IsReference(package, reader.ReadInt32()))
                {
                    return null;
                }
            }

            int count = reader.ReadCount(MaxChildren);
            List<PropertyField> members = new(count);
            for (int i = 0; i < count; i++)
            {
                members.Add(ReadField(package, ref reader, resolveStruct, 0));
            }

            return members;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static PropertyField ReadField(CookedPackage package, ref ByteReader reader, Func<int, StructSchema?> resolveStruct, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new InvalidDataException("Property types nest too deeply");
        }

        string typeName = ReadName(package, ref reader);
        string name = ReadName(package, ref reader);
        reader.ReadUInt32();
        int arrayDim = reader.ReadInt32();
        int elementSize = reader.ReadInt32();
        reader.ReadUInt64();
        reader.Skip(sizeof(ushort));
        ReadName(package, ref reader);
        reader.ReadByte();
        if (arrayDim is < 1 or > MaxChildren || elementSize < 0)
        {
            throw new InvalidDataException($"Member {name} has an invalid size");
        }

        PropertyType type = typeName switch
        {
            "BoolProperty" => SkipThen(ref reader, BoolDetailSize, PropertyType.Bool),
            "ByteProperty" => Reference(package, ref reader, PropertyType.Byte),
            "Int8Property" => PropertyType.Int8,
            "Int16Property" => PropertyType.Int16,
            "UInt16Property" => PropertyType.UInt16,
            "IntProperty" => PropertyType.Int32,
            "UInt32Property" => PropertyType.UInt32,
            "Int64Property" => PropertyType.Int64,
            "UInt64Property" => PropertyType.UInt64,
            "FloatProperty" => PropertyType.Float,
            "DoubleProperty" => PropertyType.Double,
            "NameProperty" => PropertyType.Name,
            "StrProperty" => PropertyType.String,
            "TextProperty" => PropertyType.Text,
            "ObjectProperty" or "WeakObjectProperty" or "InterfaceProperty" => Reference(package, ref reader, PropertyType.Object),
            "ClassProperty" => Reference(package, ref reader, Reference(package, ref reader, PropertyType.Object)),
            "SoftObjectProperty" => Reference(package, ref reader, PropertyType.SoftObject),
            "SoftClassProperty" => Reference(package, ref reader, Reference(package, ref reader, PropertyType.SoftObject)),
            "StructProperty" => StructType(package, ref reader, resolveStruct),
            "EnumProperty" => EnumType(package, ref reader, resolveStruct, depth),
            "ArrayProperty" => PropertyType.ArrayOf(ReadField(package, ref reader, resolveStruct, depth + 1).Type),
            "SetProperty" => PropertyType.SetOf(ReadField(package, ref reader, resolveStruct, depth + 1).Type),
            "MapProperty" => MapType(package, ref reader, resolveStruct, depth),
            "DelegateProperty" => Reference(package, ref reader, PropertyType.Delegate),
            "MulticastInlineDelegateProperty" => Reference(package, ref reader, PropertyType.MulticastDelegate),
            _ => throw new InvalidDataException($"Member {name} has the unsupported type {typeName}"),
        };
        return new PropertyField(name, type, arrayDim);
    }

    private static PropertyType StructType(CookedPackage package, ref ByteReader reader, Func<int, StructSchema?> resolveStruct)
    {
        int reference = reader.ReadInt32();
        if (!IsReference(package, reference) || reference == 0)
        {
            throw new InvalidDataException("A struct member refers to no struct");
        }

        return PropertyType.StructOf(resolveStruct(reference) ?? StructSchema.UnknownStruct($"struct {reference}"));
    }

    private static PropertyType EnumType(CookedPackage package, ref ByteReader reader, Func<int, StructSchema?> resolveStruct, int depth)
    {
        Reference(package, ref reader, PropertyType.Byte);
        return PropertyType.EnumOf(ReadField(package, ref reader, resolveStruct, depth + 1).Type);
    }

    private static PropertyType MapType(CookedPackage package, ref ByteReader reader, Func<int, StructSchema?> resolveStruct, int depth)
    {
        PropertyType key = ReadField(package, ref reader, resolveStruct, depth + 1).Type;
        return PropertyType.MapOf(key, ReadField(package, ref reader, resolveStruct, depth + 1).Type);
    }

    /// <summary>Reads a stored class, struct, enum or function reference (checked, not kept) and returns <paramref name="type"/>.</summary>
    private static PropertyType Reference(CookedPackage package, ref ByteReader reader, PropertyType type)
    {
        return IsReference(package, reader.ReadInt32()) ? type : throw new InvalidDataException("A member type refers outside the package");
    }

    private static PropertyType SkipThen(ref ByteReader reader, int count, PropertyType type)
    {
        reader.Skip(count);
        return type;
    }

    private static bool IsReference(CookedPackage package, int value) => value == 0 || package.IsImportReference(value) || (value > 0 && value <= package.Exports.Count);

    private static string ReadName(CookedPackage package, ref ByteReader reader)
    {
        int at = reader.Position;
        reader.Skip(2 * sizeof(uint));
        return package.IsName(at) ? package.ReadName(at) : throw new InvalidDataException($"No name at offset {at}");
    }
}
