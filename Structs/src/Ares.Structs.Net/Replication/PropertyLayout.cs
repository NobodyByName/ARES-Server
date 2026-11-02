using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>One atomic member of a class's property layout, addressed on the wire by its handle.</summary>
/// <param name="Handle">Handle (1-based).</param>
/// <param name="Path">Member path, e.g. <c>Name</c>, <c>Struct.Member</c>, <c>Array[2]</c> or <c>Array[]</c> for an element.</param>
/// <param name="Property">Property path.</param>
/// <param name="Parent">Index of the class replication record the member belongs to.</param>
/// <param name="CustomDelta">Whether the member is serialized by its own delta serializer.</param>
/// <param name="Children">Element member of a dynamic array.</param>
public sealed record PropertyCommand(int Handle, string Path, string Property, int Parent, bool CustomDelta, IReadOnlyList<PropertyCommand> Children);

/// <summary>
/// The property layout of a class: its replication records, with plain structs expanded into their
/// members and natively serialized structs kept whole.
/// </summary>
public sealed class PropertyLayout
{
    private const int MaxDepth = 32;
    private const int MaxHandles = 65535;

    private readonly Dictionary<string, PropertyCommand> _byPath = new(StringComparer.Ordinal);
    private readonly Dictionary<int, PropertyCommand> _byHandle = [];

    /// <summary>Builds the layout of <paramref name="classPath"/>.</summary>
    /// <param name="catalog">Replication schemas.</param>
    /// <param name="classPath">Class path.</param>
    public PropertyLayout(ReplicationCatalog catalog, string classPath)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        catalog.Class(classPath);
        List<PropertyCommand> commands = [];
        IReadOnlyList<ReplicationRecord> records = catalog.Records(classPath);
        for (int index = 0; index < records.Count; index++)
        {
            PropertyDefinition property = catalog.Property(records[index].Property);
            string path = property.ArrayDim > 1 ? $"{property.Name}[{records[index].StaticArrayIndex}]" : property.Name;
            Expand(commands, property, path, index, (property.StructFlags & StructFlags.NetDeltaSerializeNative) != 0, []);
        }

        if (commands.Count > MaxHandles)
        {
            throw new InvalidPacketException("Too many layout members");
        }

        foreach (PropertyCommand command in commands)
        {
            if (!_byPath.TryAdd(command.Path, command))
            {
                throw new InvalidPacketException("Duplicate layout member");
            }

            _byHandle[command.Handle] = command;
        }

        Commands = commands;
    }

    /// <summary>The replication schemas.</summary>
    public ReplicationCatalog Catalog { get; }

    /// <summary>Members in handle order.</summary>
    public IReadOnlyList<PropertyCommand> Commands { get; }

    /// <summary>The member at <paramref name="path"/>.</summary>
    /// <param name="path">Member path.</param>
    /// <returns>The member.</returns>
    public PropertyCommand Command(string path) =>
        _byPath.TryGetValue(path, out PropertyCommand? command) ? command : throw new InvalidPacketException("Not an atomic layout member");

    /// <summary>The member with <paramref name="handle"/>, if any.</summary>
    /// <param name="handle">Handle.</param>
    /// <returns>The member, or null.</returns>
    public PropertyCommand? CommandAt(int handle) => _byHandle.GetValueOrDefault(handle);

    private void Expand(List<PropertyCommand> target, PropertyDefinition property, string path, int parent, bool customDelta, HashSet<string> ancestry)
    {
        if (ancestry.Count >= MaxDepth || !ancestry.Add(property.Path))
        {
            throw new InvalidPacketException("Recursive property layout");
        }

        try
        {
            ExpandMembers(target, property, path, parent, customDelta, ancestry);
        }
        finally
        {
            ancestry.Remove(property.Path);
        }
    }

    private void ExpandMembers(List<PropertyCommand> target, PropertyDefinition property, string path, int parent, bool customDelta, HashSet<string> ancestry)
    {
        if (property.Kind == PropertyKind.StructProperty && (property.StructFlags & StructFlags.NetSerializeNative) == 0)
        {
            foreach (string childPath in property.Children)
            {
                PropertyDefinition child = Catalog.Property(childPath);
                if ((child.Flags & PropertyFlags.RepSkip) != 0)
                {
                    continue;
                }

                for (int index = 0; index < child.ArrayDim; index++)
                {
                    string suffix = child.ArrayDim > 1 ? $"[{index}]" : "";
                    Expand(target, child, $"{path}.{child.Name}{suffix}", parent, customDelta, ancestry);
                }
            }

            return;
        }

        List<PropertyCommand> children = [];
        if (property.Kind == PropertyKind.ArrayProperty)
        {
            Expand(children, Catalog.Property(property.Inner!), path + "[]", parent, customDelta, ancestry);
        }

        target.Add(new PropertyCommand(target.Count + 1, path, property.Path, parent, customDelta, children));
    }
}

/// <summary>
/// Property deltas: changed layout members by handle, terminated by handle 0. Dynamic arrays are sent
/// whole (up to 128 elements of names, 16-bit integers or objects).
/// </summary>
/// <param name="values">Value codec.</param>
public sealed class PropertyCodec(ValueCodec values)
{
    private const int MaxArrayElements = 128;
    private const string RoleProperty = "/Script/Engine.Actor.Role";
    private const string RemoteRoleProperty = "/Script/Engine.Actor.RemoteRole";

    private readonly ValueCodec _values = values ?? throw new ArgumentNullException(nameof(values));

    /// <summary>Encodes changed members as a content block with a property layout.</summary>
    /// <param name="layout">The class layout.</param>
    /// <param name="changes">Values by member path.</param>
    /// <returns>The content block.</returns>
    public ActorContentBlock Encode(PropertyLayout layout, IReadOnlyDictionary<string, object?> changes)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(changes);
        if (changes.Count == 0)
        {
            throw new InvalidPacketException("A property delta needs changes");
        }

        BitWriter writer = new();
        writer.WriteBit(false);
        foreach (PropertyCommand command in changes.Keys.Select(layout.Command).OrderBy(command => command.Handle))
        {
            writer.WritePacked((uint)command.Handle);
            WriteMember(writer, command, changes[command.Path]);
        }

        writer.WritePacked(0);
        return new ActorContentBlock(writer.ToArray(), writer.Length, true);
    }

    /// <summary>Decodes a property delta.</summary>
    /// <param name="layout">The class layout.</param>
    /// <param name="block">The content block.</param>
    /// <param name="swapActorRoles">
    /// Report the actor's role and remote role swapped, as seen by the receiver (the sender's role is the
    /// receiver's remote role).
    /// </param>
    /// <returns>Values by member path.</returns>
    public Dictionary<string, object?> Decode(PropertyLayout layout, ActorContentBlock block, bool swapActorRoles = true)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(block);
        if (!block.HasRepLayout)
        {
            throw new InvalidPacketException("Property content needs its layout flag");
        }

        BitReader reader = new(block.Payload, block.PayloadBits);
        if (reader.ReadBit())
        {
            throw new InvalidPacketException("Property checksum mode is not supported");
        }

        Dictionary<string, object?> result = new(StringComparer.Ordinal);
        int previous = 0;
        for (uint handle = reader.ReadPacked(); handle != 0; handle = reader.ReadPacked())
        {
            if (handle <= previous || layout.CommandAt((int)handle) is not { } command)
            {
                throw new InvalidPacketException("Unknown, repeated or unordered property handle");
            }

            previous = (int)handle;
            string path = (swapActorRoles, command.Property) switch
            {
                (true, RoleProperty) => "RemoteRole",
                (true, RemoteRoleProperty) => "Role",
                _ => command.Path,
            };
            result[path] = ReadMember(reader, command);
        }

        return reader.Remaining == 0 ? result : throw new InvalidPacketException("Trailing data after the property delta");
    }

    private void WriteMember(BitWriter writer, PropertyCommand command, object? value)
    {
        PropertyDefinition property = CheckDirect(command);
        if (property.Kind != PropertyKind.ArrayProperty)
        {
            _values.Write(writer, property, value);
            return;
        }

        IReadOnlyList<object?> items = value as IReadOnlyList<object?> ?? throw new InvalidPacketException("Array value needs a list");
        if (items.Count > MaxArrayElements)
        {
            throw new InvalidPacketException("Array too long");
        }

        writer.WriteBits((uint)items.Count, 16);
        if (items.Count != 0)
        {
            PropertyDefinition element = ArrayElement(command);
            for (int index = 0; index < items.Count; index++)
            {
                writer.WritePacked((uint)index + 1);
                _values.Write(writer, element, items[index]);
            }
        }

        writer.WritePacked(0);
    }

    private object ReadMember(BitReader reader, PropertyCommand command)
    {
        PropertyDefinition property = CheckDirect(command);
        if (property.Kind != PropertyKind.ArrayProperty)
        {
            return _values.Read(reader, property);
        }

        int count = (int)reader.ReadBits(16);
        if (count > MaxArrayElements)
        {
            throw new InvalidPacketException("Array too long");
        }

        object[] items = new object[count];
        if (count == 0)
        {
            return reader.ReadPacked() == 0 ? items : throw new InvalidPacketException("Trailing array element handle");
        }

        PropertyDefinition element = ArrayElement(command);
        for (int index = 0; index < count; index++)
        {
            if (reader.ReadPacked() != index + 1)
            {
                throw new InvalidPacketException("Sparse array updates need the previous array state");
            }

            items[index] = _values.Read(reader, element);
        }

        return reader.ReadPacked() == 0 ? items : throw new InvalidPacketException("Trailing array element handle");
    }

    private PropertyDefinition CheckDirect(PropertyCommand command) =>
        !command.CustomDelta
            ? _values.Catalog.Property(command.Property)
            : throw new InvalidPacketException("Custom delta members need their own serializer");

    private PropertyDefinition ArrayElement(PropertyCommand command)
    {
        if (command.Children is not [{ CustomDelta: false } child])
        {
            throw new InvalidPacketException("Only whole arrays of names, 16-bit integers or objects are supported");
        }

        PropertyDefinition element = _values.Catalog.Property(child.Property);
        return element.Kind is PropertyKind.NameProperty or PropertyKind.UInt16Property or PropertyKind.ObjectProperty
            ? element
            : throw new InvalidPacketException("Only whole arrays of names, 16-bit integers or objects are supported");
    }
}
