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

/// <summary><c>loot-rules.json</c>: the operator's loot rules, in order.</summary>
/// <param name="Rules">The rules; a later rule wins over an earlier one for each setting it sets.</param>
internal sealed record LootRulesDocument(
    [property: JsonPropertyName("rules")] IReadOnlyList<LootRuleDocument> Rules);

/// <summary>One loot rule: which containers it covers and what it sets for them.</summary>
/// <param name="Name">Name shown in logs; defaults to the rule's position.</param>
/// <param name="Tags">Container tags it covers, each with the tags below it.</param>
/// <param name="Containers">Ids of the containers it covers.</param>
/// <param name="SpawnChance">Chance from 0 to 1 that a covered container spawns.</param>
/// <param name="Loot">Contents of a covered container.</param>
internal sealed record LootRuleDocument(
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("tags")] IReadOnlyList<string?>? Tags = null,
    [property: JsonPropertyName("containers")] IReadOnlyList<string?>? Containers = null,
    [property: JsonPropertyName("spawnChance")] double? SpawnChance = null,
    [property: JsonPropertyName("loot")] LootListDocument? Loot = null);

/// <summary>The contents a loot rule gives.</summary>
/// <param name="Items">Items to draw from.</param>
/// <param name="Stacks">How many stacks; defaults to the game's usual target.</param>
internal sealed record LootListDocument(
    [property: JsonPropertyName("items")] IReadOnlyList<LootItemDocument?> Items,
    [property: JsonPropertyName("stacks")] LootCountDocument? Stacks = null);

/// <summary>An item of a loot rule's contents.</summary>
/// <param name="Item">The item's technical name (<c>DA_Item_…</c>).</param>
/// <param name="Weight">Relative chance among the rule's items.</param>
/// <param name="Amount">Stack size range; defaults to 1.</param>
internal sealed record LootItemDocument(
    [property: JsonPropertyName("item")] string Item,
    [property: JsonPropertyName("weight")] double Weight = 1,
    [property: JsonPropertyName("amount")] LootCountDocument? Amount = null);

/// <summary>A whole-number range, both ends included.</summary>
internal sealed record LootCountDocument(
    [property: JsonPropertyName("min")] int Min,
    [property: JsonPropertyName("max")] int Max);

/// <summary>Reads content files: strict JSON in the backend's wire format, unknown members refused.</summary>
internal static class AresContentReader
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.Strict,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowOutOfOrderMetadataProperties = true,
    };

    /// <summary>Reads a content file from a content folder.</summary>
    /// <param name="directory">The content folder.</param>
    /// <param name="file">The content file name.</param>
    /// <exception cref="AresContentException">The file is missing, cannot be read or is not valid for <typeparamref name="T"/>.</exception>
    public static T Read<T>(string directory, string file)
    {
        string path = Path.Combine(directory, file);
        if (!File.Exists(path))
        {
            throw new AresContentException($"The content folder {directory} has no {file}. Every content file must be there; ARES ships them in content/.");
        }

        try
        {
            using Stream stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, s_options) ?? throw new AresContentException($"{path} is empty (null).");
        }
        catch (JsonException error)
        {
            throw new AresContentException($"{path} is not valid content: {error.Message}", error);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new AresContentException($"Cannot read {path}: {error.Message}", error);
        }
    }

    /// <summary>The error for a file whose content contradicts itself.</summary>
    public static AresContentException Invalid(string directory, string file, string problem) => new($"{Path.Combine(directory, file)}: {problem}");
}
