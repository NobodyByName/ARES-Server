using System.Globalization;
using Ares.Host.Services.Status;
using SupportedGame = Ares.Structs.Core.GameBuild;

namespace Ares.Host.Services;

/// <summary>Validated Host configuration.</summary>
public sealed record HostSettings
{
    /// <summary>Default <see cref="ServerName"/>.</summary>
    public const string DefaultServerName = "ARES-Host";

    /// <summary>Default <see cref="RaidCapacity"/>.</summary>
    public const int DefaultRaidCapacity = 1;

    /// <summary>Name shown in launcher server lists.</summary>
    public string ServerName { get; init; } = DefaultServerName;

    /// <summary>Message of the day shown in launcher server lists.</summary>
    public string Motd { get; init; } = "";

    /// <summary>Steam build id of the supported game version.</summary>
    public string GameBuild { get; init; } = SupportedGame.SteamBuildId.ToString(CultureInfo.InvariantCulture);

    /// <summary>Concurrent raids this deployment hosts (advertised in the public status).</summary>
    public int RaidCapacity { get; init; } = DefaultRaidCapacity;

    /// <summary>The origin players configure, e.g. <c>http://127.0.0.1:18080</c>.</summary>
    public required string PublicOrigin { get; init; }

    /// <summary>Identity token issuer: <see cref="PublicOrigin"/> plus "/". The client compares it to its configured endpoint.</summary>
    public string Issuer => PublicOrigin.TrimEnd('/') + "/";

    /// <summary>Operator key that signs and checks identity tokens; null to use a random key for this run only.</summary>
    public byte[]? IdentityKey { get; init; }

    /// <summary>Game archive keys served to ARES-Launcher, supplied by the operator.</summary>
    public required ArchiveKeys ArchiveKeys { get; init; }

    /// <summary>MongoDB URI; operators can supply credentials through a private URI file.</summary>
    public required string MongoDbUri { get; init; }

    /// <summary>One database per independent Host/private server.</summary>
    public string MongoDbDatabase { get; init; } = "ares_host";

    /// <summary>Scale factor applied to the bootstrap reply delays some routes need (tests use 0).</summary>
    public double RouteDelayScale { get; init; } = 1.0;
}
