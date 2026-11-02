using System.Text.Json;
using Ares.Structs.Backend;

namespace Ares.Host.Services.Routing;

/// <summary>A received backend request: its body as JSON object, form fields, or neither.</summary>
public sealed class BackendRequest
{
    private readonly byte[] _body;

    /// <summary>Creates a request from its raw parts.</summary>
    public BackendRequest(string method, string path, string? authorization, byte[] body, IReadOnlyDictionary<string, string>? form)
    {
        Method = method;
        Path = path;
        Authorization = authorization;
        _body = body;
        Form = form;
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            Json = document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            Json = null;
        }
    }

    /// <summary>HTTP method.</summary>
    public string Method { get; }

    /// <summary>Request path without query.</summary>
    public string Path { get; }

    /// <summary>The Authorization header, if any.</summary>
    public string? Authorization { get; }

    /// <summary>The body when it is a JSON object.</summary>
    public JsonElement? Json { get; }

    /// <summary>Form fields of a <c>application/x-www-form-urlencoded</c> body that is not JSON.</summary>
    private IReadOnlyDictionary<string, string>? Form { get; }

    /// <summary>
    /// The JSON body as <typeparamref name="T"/>. A missing, non-object or mistyped body reads as an
    /// empty object, so absent members stay null and the route decides how to reply.
    /// </summary>
    public T Read<T>()
        where T : class
    {
        if (Json is null)
        {
            return EmptyObject<T>();
        }

        try
        {
            return JsonSerializer.Deserialize<T>(_body, BackendJson.Options) ?? EmptyObject<T>();
        }
        catch (JsonException)
        {
            return EmptyObject<T>();
        }
    }

    private static T EmptyObject<T>()
        where T : class
    {
        return JsonSerializer.Deserialize<T>("{}"u8, BackendJson.Options)!;
    }

    /// <summary>A form field, or null.</summary>
    public string? FormValue(string name)
    {
        return Form is not null && Form.TryGetValue(name, out string? value) ? value : null;
    }
}
