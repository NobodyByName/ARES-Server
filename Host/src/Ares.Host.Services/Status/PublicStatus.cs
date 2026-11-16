using System.Text.Json.Serialization;
using Ares.Host.Services.Players;

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

    /// <summary>The status of this Host right now.</summary>
    public static PublicStatus Current(HostSettings settings, PlayerRegistry players)
    {
        (int raids, int inRaids) = players.CurrentRaids;
        return new PublicStatus(1, settings.ServerName, settings.Motd, settings.GameBuild,
            new RaidCounts(raids, settings.RaidCapacity), inRaids, players.HasAllocator);
    }
}

/// <summary>Active and maximum concurrent raids.</summary>
public sealed record RaidCounts(
    [property: JsonPropertyName("active")] int Active,
    [property: JsonPropertyName("capacity")] int Capacity);
