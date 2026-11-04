using System.Text.Json.Serialization;

namespace Ares.Host.Services.Status;

/// <summary>
/// Unauthenticated launcher discovery response containing server metadata and raid counts.
/// Protocol 1; ARES-Launcher keeps its own copy of this contract.
/// </summary>
public sealed record PublicStatus(
    [property: JsonPropertyName("protocol")] int Protocol,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("motd")] string Motd,
    [property: JsonPropertyName("game_build")] string GameBuild,
    [property: JsonPropertyName("raids")] RaidCounts Raids,
    [property: JsonPropertyName("players_in_raids")] int PlayersInRaids,
    [property: JsonPropertyName("matchmaking")] bool Matchmaking)
{
    /// <summary>Current status path.</summary>
    public const string Path = "/ares/v1/status";
}

/// <summary>Active and maximum concurrent raids.</summary>
public sealed record RaidCounts(
    [property: JsonPropertyName("active")] int Active,
    [property: JsonPropertyName("capacity")] int Capacity);
