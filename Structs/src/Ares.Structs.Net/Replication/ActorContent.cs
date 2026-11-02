using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Session;

namespace Ares.Structs.Net.Replication;

/// <summary>Replicated content for the actor of the channel.</summary>
/// <param name="Payload">Content bits.</param>
/// <param name="PayloadBits">Number of content bits.</param>
/// <param name="HasRepLayout">Whether the content starts with property layout data.</param>
public sealed record ActorContentBlock(byte[] Payload, long PayloadBits, bool HasRepLayout)
{
    /// <summary>Writes the block.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteBit(HasRepLayout);
        writer.WriteBit(true);
        writer.WritePacked(checked((uint)PayloadBits));
        writer.WriteBits(Payload, PayloadBits);
    }

    /// <summary>Reads a block for the channel's actor.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The block.</returns>
    public static ActorContentBlock Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        bool layout = reader.ReadBit();
        return reader.ReadBit() ? ReadPayload(reader, layout) : throw new InvalidPacketException("Expected content for the channel's actor");
    }

    internal static ActorContentBlock ReadPayload(BitReader reader, bool layout)
    {
        uint size = reader.ReadPacked();
        return new ActorContentBlock(reader.ReadBitArray(size), size, layout);
    }

    internal void WritePayload(BitWriter writer)
    {
        writer.WritePacked(checked((uint)PayloadBits));
        writer.WriteBits(Payload, PayloadBits);
    }
}

/// <summary>A field (property or function) payload addressed by its index in the class's field list.</summary>
/// <param name="Index">Field index.</param>
/// <param name="Payload">Field bits.</param>
/// <param name="PayloadBits">Number of field bits.</param>
public sealed record ActorFieldPayload(int Index, byte[] Payload, long PayloadBits)
{
    /// <summary>Writes the field.</summary>
    /// <param name="writer">Destination.</param>
    /// <param name="fieldCount">Number of fields of the class (1 to 65535).</param>
    public void Write(BitWriter writer, int fieldCount)
    {
        ArgumentNullException.ThrowIfNull(writer);
        CheckFieldCount(fieldCount);
        if (Index < 0 || Index >= fieldCount)
        {
            throw new InvalidPacketException("Field index out of range");
        }

        writer.WriteBounded((uint)Index, (ulong)fieldCount + 1);
        writer.WritePacked(checked((uint)PayloadBits));
        writer.WriteBits(Payload, PayloadBits);
    }

    /// <summary>Reads a field.</summary>
    /// <param name="reader">Source.</param>
    /// <param name="fieldCount">Number of fields of the class (1 to 65535).</param>
    /// <returns>The field.</returns>
    public static ActorFieldPayload Read(BitReader reader, int fieldCount)
    {
        ArgumentNullException.ThrowIfNull(reader);
        CheckFieldCount(fieldCount);
        int index = (int)reader.ReadBounded((ulong)fieldCount + 1);
        if (index == fieldCount)
        {
            throw new InvalidPacketException("Reserved field index");
        }

        uint size = reader.ReadPacked();
        return new ActorFieldPayload(index, reader.ReadBitArray(size), size);
    }

    private static void CheckFieldCount(int fieldCount)
    {
        if (fieldCount is < 1 or > 65535)
        {
            throw new InvalidPacketException("Invalid field count");
        }
    }
}

/// <summary>Static ids the client must resolve before it processes the actor's bunch.</summary>
/// <param name="Guids">1 to 256 distinct static ids.</param>
public sealed record MustMapGuids(IReadOnlyList<ulong> Guids)
{
    /// <summary>Largest number of ids.</summary>
    public const int MaxGuids = 256;

    /// <summary>Writes the ids.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Validate();
        writer.WriteBits((uint)Guids.Count, 16);
        foreach (ulong guid in Guids)
        {
            NetGuid.Write(writer, guid);
        }
    }

    /// <summary>Reads ids.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The ids.</returns>
    public static MustMapGuids Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        int count = (int)reader.ReadBits(16);
        if (count is < 1 or > MaxGuids)
        {
            throw new InvalidPacketException("Invalid must-map id count");
        }

        ulong[] guids = new ulong[count];
        for (int index = 0; index < count; index++)
        {
            guids[index] = NetGuid.Read(reader);
        }

        MustMapGuids result = new(guids);
        result.Validate();
        return result;
    }

    private void Validate()
    {
        if (Guids.Count is < 1 or > MaxGuids || Guids.Distinct().Count() != Guids.Count || Guids.Any(guid => guid < 3 || !NetGuid.IsStatic(guid)))
        {
            throw new InvalidPacketException("Invalid must-map ids");
        }
    }
}

/// <summary>Content for an existing, stably named default subobject (component) of the channel's actor.</summary>
/// <param name="Guid">Subobject id (dynamic).</param>
/// <param name="Content">The content.</param>
public sealed record StableSubobjectContent(ulong Guid, ActorContentBlock Content)
{
    /// <summary>Writes the block (server to client).</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Validate();
        writer.WriteBit(Content.HasRepLayout);
        writer.WriteBit(false);
        NetGuid.Write(writer, Guid);
        writer.WriteBit(true);
        Content.WritePayload(writer);
    }

    /// <summary>Reads a server block.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The block.</returns>
    public static StableSubobjectContent Read(BitReader reader)
    {
        (bool layout, ulong guid) = ReadHeader(reader);
        if (!reader.ReadBit())
        {
            throw new InvalidPacketException("Expected a stably named subobject");
        }

        StableSubobjectContent result = new(guid, ActorContentBlock.ReadPayload(reader, layout));
        result.Validate();
        return result;
    }

    /// <summary>Reads a client block (clients omit the stable-name field).</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The block.</returns>
    public static StableSubobjectContent ReadFromClient(BitReader reader)
    {
        (bool layout, ulong guid) = ReadHeader(reader);
        StableSubobjectContent result = new(guid, ActorContentBlock.ReadPayload(reader, layout));
        result.Validate();
        return result;
    }

    internal static (bool Layout, ulong Guid) ReadHeader(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        bool layout = reader.ReadBit();
        return reader.ReadBit()
            ? throw new InvalidPacketException("Expected subobject content, not actor content")
            : (layout, NetGuid.Read(reader));
    }

    private void Validate()
    {
        if (Guid < 2 || NetGuid.IsStatic(Guid))
        {
            throw new InvalidPacketException("Invalid stable subobject id");
        }
    }
}

/// <summary>Content for a stably named component of a map actor (both are loaded from the map, so their ids are static).</summary>
/// <param name="Guid">Component id (static).</param>
/// <param name="Content">The content.</param>
public sealed record StaticSubobjectContent(ulong Guid, ActorContentBlock Content)
{
    /// <summary>Writes the block (server to client).</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (Guid < 3 || !NetGuid.IsStatic(Guid))
        {
            throw new InvalidPacketException("Invalid map component id");
        }

        writer.WriteBit(Content.HasRepLayout);
        writer.WriteBit(false);
        NetGuid.Write(writer, Guid);
        writer.WriteBit(true);
        Content.WritePayload(writer);
    }
}

/// <summary>
/// A client block for a subobject the server never exported: it names the subobject by path inside a
/// known outer. The subobject stays unmapped.
/// </summary>
/// <param name="Outer">Outer id.</param>
/// <param name="Name">Subobject name.</param>
/// <param name="Content">The content.</param>
public sealed record ClientSubobjectReference(ulong Outer, string Name, ActorContentBlock Content)
{
    private const ulong PathExport = 1;

    /// <summary>Reads the block.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The reference.</returns>
    public static ClientSubobjectReference Read(BitReader reader)
    {
        (bool layout, ulong guid) = StableSubobjectContent.ReadHeader(reader);
        if (guid != PathExport)
        {
            throw new InvalidPacketException("Expected a path reference");
        }

        byte flags = reader.ReadByte();
        if ((flags & 1) == 0)
        {
            throw new InvalidPacketException("Path reference without a path");
        }

        ulong outer = NetGuid.Read(reader);
        string name = reader.ReadString(256);
        if ((flags & 4) != 0)
        {
            reader.ReadBits(32);
        }

        return new ClientSubobjectReference(outer, name, ActorContentBlock.ReadPayload(reader, layout));
    }
}

/// <summary>Construction of a server-created subobject of a class, inside the channel's actor or another outer.</summary>
/// <param name="Guid">Subobject id (dynamic).</param>
/// <param name="ClassGuid">Static id of the subobject's class.</param>
/// <param name="Content">The content.</param>
/// <param name="OuterGuid">Outer id; null when the channel's actor is the outer.</param>
public sealed record DynamicSubobjectContent(ulong Guid, ulong ClassGuid, ActorContentBlock Content, ulong? OuterGuid)
{
    /// <summary>Writes the block.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Validate();
        writer.WriteBit(Content.HasRepLayout);
        writer.WriteBit(false);
        NetGuid.Write(writer, Guid);
        writer.WriteBit(false);
        writer.WriteBit(false);
        NetGuid.Write(writer, ClassGuid);
        writer.WriteBit(OuterGuid is null);
        if (OuterGuid is { } outer)
        {
            NetGuid.Write(writer, outer);
        }

        Content.WritePayload(writer);
    }

    /// <summary>Reads a block.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The block.</returns>
    public static DynamicSubobjectContent Read(BitReader reader)
    {
        (bool layout, ulong guid) = StableSubobjectContent.ReadHeader(reader);
        if (reader.ReadBit() || reader.ReadBit())
        {
            throw new InvalidPacketException("Expected a new subobject, not a stable or deleted one");
        }

        ulong classGuid = NetGuid.Read(reader);
        ulong? outer = reader.ReadBit() ? null : NetGuid.Read(reader);
        DynamicSubobjectContent result = new(guid, classGuid, ActorContentBlock.ReadPayload(reader, layout), outer);
        result.Validate();
        return result;
    }

    private void Validate()
    {
        if (Guid < 2 || NetGuid.IsStatic(Guid) || ClassGuid < 3 || !NetGuid.IsStatic(ClassGuid) || OuterGuid is < 2 || OuterGuid == Guid)
        {
            throw new InvalidPacketException("Invalid subobject construction");
        }
    }
}

/// <summary>The opening bunch of a spawned actor's channel.</summary>
/// <param name="Spawn">The spawn.</param>
/// <param name="Controller">Player controller fields, for player controllers.</param>
/// <param name="Content">Initial content blocks (at most 256).</param>
public sealed record ActorInitialization(ActorSpawn Spawn, PlayerControllerOpen? Controller, IReadOnlyList<ActorContentBlock> Content)
{
    /// <summary>
    /// Builds the reliable opening bunch. The archetype and <paramref name="requiredGuids"/> (already exported)
    /// must be resolved by the client before it spawns the actor.
    /// </summary>
    /// <param name="channel">Channel index (1 to 32767).</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="requiredGuids">Other static ids the actor needs.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ToBunch(int channel, int sequence, IReadOnlyList<ulong> requiredGuids)
    {
        ArgumentNullException.ThrowIfNull(requiredGuids);
        if (channel is < 1 or >= ChannelBunch.ChannelCount || Content.Count > MustMapGuids.MaxGuids)
        {
            throw new InvalidPacketException("Invalid actor initialization");
        }

        BitWriter writer = new();
        new MustMapGuids([Spawn.Archetype, .. requiredGuids.Where(guid => guid != Spawn.Archetype)]).Write(writer);
        Spawn.Write(writer);
        Controller?.Write(writer);
        foreach (ActorContentBlock block in Content)
        {
            block.Write(writer);
        }

        ChannelBunch bunch = new(channel, writer.ToArray(), writer.Length)
        {
            Name = ChannelName.Actor,
            Sequence = sequence,
            Opened = true,
            MustBeMapped = true,
        };
        bunch.Validate();
        return bunch;
    }
}

/// <summary>The opening bunch of a channel for an actor that already exists in the map.</summary>
/// <param name="Actor">The map actor.</param>
/// <param name="Content">Initial content blocks (1 to 256).</param>
public sealed record StaticActorInitialization(StaticActorReference Actor, IReadOnlyList<ActorContentBlock> Content)
{
    /// <summary>Content for the actor's named components, after the actor's own content.</summary>
    public IReadOnlyList<StaticSubobjectContent> Components { get; init; } = [];

    /// <summary>Builds the reliable opening bunch.</summary>
    /// <param name="channel">Channel index (1 to 32767).</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ToBunch(int channel, int sequence)
    {
        if (channel is < 1 or >= ChannelBunch.ChannelCount || Content.Count is < 1 or > MustMapGuids.MaxGuids)
        {
            throw new InvalidPacketException("Invalid static actor initialization");
        }

        BitWriter writer = new();
        Actor.Write(writer);
        foreach (ActorContentBlock block in Content)
        {
            block.Write(writer);
        }

        foreach (StaticSubobjectContent component in Components)
        {
            component.Write(writer);
        }

        ChannelBunch bunch = new(channel, writer.ToArray(), writer.Length) { Name = ChannelName.Actor, Sequence = sequence, Opened = true };
        bunch.Validate();
        return bunch;
    }
}
