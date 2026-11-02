namespace Ares.Structs.Backend.Telemetry;

/// <summary>A protobuf field: number, wire type and either a varint or the raw bytes.</summary>
public readonly record struct ProtobufField(int Number, int WireType, ulong Varint, ReadOnlyMemory<byte> Bytes);

/// <summary>Bounded reader for protobuf wire format (varint, 64-bit, length-delimited, 32-bit).</summary>
public static class ProtobufReader
{
    /// <summary>Most fields read from one message.</summary>
    public const int MaximumFields = 4096;

    /// <summary>Reads all top-level fields of a message.</summary>
    /// <exception cref="InvalidDataException">Malformed or unsupported encoding.</exception>
    public static IReadOnlyList<ProtobufField> ReadFields(ReadOnlyMemory<byte> data)
    {
        List<ProtobufField> fields = [];
        int at = 0;
        while (at < data.Length)
        {
            ulong tag = ReadVarint(data.Span, ref at);
            int number = checked((int)(tag >> 3));
            int wire = (int)(tag & 7);
            if (number == 0 || fields.Count >= MaximumFields)
            {
                throw new InvalidDataException("Invalid field count");
            }

            switch (wire)
            {
                case 0:
                    fields.Add(new ProtobufField(number, wire, ReadVarint(data.Span, ref at), ReadOnlyMemory<byte>.Empty));
                    break;
                case 1:
                case 5:
                    int width = wire == 1 ? 8 : 4;
                    fields.Add(new ProtobufField(number, wire, 0, Take(data, ref at, width)));
                    break;
                case 2:
                    ulong size = ReadVarint(data.Span, ref at);
                    fields.Add(new ProtobufField(number, wire, 0, Take(data, ref at, size > int.MaxValue ? -1 : (int)size)));
                    break;
                default:
                    throw new InvalidDataException("Unsupported field encoding");
            }
        }

        return fields;
    }

    private static ReadOnlyMemory<byte> Take(ReadOnlyMemory<byte> data, ref int at, int size)
    {
        if (size < 0 || size > data.Length - at)
        {
            throw new InvalidDataException("Truncated field");
        }

        ReadOnlyMemory<byte> result = data.Slice(at, size);
        at += size;
        return result;
    }

    private static ulong ReadVarint(ReadOnlySpan<byte> data, ref int at)
    {
        ulong value = 0;
        for (int shift = 0; shift < 70; shift += 7)
        {
            if (at >= data.Length)
            {
                throw new InvalidDataException("Truncated integer");
            }

            byte b = data[at++];
            value |= (ulong)(b & 0x7f) << shift;
            if (b < 0x80)
            {
                return value;
            }
        }

        throw new InvalidDataException("Oversized integer");
    }
}
