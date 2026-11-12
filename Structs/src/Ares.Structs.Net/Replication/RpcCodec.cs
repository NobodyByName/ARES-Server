using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.GameplayTags;

namespace Ares.Structs.Net.Replication;

/// <summary>
/// Network function calls: a field payload addressed by the class's field table, holding the parameters.
/// Each parameter except booleans has a presence bit; parameters at their default are not sent (structs
/// always are, unless given as null). A struct without its own serializer is sent as its members, given as a
/// dictionary by name.
/// </summary>
/// <param name="values">Value codec.</param>
public sealed class RpcCodec(ValueCodec values)
{
    private const int MaxFields = 128;

    // Default parameter values; each keeps its own boxed type (a numeric switch would widen them all).
    private static readonly Dictionary<PropertyKind, object> s_defaults = new()
    {
        [PropertyKind.FloatProperty] = 0f,
        [PropertyKind.DoubleProperty] = 0d,
        [PropertyKind.IntProperty] = 0,
        [PropertyKind.UInt32Property] = 0U,
        [PropertyKind.Int64Property] = 0L,
        [PropertyKind.UInt64Property] = 0UL,
        [PropertyKind.Int8Property] = (sbyte)0,
        [PropertyKind.Int16Property] = (short)0,
        [PropertyKind.UInt16Property] = (ushort)0,
        [PropertyKind.ByteProperty] = 0UL,
        [PropertyKind.EnumProperty] = 0UL,
        [PropertyKind.ObjectProperty] = 0UL,
        [PropertyKind.ClassProperty] = 0UL,
        [PropertyKind.StrProperty] = "",
        [PropertyKind.NameProperty] = NetworkName.None,
        [PropertyKind.ArrayProperty] = Array.Empty<object>(),
    };

    private readonly ValueCodec _values = values ?? throw new ArgumentNullException(nameof(values));
    private readonly Dictionary<string, NetFieldTable> _tables = new(StringComparer.Ordinal);

    /// <summary>The field table of a class.</summary>
    /// <param name="classPath">Class path.</param>
    /// <returns>The table.</returns>
    public NetFieldTable Fields(string classPath)
    {
        if (_tables.TryGetValue(classPath, out NetFieldTable? table))
        {
            return table;
        }

        table = new NetFieldTable(_values.Catalog, classPath);
        _tables[classPath] = table;
        return table;
    }

    /// <summary>Encodes a call as a content block.</summary>
    /// <param name="classPath">Class of the target object.</param>
    /// <param name="function">Function name.</param>
    /// <param name="parameters">Parameter values by name.</param>
    /// <param name="direction">Call direction.</param>
    /// <returns>The content block.</returns>
    public ActorContentBlock Call(string classPath, string function, IReadOnlyDictionary<string, object?> parameters, RpcDirection direction)
    {
        NetFieldTable table = Fields(classPath);
        NetField field = table.Field(function).RequireDirection(direction);
        BitWriter payload = new();
        WriteParameters(payload, field, parameters);
        BitWriter writer = new();
        new ActorFieldPayload(field.Index, payload.ToArray(), payload.Length).Write(writer, table.Count);
        return new ActorContentBlock(writer.ToArray(), writer.Length, false);
    }

    /// <summary>Decodes a content block holding exactly one call.</summary>
    /// <param name="classPath">Class of the target object.</param>
    /// <param name="block">The content block.</param>
    /// <param name="direction">Expected call direction.</param>
    /// <returns>The function name and parameter values by name.</returns>
    public (string Function, Dictionary<string, object?> Parameters) ReadCall(string classPath, ActorContentBlock block, RpcDirection direction)
    {
        IReadOnlyList<(NetField Field, ActorFieldPayload Payload)> calls = ReadFields(classPath, block, direction);
        return calls is [var call]
            ? (call.Field.Name, ReadParameters(call.Field, call.Payload))
            : throw new InvalidPacketException("Expected exactly one call");
    }

    /// <summary>Reads the field payloads of a block; callers decide how to handle each.</summary>
    /// <param name="classPath">Class of the target object.</param>
    /// <param name="block">The content block.</param>
    /// <param name="direction">Expected call direction.</param>
    /// <returns>Function fields and their payloads.</returns>
    public IReadOnlyList<(NetField Field, ActorFieldPayload Payload)> ReadFields(string classPath, ActorContentBlock block, RpcDirection direction)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (block.HasRepLayout)
        {
            throw new InvalidPacketException("Function calls cannot carry a property layout");
        }

        NetFieldTable table = Fields(classPath);
        BitReader reader = new(block.Payload, block.PayloadBits);
        List<(NetField, ActorFieldPayload)> result = [];
        while (reader.Remaining > 0)
        {
            if (result.Count >= MaxFields)
            {
                throw new InvalidPacketException("Too many calls in one block");
            }

            ActorFieldPayload payload = ActorFieldPayload.Read(reader, table.Count);
            result.Add((table.FieldAt(payload.Index).RequireDirection(direction), payload));
        }

        return result;
    }

    /// <summary>Decodes the parameters of a call.</summary>
    /// <param name="function">The function field.</param>
    /// <param name="payload">Its payload.</param>
    /// <returns>Parameter values by name.</returns>
    public Dictionary<string, object?> ReadParameters(NetField function, ActorFieldPayload payload)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(payload);
        FunctionSchema schema = function.Function ?? throw new InvalidPacketException("Not a function");
        BitReader reader = new(payload.Payload, payload.PayloadBits);
        Dictionary<string, object?> result = new(StringComparer.Ordinal);
        foreach (string parameter in schema.Parameters)
        {
            PropertyDefinition property = Parameter(parameter);
            result[property.Name] = property.Kind == PropertyKind.BoolProperty
                ? _values.Read(reader, property)
                : DefaultOf(property) is var fallback && reader.ReadBit() ? ReadValue(reader, property) : fallback;
        }

        return reader.Remaining == 0 ? result : throw new InvalidPacketException("Trailing parameter bits");
    }

    private void WriteParameters(BitWriter writer, NetField function, IReadOnlyDictionary<string, object?> parameters)
    {
        FunctionSchema schema = function.Function ?? throw new InvalidPacketException("Not a function");
        PropertyDefinition[] properties = [.. schema.Parameters.Select(Parameter)];
        if (properties.Length != parameters.Count || properties.Any(property => !parameters.ContainsKey(property.Name)))
        {
            throw new InvalidPacketException("Parameters do not match the function");
        }

        foreach (PropertyDefinition property in properties)
        {
            object? value = parameters[property.Name];
            switch (property.Kind)
            {
                case PropertyKind.TextProperty:
                    // Only the empty text is supported: it is sent as "not present".
                    writer.WriteBit(value is null or "" ? false : throw new InvalidPacketException("Non-empty text parameters are not supported"));
                    break;
                case PropertyKind.BoolProperty:
                    _values.Write(writer, property, value);
                    break;
                default:
                    if (property.Kind == PropertyKind.NameProperty && value is string text)
                    {
                        value = new NetworkName(text, 0);
                    }

                    // A struct given as null is not sent: the receiver uses the struct's defaults.
                    if (property.Kind == PropertyKind.StructProperty && value is null)
                    {
                        writer.WriteBit(false);
                        break;
                    }

                    BitWriter raw = new();
                    WriteValue(raw, property, value);
                    bool send = property.Kind == PropertyKind.StructProperty || !IsDefault(property, value);
                    writer.WriteBit(send);
                    if (send)
                    {
                        writer.WriteBits(raw.ToArray(), raw.Length);
                    }

                    break;
            }
        }
    }

    // A struct without its own serializer is sent as its members in order, without presence bits of their own.
    private static bool IsPlainStruct(PropertyDefinition property) =>
        property.Kind == PropertyKind.StructProperty && (property.StructFlags & StructFlags.NetSerializeNative) == 0;

    private IEnumerable<PropertyDefinition> Members(PropertyDefinition property) =>
        property.Children.Select(_values.Catalog.Property).Where(member => (member.Flags & PropertyFlags.RepSkip) == 0)
            .Select(member => member.ArrayDim == 1 ? member : throw new InvalidPacketException("Static array members are not supported"));

    private void WriteValue(BitWriter writer, PropertyDefinition property, object? value)
    {
        if (!IsPlainStruct(property))
        {
            _values.Write(writer, property, value);
            return;
        }

        IReadOnlyDictionary<string, object?> members = value as IReadOnlyDictionary<string, object?> ?? throw new InvalidPacketException("Struct parameters need their members");
        PropertyDefinition[] definitions = [.. Members(property)];
        if (property.Children.Count == 0)
        {
            throw new InvalidPacketException("The struct's members are not known; it can only be sent as not present");
        }

        if (definitions.Length != members.Count || definitions.Any(member => !members.ContainsKey(member.Name)))
        {
            throw new InvalidPacketException("Members do not match the struct");
        }

        foreach (PropertyDefinition member in definitions)
        {
            WriteValue(writer, member, members[member.Name]);
        }
    }

    private object ReadValue(BitReader reader, PropertyDefinition property) =>
        IsPlainStruct(property)
            ? Members(property).ToDictionary(member => member.Name, member => ReadValue(reader, member), StringComparer.Ordinal)
            : _values.Read(reader, property);

    private PropertyDefinition Parameter(string path)
    {
        PropertyDefinition property = _values.Catalog.Property(path);
        return property.ArrayDim == 1 ? property : throw new InvalidPacketException("Static array parameters are not supported");
    }

    private static bool IsDefault(PropertyDefinition property, object? value) => value switch
    {
        float number => number == 0,
        double number => number == 0,
        int number => number == 0,
        uint number => number == 0,
        long number => number == 0,
        ulong number => number == 0,
        sbyte number => number == 0,
        short number => number == 0,
        ushort number => number == 0,
        string text => text.Length == 0,
        NetworkName name => name == NetworkName.None,
        IReadOnlyList<object?> list => property.Kind == PropertyKind.ArrayProperty && list.Count == 0,
        _ => false,
    };

    private object DefaultOf(PropertyDefinition property) =>
        s_defaults.TryGetValue(property.Kind, out object? value)
            ? value
            : property.Kind == PropertyKind.BoolProperty
                ? false
            : IsPlainStruct(property)
                ? Members(property).ToDictionary(member => member.Name, DefaultOf, StringComparer.Ordinal)
            : property.Kind == PropertyKind.StructProperty
                ? StructDefault(property.TypePath)
                : throw new InvalidPacketException("Parameter needs a serializer that is not implemented");

    private static object StructDefault(string? typePath) => typePath switch
    {
        ValueCodec.GameplayTagStruct => new GameplayTag(""),
        ValueCodec.VectorStruct => default(Vector3D),
        ValueCodec.LevelVisibilityStruct => new LevelVisibility("None", "None", false, false, new VisibilityTransaction(0, false)),
        ValueCodec.VisibilityTransactionStruct => new VisibilityTransaction(0, false),
        not null when ValueCodec.MovementStructs.Contains(typePath) => PackedMovementBits.Empty,
        _ => throw new InvalidPacketException("Parameter needs a serializer that is not implemented"),
    };
}
