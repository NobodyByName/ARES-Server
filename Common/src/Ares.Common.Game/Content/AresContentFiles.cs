using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Structs.Backend.Progression;

namespace Ares.Common.Game.Content;

/// <summary>The ARES content cannot be used: a file is missing, malformed or inconsistent; the message tells the operator why.</summary>
public sealed class AresContentException : Exception
{
    /// <summary>Creates the exception.</summary>
    public AresContentException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with the failure that caused it.</summary>
    public AresContentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary><c>stash.json</c>: the stash structure.</summary>
/// <param name="SlotAssetId">The stash slot, the root holding the owned container tier.</param>
/// <param name="FirstContainerAssetId">The container tier a new stash starts with; each tier's upgrade names the next one.</param>
/// <param name="Definitions">The stash slot, every container tier and their content slot, in serving order.</param>
internal sealed record StashDocument(
    [property: JsonPropertyName("stashSlot")] long SlotAssetId,
    [property: JsonPropertyName("firstContainer")] long FirstContainerAssetId,
    [property: JsonPropertyName("definitions")] IReadOnlyList<ItemDescription> Definitions);

/// <summary><c>loadout.json</c>: the loadout structure.</summary>
/// <param name="FrameSlotAssetId">The loadout root holding the selected frame.</param>
/// <param name="StandardFrameAssetId">The frame selected without an augment.</param>
/// <param name="Branches">The standard frame's branch for each equipment place.</param>
/// <param name="Definitions">The frame slot, the standard frame and its branches, containers and slots, in serving order.</param>
internal sealed record LoadoutDocument(
    [property: JsonPropertyName("frameSlot")] long FrameSlotAssetId,
    [property: JsonPropertyName("standardFrame")] long StandardFrameAssetId,
    [property: JsonPropertyName("branches")] LoadoutBranches Branches,
    [property: JsonPropertyName("definitions")] IReadOnlyList<ItemDescription> Definitions);

/// <summary>
/// The standard frame's branch asset id of each equipment place. Host keeps the melee branch empty; a raid puts the
/// Raider Tool in its slot.
/// </summary>
public sealed record LoadoutBranches(
    [property: JsonPropertyName("backpack")] long Backpack,
    [property: JsonPropertyName("quickUse")] long QuickUse,
    [property: JsonPropertyName("safePockets")] long SafePockets,
    [property: JsonPropertyName("mainWeapon")] long MainWeapon,
    [property: JsonPropertyName("secondaryWeapon")] long SecondaryWeapon,
    [property: JsonPropertyName("shield")] long Shield,
    [property: JsonPropertyName("melee")] long Melee)
{
    /// <summary>Every named branch.</summary>
    public IEnumerable<long> All => [Backpack, QuickUse, SafePockets, MainWeapon, SecondaryWeapon, Shield, Melee];
}

/// <summary>One row of <c>item-recycling.json</c>.</summary>
/// <param name="GameAssetId">The recycled item.</param>
/// <param name="Outputs">What recycling one item gives, in package order.</param>
internal sealed record RecyclingRow(
    [property: JsonPropertyName("gameAssetId")] long GameAssetId,
    [property: JsonPropertyName("outputs")] IReadOnlyList<ItemAssetAmount> Outputs);

/// <summary>Reads content files: strict JSON in the backend's wire format, unknown members refused.</summary>
internal static class AresContentReader
{
    private const string ResourcePrefix = "Ares.Common.Game.Content.Defaults.";

    private static readonly JsonSerializerOptions s_options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.Strict,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowOutOfOrderMetadataProperties = true,
    };

    /// <summary>Reads a content file from <paramref name="directory"/> when it has one, else the built-in default.</summary>
    /// <param name="directory">The operator's content folder, or null.</param>
    /// <param name="file">The content file name.</param>
    /// <param name="overridden">Whether the file came from <paramref name="directory"/>.</param>
    /// <exception cref="AresContentException">The file cannot be read or is not valid for <typeparamref name="T"/>.</exception>
    public static T Read<T>(string? directory, string file, out bool overridden)
    {
        string? path = directory is null ? null : Path.Combine(directory, file);
        overridden = path is not null && File.Exists(path);
        string source = overridden ? path! : $"the built-in {file}";
        try
        {
            using Stream stream = overridden ? File.OpenRead(path!)
                : typeof(AresContentReader).Assembly.GetManifestResourceStream(ResourcePrefix + file)
                    ?? throw new AresContentException($"The built-in content file {file} is missing from this ARES build.");
            return JsonSerializer.Deserialize<T>(stream, s_options) ?? throw new AresContentException($"{source} is empty (null).");
        }
        catch (JsonException error)
        {
            throw new AresContentException($"{source} is not valid content: {error.Message}", error);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new AresContentException($"Cannot read {source}: {error.Message}", error);
        }
    }

    /// <summary>The error for a file whose content contradicts itself.</summary>
    public static AresContentException Invalid(string? directory, string file, bool overridden, string problem) =>
        new($"{(overridden ? Path.Combine(directory!, file) : $"The built-in {file}")}: {problem}");
}
