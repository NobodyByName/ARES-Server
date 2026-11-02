using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ares.Common.Logging;

/// <summary>
/// Writes one JSON object per line. ARES services log structured events this way so
/// operators and tools can parse their output. Never pass secrets, tokens or request bodies.
/// </summary>
public sealed class JsonEventLog
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly TextWriter _output;
    private readonly Lock _lock = new();

    /// <summary>Creates a log writing to <paramref name="output"/>.</summary>
    public JsonEventLog(TextWriter output)
    {
        _output = output;
    }

    /// <summary>A log writing to standard output.</summary>
    public static JsonEventLog StandardOutput { get; } = new(Console.Out);

    /// <summary>Writes <paramref name="value"/> as one JSON line.</summary>
    public void Write(JsonObject value)
    {
        string line = value.ToJsonString(s_options);
        lock (_lock)
        {
            _output.WriteLine(line);
            _output.Flush();
        }
    }

    /// <summary>Writes a single-property event, e.g. <c>{"listening": "127.0.0.1:18080"}</c>.</summary>
    public void Write(string name, JsonNode? value)
    {
        Write(new JsonObject { [name] = value });
    }
}
