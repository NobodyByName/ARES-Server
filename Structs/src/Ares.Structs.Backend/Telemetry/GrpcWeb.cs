using System.Buffers.Binary;

namespace Ares.Structs.Backend.Telemetry;

/// <summary>gRPC-web framing (<c>application/grpc-web+proto</c>) as used by the client's telemetry.</summary>
public static class GrpcWeb
{
    /// <summary>Content type of gRPC-web requests and responses.</summary>
    public const string ContentType = "application/grpc-web+proto";

    /// <summary>Flag of a trailer frame; message frames use 0.</summary>
    public const byte TrailerFlag = 0x80;

    /// <summary>A successful unary reply: an empty message frame followed by <c>grpc-status: 0</c>.</summary>
    public static byte[] EmptyOkResponse()
    {
        ReadOnlySpan<byte> trailer = "grpc-status: 0\r\n"u8;
        byte[] payload = new byte[5 + 5 + trailer.Length];
        payload[5] = TrailerFlag;
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(6), trailer.Length);
        trailer.CopyTo(payload.AsSpan(10));
        return payload;
    }

    /// <summary>The message frames of a gRPC-web body (trailer frames skipped).</summary>
    /// <exception cref="InvalidDataException">A frame is truncated.</exception>
    public static IReadOnlyList<ReadOnlyMemory<byte>> ReadMessages(ReadOnlyMemory<byte> body)
    {
        List<ReadOnlyMemory<byte>> messages = [];
        int at = 0;
        while (at < body.Length)
        {
            if (body.Length - at < 5)
            {
                throw new InvalidDataException("Truncated frame");
            }

            byte flag = body.Span[at];
            int size = BinaryPrimitives.ReadInt32BigEndian(body.Span[(at + 1)..]);
            at += 5;
            if (size < 0 || size > body.Length - at)
            {
                throw new InvalidDataException("Truncated frame body");
            }

            if (flag == 0)
            {
                messages.Add(body.Slice(at, size));
            }

            at += size;
        }

        return messages;
    }
}
