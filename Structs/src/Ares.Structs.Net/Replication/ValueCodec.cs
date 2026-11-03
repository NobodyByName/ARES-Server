using System.Text.RegularExpressions;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.GameplayTags;

namespace Ares.Structs.Net.Replication;

/// <summary>
/// Property values shared by property layouts and network function parameters. Values are typed by
/// property kind: <see cref="bool"/>; <see cref="float"/>, <see cref="double"/>, <see cref="int"/>,
/// <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="sbyte"/>, <see cref="short"/>, <see cref="ushort"/>
/// for numbers; <see cref="ulong"/> for bytes, enums, objects and classes (object and class ids must be
/// mapped); <see cref="string"/>; <see cref="NetworkName"/>; <see cref="Vector3D"/>;
/// <see cref="GameplayTag"/>; <see cref="UniqueNetId"/>; <see cref="LevelVisibility"/>;
/// <see cref="VisibilityTransaction"/>; <see cref="PackedMovementBits"/>; and lists of level visibilities.
/// </summary>
public sealed partial class ValueCodec
{
    /// <summary>Level visibility report struct.</summary>
    public const string LevelVisibilityStruct = "/Script/Engine.UpdateLevelVisibilityLevelInfo";

    /// <summary>Level visibility transaction struct.</summary>
    public const string VisibilityTransactionStruct = "/Script/Engine.NetLevelVisibilityTransactionId";

    /// <summary>Vector struct.</summary>
    public const string VectorStruct = "/Script/CoreUObject.Vector";

    /// <summary>Gameplay tag struct.</summary>
    public const string GameplayTagStruct = "/Script/GameplayTags.GameplayTag";

    /// <summary>Player unique id struct.</summary>
    public const string UniqueNetIdStruct = "/Script/Engine.UniqueNetIdRepl";

    /// <summary>Vector quantized to hundredths.</summary>
    public const string QuantizedVector100Struct = "/Script/Engine.Vector_NetQuantize100";

    /// <summary>Character movement RPC bit containers.</summary>
    public static IReadOnlySet<string> MovementStructs { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "/Script/Engine.CharacterServerMovePackedBits", "/Script/Engine.CharacterMoveResponsePackedBits" };

    private const string ObjectClass = "/Script/CoreUObject.Object";
    private const int MaxLevelReports = 128;
    private const int MaxTagLength = 1024;

    private readonly Dictionary<string, ulong> _enumMaxima = new(StringComparer.Ordinal);
    private readonly Dictionary<ulong, string> _mappedObjects;
    private readonly Dictionary<ulong, string> _mappedClasses;

    /// <summary>Creates a codec.</summary>
    /// <param name="catalog">Replication schemas.</param>
    /// <param name="enums">Enums whose wire width is known.</param>
    /// <param name="mappedObjects">Object ids the client knows, with their classes.</param>
    /// <param name="mappedClasses">Class ids (static) the client knows, with the class they name.</param>
    public ValueCodec(
        ReplicationCatalog catalog,
        IEnumerable<EnumDefinition> enums,
        IReadOnlyDictionary<ulong, string>? mappedObjects = null,
        IReadOnlyDictionary<ulong, string>? mappedClasses = null)
    {
        ArgumentNullException.ThrowIfNull(enums);
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        foreach (EnumDefinition definition in enums)
        {
            _enumMaxima[definition.Path] = (ulong)definition.MaxValue;
        }

        _mappedObjects = new Dictionary<ulong, string>(mappedObjects ?? new Dictionary<ulong, string>());
        _mappedClasses = new Dictionary<ulong, string>(mappedClasses ?? new Dictionary<ulong, string>());
        if (_mappedObjects.Keys.Intersect(_mappedClasses.Keys).Any())
        {
            throw new InvalidPacketException("Class and object ids cannot overlap");
        }

        foreach ((ulong guid, string path) in _mappedClasses)
        {
            if (guid < 3 || !NetGuid.IsStatic(guid) || !catalog.IsA(path, ObjectClass))
            {
                throw new InvalidPacketException("Invalid mapped class");
            }
        }

        foreach ((ulong guid, string path) in _mappedObjects)
        {
            if (guid < 2 || !catalog.Classes.ContainsKey(path))
            {
                throw new InvalidPacketException("Invalid mapped object");
            }
        }
    }

    /// <summary>The replication schemas.</summary>
    public ReplicationCatalog Catalog { get; }

    /// <summary>Writes a value of a property.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="property">The property.</param>
    /// <param name="value">The value, typed as described on the class.</param>
    public void Write(BitWriter writer, PropertyDefinition property, object? value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(property);
        switch (property.Kind)
        {
            case PropertyKind.StructProperty:
                WriteStruct(writer, property, value);
                return;
            case PropertyKind.ArrayProperty when IsLevelReportArray(property):
                IReadOnlyList<object?> reports = value as IReadOnlyList<object?> ?? throw Invalid();
                if (reports.Count > MaxLevelReports)
                {
                    throw new InvalidPacketException("Too many level visibility reports");
                }

                writer.WriteBits((uint)reports.Count, 16);
                foreach (object? report in reports)
                {
                    Write(writer, Catalog.Property(property.Inner!), report);
                }

                return;
            case PropertyKind.BoolProperty:
                writer.WriteBit(value as bool? ?? throw Invalid());
                return;
            case PropertyKind.ObjectProperty:
                ulong objectGuid = value as ulong? ?? throw Invalid();
                CheckObject(objectGuid, property.TypePath!);
                NetGuid.Write(writer, objectGuid);
                return;
            case PropertyKind.ClassProperty:
                ulong classGuid = value as ulong? ?? throw Invalid();
                CheckClass(classGuid, property);
                NetGuid.Write(writer, classGuid);
                return;
            case PropertyKind.ByteProperty or PropertyKind.EnumProperty:
                (int width, ulong maximum) = EnumRange(property);
                ulong number = value as ulong? ?? throw Invalid();
                writer.WriteBits(number <= maximum ? number : throw Invalid(), width);
                return;
            case PropertyKind.StrProperty:
                writer.WriteString(value as string ?? throw Invalid());
                return;
            case PropertyKind.NameProperty:
                (value as NetworkName ?? throw Invalid()).Write(writer);
                return;
            default:
                WriteNumber(writer, property.Kind, value);
                return;
        }
    }

    /// <summary>Reads a value of a property.</summary>
    /// <param name="reader">Source.</param>
    /// <param name="property">The property.</param>
    /// <returns>The value, typed as described on the class.</returns>
    public object Read(BitReader reader, PropertyDefinition property)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(property);
        switch (property.Kind)
        {
            case PropertyKind.StructProperty:
                return ReadStruct(reader, property);
            case PropertyKind.ArrayProperty when IsLevelReportArray(property):
                int count = (int)reader.ReadBits(16);
                if (count > MaxLevelReports)
                {
                    throw new InvalidPacketException("Too many level visibility reports");
                }

                PropertyDefinition inner = Catalog.Property(property.Inner!);
                object[] reports = new object[count];
                for (int index = 0; index < count; index++)
                {
                    reports[index] = Read(reader, inner);
                }

                return reports;
            case PropertyKind.BoolProperty:
                return reader.ReadBit();
            case PropertyKind.ObjectProperty:
                ulong guid = NetGuid.Read(reader);
                CheckObject(guid, property.TypePath!);
                return guid;
            case PropertyKind.ClassProperty:
                ulong classGuid = NetGuid.Read(reader);
                CheckClass(classGuid, property);
                return classGuid;
            case PropertyKind.ByteProperty or PropertyKind.EnumProperty:
                (int width, ulong maximum) = EnumRange(property);
                ulong number = reader.ReadBits(width);
                return number <= maximum ? number : throw new InvalidPacketException("Enum value out of range");
            case PropertyKind.StrProperty:
                return reader.ReadString();
            case PropertyKind.NameProperty:
                return NetworkName.Read(reader);
            default:
                return ReadNumber(reader, property.Kind);
        }
    }

    private static void WriteStruct(BitWriter writer, PropertyDefinition property, object? value)
    {
        switch (property.TypePath)
        {
            case VisibilityTransactionStruct:
                (value as VisibilityTransaction ?? throw Invalid()).Write(writer);
                return;
            case LevelVisibilityStruct:
                (value as LevelVisibility ?? throw Invalid()).Write(writer);
                return;
            case VectorStruct:
                Vector3D vector = value as Vector3D? ?? throw Invalid();
                foreach (double component in (ReadOnlySpan<double>)[vector.X, vector.Y, vector.Z])
                {
                    writer.WriteBits(BitConverter.DoubleToUInt64Bits(double.IsFinite(component) ? component : throw Invalid()), 64);
                }

                return;
            case QuantizedVector100Struct:
                QuantizedVector.Write(writer, value as Vector3D? ?? throw Invalid(), QuantizedVector.Hundredths);
                return;
            case GameplayTagStruct:
                string tag = (value as GameplayTag ?? throw Invalid()).Name;
                CheckTag(tag.Equals("none", StringComparison.OrdinalIgnoreCase) ? "!" : tag);
                writer.WriteBits(0, 2);
                new NetworkName(tag.Length == 0 ? "None" : tag, 0).Write(writer);
                return;
            case UniqueNetIdStruct:
                (value as UniqueNetId ?? throw Invalid()).Write(writer);
                return;
            case { } path when MovementStructs.Contains(path):
                (value as PackedMovementBits ?? throw Invalid()).Write(writer);
                return;
            default:
                throw new InvalidPacketException("Struct needs a serializer that is not implemented");
        }
    }

    private static object ReadStruct(BitReader reader, PropertyDefinition property)
    {
        switch (property.TypePath)
        {
            case VisibilityTransactionStruct:
                return VisibilityTransaction.Read(reader);
            case LevelVisibilityStruct:
                return LevelVisibility.Read(reader);
            case VectorStruct:
                return new Vector3D(ReadDouble(reader), ReadDouble(reader), ReadDouble(reader));
            case QuantizedVector100Struct:
                return QuantizedVector.Read(reader, QuantizedVector.Hundredths);
            case GameplayTagStruct:
                if (reader.ReadBits(2) != 0)
                {
                    throw new InvalidPacketException("Compressed or dynamic gameplay tags are not supported");
                }

                NetworkName name = NetworkName.Read(reader);
                if (name.Number != 0)
                {
                    throw new InvalidPacketException("Numbered gameplay tag names are not supported");
                }

                string tag = name.Text.Equals("none", StringComparison.OrdinalIgnoreCase) ? "" : name.Text;
                CheckTag(tag);
                return new GameplayTag(tag);
            case UniqueNetIdStruct:
                return UniqueNetId.Read(reader);
            case { } path when MovementStructs.Contains(path):
                return PackedMovementBits.Read(reader);
            default:
                throw new InvalidPacketException("Struct needs a serializer that is not implemented");
        }
    }

    private static void WriteNumber(BitWriter writer, PropertyKind kind, object? value)
    {
        switch (kind, value)
        {
            case (PropertyKind.FloatProperty, float number) when float.IsFinite(number):
                writer.WriteBits(BitConverter.SingleToUInt32Bits(number), 32);
                return;
            case (PropertyKind.DoubleProperty, double number) when double.IsFinite(number):
                writer.WriteBits(BitConverter.DoubleToUInt64Bits(number), 64);
                return;
            case (PropertyKind.IntProperty, int number):
                writer.WriteBits((uint)number, 32);
                return;
            case (PropertyKind.UInt32Property, uint number):
                writer.WriteBits(number, 32);
                return;
            case (PropertyKind.Int64Property, long number):
                writer.WriteBits((ulong)number, 64);
                return;
            case (PropertyKind.UInt64Property, ulong number):
                writer.WriteBits(number, 64);
                return;
            case (PropertyKind.Int8Property, sbyte number):
                writer.WriteBits((byte)number, 8);
                return;
            case (PropertyKind.Int16Property, short number):
                writer.WriteBits((ushort)number, 16);
                return;
            case (PropertyKind.UInt16Property, ushort number):
                writer.WriteBits(number, 16);
                return;
            default:
                throw new InvalidPacketException("Value does not match its property kind, or the kind needs a serializer that is not implemented");
        }
    }

    private static object ReadNumber(BitReader reader, PropertyKind kind)
    {
        switch (kind)
        {
            case PropertyKind.FloatProperty:
                float single = BitConverter.UInt32BitsToSingle((uint)reader.ReadBits(32));
                return float.IsFinite(single) ? single : throw new InvalidPacketException("Non-finite number");
            case PropertyKind.DoubleProperty:
                return ReadDouble(reader);
            case PropertyKind.IntProperty:
                return (int)(uint)reader.ReadBits(32);
            case PropertyKind.UInt32Property:
                return (uint)reader.ReadBits(32);
            case PropertyKind.Int64Property:
                return (long)reader.ReadBits(64);
            case PropertyKind.UInt64Property:
                return reader.ReadBits(64);
            case PropertyKind.Int8Property:
                return (sbyte)(byte)reader.ReadBits(8);
            case PropertyKind.Int16Property:
                return (short)(ushort)reader.ReadBits(16);
            case PropertyKind.UInt16Property:
                return (ushort)reader.ReadBits(16);
            default:
                throw new InvalidPacketException("Property kind needs a serializer that is not implemented");
        }
    }

    private static double ReadDouble(BitReader reader)
    {
        double value = BitConverter.UInt64BitsToDouble(reader.ReadBits(64));
        return double.IsFinite(value) ? value : throw new InvalidPacketException("Non-finite number");
    }

    private bool IsLevelReportArray(PropertyDefinition property) =>
        property.Inner is { } inner && Catalog.Property(inner).TypePath == LevelVisibilityStruct;

    private (int Width, ulong Maximum) EnumRange(PropertyDefinition property)
    {
        if (property.TypePath is null && property.Kind == PropertyKind.ByteProperty)
        {
            return (8, byte.MaxValue);
        }

        if (property.TypePath is null || !_enumMaxima.TryGetValue(property.TypePath, out ulong maximum))
        {
            throw new InvalidPacketException("Enum width is not known");
        }

        int storage = property.Kind == PropertyKind.EnumProperty ? StorageBits(Catalog.Property(property.Underlying!).Kind) : 8;
        int width = Math.Min(64 - System.Numerics.BitOperations.LeadingZeroCount(maximum), storage);
        return (width, Math.Min(maximum, width == 64 ? ulong.MaxValue : (1UL << width) - 1));
    }

    private static int StorageBits(PropertyKind kind) => kind switch
    {
        PropertyKind.ByteProperty or PropertyKind.Int8Property => 8,
        PropertyKind.UInt16Property or PropertyKind.Int16Property => 16,
        PropertyKind.IntProperty or PropertyKind.UInt32Property => 32,
        _ => 64,
    };

    private void CheckObject(ulong guid, string expected)
    {
        if (guid == 1)
        {
            throw new InvalidPacketException("Invalid object id");
        }

        if (guid != 0 && (!_mappedObjects.TryGetValue(guid, out string? path) || !Catalog.IsA(path, expected)))
        {
            throw new InvalidPacketException("Unmapped object, or one of the wrong class");
        }
    }

    private void CheckClass(ulong guid, PropertyDefinition property)
    {
        if (guid == 1)
        {
            throw new InvalidPacketException("Invalid class id");
        }

        if (guid != 0 && (property.MetaClassPath is not { } expected || !_mappedClasses.TryGetValue(guid, out string? path) || !Catalog.IsA(path, expected)))
        {
            throw new InvalidPacketException("Unmapped class, or one of the wrong kind");
        }
    }

    private static void CheckTag(string tag)
    {
        if (tag.Length > MaxTagLength || (tag.Length != 0 && !TagPattern().IsMatch(tag)))
        {
            throw new InvalidPacketException("Invalid gameplay tag");
        }
    }

    private static InvalidPacketException Invalid() => new("Value does not match its property");

    [GeneratedRegex("^[A-Za-z_][A-Za-z_0-9]*(?:\\.[A-Za-z_][A-Za-z_0-9]*)*$")]
    private static partial Regex TagPattern();
}
