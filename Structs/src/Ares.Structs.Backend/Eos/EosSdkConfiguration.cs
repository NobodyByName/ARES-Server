using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Eos;

/// <summary>
/// Remote EOS SDK configuration (<c>GET /sdk/v1/default</c> and the product route): client
/// tuning sections and service base URLs. See <see cref="EosSdkDefaults"/> for the values.
/// </summary>
public sealed record EosSdkConfiguration(
    [property: JsonPropertyName("client")] IReadOnlyDictionary<string, IReadOnlyDictionary<string, EosConfigValue>> Client,
    [property: JsonPropertyName("services")] IReadOnlyDictionary<string, EosService> Services,
    [property: JsonPropertyName("watermark")] int Watermark);

/// <summary>An EOS service's endpoints.</summary>
public sealed record EosService(
    [property: JsonPropertyName("BaseUrl")] string BaseUrl,
    [property: JsonPropertyName("RedirectUrl"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RedirectUrl = null);

/// <summary>
/// One value of an EOS client configuration section: an integer, a real number (always written
/// with a decimal point), a boolean, a string, or a list of strings or integers.
/// </summary>
[JsonConverter(typeof(EosConfigValueConverter))]
public abstract record EosConfigValue
{
    public static implicit operator EosConfigValue(long value) => new EosInteger(value);

    public static implicit operator EosConfigValue(double value) => new EosReal(value);

    public static implicit operator EosConfigValue(bool value) => new EosBoolean(value);

    public static implicit operator EosConfigValue(string value) => new EosText(value);

    public static implicit operator EosConfigValue(string[] value) => new EosTextList(value);

    public static implicit operator EosConfigValue(long[] value) => new EosIntegerList(value);
}

public sealed record EosInteger(long Value) : EosConfigValue;

public sealed record EosReal(double Value) : EosConfigValue;

public sealed record EosBoolean(bool Value) : EosConfigValue;

public sealed record EosText(string Value) : EosConfigValue;

public sealed record EosTextList(IReadOnlyList<string> Value) : EosConfigValue;

public sealed record EosIntegerList(IReadOnlyList<long> Value) : EosConfigValue;

/// <summary>Writes <see cref="EosConfigValue"/> as its plain JSON value.</summary>
public sealed class EosConfigValueConverter : JsonConverter<EosConfigValue>
{
    /// <inheritdoc/>
    public override EosConfigValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("EOS configuration is only written by the server");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, EosConfigValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case EosInteger integer:
                writer.WriteNumberValue(integer.Value);
                break;
            case EosReal real:
                string text = real.Value.ToString("R", CultureInfo.InvariantCulture);
                writer.WriteRawValue(text.Contains('.', StringComparison.Ordinal) || text.Contains('E', StringComparison.Ordinal) ? text : text + ".0");
                break;
            case EosBoolean boolean:
                writer.WriteBooleanValue(boolean.Value);
                break;
            case EosText str:
                writer.WriteStringValue(str.Value);
                break;
            case EosTextList list:
                writer.WriteStartArray();
                foreach (string item in list.Value)
                {
                    writer.WriteStringValue(item);
                }

                writer.WriteEndArray();
                break;
            case EosIntegerList list:
                writer.WriteStartArray();
                foreach (long item in list.Value)
                {
                    writer.WriteNumberValue(item);
                }

                writer.WriteEndArray();
                break;
            default:
                throw new JsonException("Unknown EOS configuration value");
        }
    }
}
