using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Ares.Structs.Backend.Telemetry;

namespace Ares.Host.Services.Telemetry;

/// <summary>
/// Turns UI telemetry into log rows. Only values matching known UI tag, level and error-code
/// formats are kept; arbitrary payload strings are never logged.
/// </summary>
public static partial class UiTelemetryLog
{
    /// <summary>Loggable rows for the events of a telemetry batch.</summary>
    public static IReadOnlyList<JsonObject> Project(ReadOnlyMemory<byte> body)
    {
        List<JsonObject> rows = [];
        foreach (UiTelemetryEvent item in UiTelemetry.ReadEvents(body))
        {
            JsonObject row = new() { ["event"] = item.EventId };
            switch (item)
            {
                case UiNavigationEvent navigation:
                    AddIf(row, "navigation", navigation.Navigation, Tag());
                    AddIf(row, "previous_navigation", navigation.PreviousNavigation, Tag());
                    AddIf(row, "level", navigation.Level, Level());
                    break;
                case UiViewEvent view:
                    if (view.View is not null && Tag().IsMatch(view.View))
                    {
                        row[view.View.StartsWith("UI.Modal.", StringComparison.Ordinal) ? "modal" : "error_view"] = view.View;
                    }

                    AddIf(row, "navigation", view.Navigation, Tag());
                    break;
                case UiErrorEvent error:
                    AddIf(row, "error_code", error.ErrorCode, ErrorCode());
                    break;
            }

            if (row.Count > 1)
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private static void AddIf(JsonObject row, string name, string? value, Regex format)
    {
        if (value is not null && format.IsMatch(value))
        {
            row[name] = value;
        }
    }

    [GeneratedRegex(@"\AUI\.(?:Nav|Error|Modal)\.[A-Za-z0-9_.-]{1,180}\z")]
    private static partial Regex Tag();

    [GeneratedRegex(@"\AAR[A-Z]{2}[0-9]{4}\z")]
    private static partial Regex ErrorCode();

    [GeneratedRegex(@"\A[A-Za-z][A-Za-z0-9_]{0,79}\z")]
    private static partial Regex Level();
}
