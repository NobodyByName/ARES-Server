using System.Text;

namespace Ares.Structs.Backend.Telemetry;

/// <summary>
/// UI events inside <c>/client2pubsub.Client2PubSub/TransferBatched</c> telemetry batches.
/// Each batch message holds records (field 1); a record's field number is the event id.
/// Event payload strings are returned as sent; callers decide what is safe to log.
/// </summary>
public static class UiTelemetry
{
    /// <summary>Request path of the telemetry batch upload.</summary>
    public const string TransferBatchedPath = "/client2pubsub.Client2PubSub/TransferBatched";

    /// <summary>Event id of a menu navigation.</summary>
    public const int NavigationEventId = 1051;

    /// <summary>Event id of a modal or error view being shown.</summary>
    public const int ViewEventId = 1052;

    /// <summary>Event id of an error code being reported.</summary>
    public const int ErrorEventId = 1105;

    /// <summary>
    /// Decodes the UI events of a gRPC-web batch body. Decoding stops at the first malformed
    /// frame or field; events decoded before it are returned.
    /// </summary>
    public static IReadOnlyList<UiTelemetryEvent> ReadEvents(ReadOnlyMemory<byte> body)
    {
        List<UiTelemetryEvent> events = [];
        try
        {
            foreach (ReadOnlyMemory<byte> message in GrpcWeb.ReadMessages(body))
            {
                foreach (ProtobufField record in ProtobufReader.ReadFields(message))
                {
                    if (record.Number != 1 || record.WireType != 2)
                    {
                        continue;
                    }

                    foreach (ProtobufField item in ProtobufReader.ReadFields(record.Bytes))
                    {
                        if (item is { WireType: 2, Number: NavigationEventId or ViewEventId or ErrorEventId })
                        {
                            events.Add(ReadEvent(item.Number, item.Bytes));
                        }
                    }
                }
            }
        }
        catch (InvalidDataException)
        {
            // Return what decoded cleanly.
        }

        return events;
    }

    private static UiTelemetryEvent ReadEvent(int eventId, ReadOnlyMemory<byte> payload)
    {
        string? field2 = null;
        string? field3 = null;
        string? field4 = null;
        foreach (ProtobufField field in ProtobufReader.ReadFields(payload))
        {
            if (field.WireType != 2)
            {
                continue;
            }

            string text = Encoding.UTF8.GetString(field.Bytes.Span);
            switch (field.Number)
            {
                case 2:
                    field2 = text;
                    break;
                case 3:
                    field3 = text;
                    break;
                case 4:
                    field4 = text;
                    break;
            }
        }

        return eventId switch
        {
            NavigationEventId => new UiNavigationEvent(field2, field3, field4),
            ViewEventId => new UiViewEvent(field2, field3),
            _ => new UiErrorEvent(field2, field3, field4),
        };
    }
}

/// <summary>A decoded UI telemetry event.</summary>
public abstract record UiTelemetryEvent(int EventId);

/// <summary>Event 1051: the menu navigated to <paramref name="Navigation"/> (a <c>UI.Nav.*</c> tag).</summary>
public sealed record UiNavigationEvent(string? Navigation, string? PreviousNavigation, string? Level)
    : UiTelemetryEvent(UiTelemetry.NavigationEventId);

/// <summary>Event 1052: a modal (<c>UI.Modal.*</c>) or error view (<c>UI.Error.*</c>) was shown.</summary>
public sealed record UiViewEvent(string? View, string? Navigation)
    : UiTelemetryEvent(UiTelemetry.ViewEventId);

/// <summary>Event 1105: an error code such as <c>ARSQ0003</c> was reported (field 3).</summary>
public sealed record UiErrorEvent(string? UnknownField2, string? ErrorCode, string? UnknownField4)
    : UiTelemetryEvent(UiTelemetry.ErrorEventId);
