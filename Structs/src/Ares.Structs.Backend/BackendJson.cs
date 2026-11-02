using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ares.Structs.Backend;

/// <summary>
/// JSON settings for the game backend API. Member names are always given explicitly with
/// <see cref="JsonPropertyNameAttribute"/>, because the backend mixes snake_case and camelCase.
/// </summary>
public static class BackendJson
{
    /// <summary>Options for reading requests and writing responses.</summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.Strict,
    };
}

/// <summary>An empty JSON object, <c>{}</c>.</summary>
public sealed record EmptyResponse
{
    /// <summary>The shared instance.</summary>
    public static EmptyResponse Instance { get; } = new();
}

/// <summary>Error body, <c>{"error": "..."}</c>.</summary>
/// <param name="Error">Machine-readable reason.</param>
public sealed record ErrorResponse([property: JsonPropertyName("error")] string Error);
