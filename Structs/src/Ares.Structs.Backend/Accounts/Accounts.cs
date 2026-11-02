using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Accounts;

/// <summary>Request of <c>POST /v1/shared/profile/display-name/validate</c> and <c>PUT /v1/shared/profile/display-name</c>.</summary>
public sealed record DisplayNameRequest([property: JsonPropertyName("displayName")] string? DisplayName);

/// <summary>Response of <c>POST /v1/shared/profile/display-name/validate</c>.</summary>
public sealed record DisplayNameValidationResponse(
    [property: JsonPropertyName("valid")] bool Valid,
    [property: JsonPropertyName("errors")] IReadOnlyList<string> Errors);

/// <summary>A persistent per-player key/value entry (UI flags such as tutorial completion).</summary>
public sealed record PersistentPlayerKey(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("etag")] string ETag);

/// <summary>A key/value pair the client asks to store.</summary>
public sealed record PersistentPlayerKeyWrite(
    [property: JsonPropertyName("key")] string? Key,
    [property: JsonPropertyName("value")] string? Value);

/// <summary>Request of <c>POST /v1/shared/set-persistent-player-keys</c>.</summary>
public sealed record SetPersistentPlayerKeysRequest(
    [property: JsonPropertyName("persistentPlayerKeys")] IReadOnlyList<PersistentPlayerKeyWrite?>? PersistentPlayerKeys);

/// <summary>Response of the get/set persistent player key routes.</summary>
public sealed record PersistentPlayerKeysResponse(
    [property: JsonPropertyName("persistentPlayerKeys")] IReadOnlyList<PersistentPlayerKey> PersistentPlayerKeys);

/// <summary>Persistent player key names the client reads at startup.</summary>
public static class PersistentPlayerKeyNames
{
    public const string AccountCreatedSeen = "UI.PersistentPlayerKey.FTUE.FirstStartup.AccountCreatedSeen";
    public const string FirstStartupDone = "UI.PersistentPlayerKey.FTUE.FirstStartup.Done";
    public const string CharacterCreatorSeen = "UI.PersistentPlayerKey.FTUE.FirstStartup.CharacterCreatorSeen";
    public const string SettingsSeen = "UI.PersistentPlayerKey.FTUE.FirstStartup.SettingsSeen";
    public const string Video1ASeen = "UI.PersistentPlayerKey.FTUE.FirstStartup.Video1aSeen";
    public const string Video1BSeen = "UI.PersistentPlayerKey.FTUE.FirstStartup.Video1bSeen";
    public const string OnboardingLevelFinished = "UI.PersistentPlayerKey.FTUE.FirstStartup.OnboardingLevelFinished";
    public const string FirstStartupExtracted = "UI.PersistentPlayerKey.FTUE.FirstStartup.Extracted";
    public const string HasSeenUpdateEmailModal = "UI.PersistentPlayerKey.Account.HasSeenUpdateEmailModal";
    public const string Graduated = "UI.PersistentPlayerKey.FTUE.Graduated";
}
