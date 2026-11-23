using System.Globalization;
using Ares.Common.Game;
using Ares.Host.Services;
using Ares.Structs.Backend.Bootstrap;
using Ares.Structs.Core;

namespace Ares.Host;

/// <summary>One Host option: its key (in ares-host.conf and as <c>--key</c>), its argument and its help.</summary>
/// <param name="Key">Setting key.</param>
/// <param name="Argument">Argument placeholder shown in the help; empty for a flag.</param>
/// <param name="Help">One-line description.</param>
/// <param name="Default">Value used when neither the file nor the command line sets it.</param>
/// <param name="DefaultNote">Shown in the help instead of a value when the default is not a plain value.</param>
internal sealed record HostOption(string Key, string Argument, string Help, string? Default = null, string? DefaultNote = null)
{
    /// <summary>Whether the option takes no argument.</summary>
    internal bool IsFlag => Argument.Length == 0;

    /// <summary>The option's help line.</summary>
    internal string HelpLine
    {
        get
        {
            string name = IsFlag ? $"--{Key}" : $"--{Key} {Argument}";
            string suffix = DefaultNote is not null ? $" (default: {DefaultNote})"
                : string.IsNullOrEmpty(Default) ? ""
                : $" (default {Default})";
            return $"  {name,-33} {Help}{suffix}";
        }
    }
}

/// <summary>Every Host option, in help order: the single definition of keys, defaults and help.</summary>
internal static class HostOptions
{
    internal static HostOption Config { get; } = new("config", "<path>", "Existing configuration file (default ares-host.conf; auto-created)");
    internal static HostOption Port { get; } = new("port", "<n>", "Listen port", "18080");
    internal static HostOption Bind { get; } = new("bind", "<ip>", "Listen address; non-loopback requires --allow-network-clients", "127.0.0.1");
    internal static HostOption PublicOrigin { get; } = new("public-origin", "<http://host:port>", "Origin players configure; also the token issuer",
        DefaultNote: "http://127.0.0.1:<port>");
    internal static HostOption AllowNetworkClients { get; } = new("allow-network-clients", "", "Serve players beyond this machine (plain HTTP; trust unverified)");
    internal static HostOption MongoDbUri { get; } = new("mongodb-uri", "<uri>", "MongoDB connection URI");
    internal static HostOption MongoDbUriFile { get; } = new("mongodb-uri-file", "<path>", "Private URI file; takes precedence over mongodb-uri");
    internal static HostOption MongoDbDatabase { get; } = new("mongodb-database", "<name>", "Database for this Host/private server", "ares_host");
    internal static HostOption RaidExecutable { get; } = new("raid-executable", "<path>", "ARES-Raid executable Host starts for matchmaking",
        DefaultNote: "none; tickets stay MATCHING");
    internal static HostOption SessionControlTokenFile { get; } = new("session-control-token-file", "<f>", "Operator token Host shares with the Raid servers it starts");
    internal static HostOption RaidPlayers { get; } = new("raid-players", "<n>", "Most players in one raid, across every party", "1");
    internal static HostOption RaidFillSeconds { get; } = new("raid-fill-seconds", "<n>",
        "Seconds after a raid's first party during which other parties may join, 0 to 600", "60");
    internal static HostOption RaidPort { get; } = new("raid-port", "<n>", "UDP game port of the first raid; each further raid uses the next port", "7777");
    internal static HostOption RaidControlPort { get; } = new("raid-control-port", "<n>", "Loopback allocation API port of the first raid, counted the same way", "17778");
    internal static HostOption RaidLighting { get; } = new("raid-lighting", "<tag>", "Lighting of every raid", DefaultNote: "picked by the map's weights");
    internal static HostOption LootCullDistance { get; } = new("loot-cull-distance", "<cm>", "Distance within which a loot container is relevant to a player",
        Text(RaidSettings.DefaultLootCullDistance));
    internal static HostOption ExtractionSeconds { get; } = new("extraction-seconds", "<n>", "Seconds an elevator stays ready before it leaves on its own",
        Text(RaidSettings.DefaultExtractionSeconds));
    internal static HostOption ExtractionReadySeconds { get; } = new("extraction-ready-seconds", "<n>", "Seconds from the end of an elevator's call to its doors opening",
        Text(RaidSettings.DefaultExtractionReadySeconds));
    internal static HostOption ExploitTrapResetSeconds { get; } = new("exploit-trap-reset-seconds", "<n>",
        "Seconds a locked room's anti-exploit jets keep firing after the last player left, 0 to turn the trap off", Text(RaidSettings.DefaultExploitTrapResetSeconds));
    internal static HostOption GameDataDirectory { get; } = new("game-data-dir", "<dir>",
        "Folder with the game data written by ARES-GameDataExtract; required, Host and every raid load it");
    internal static HostOption ContentDirectory { get; } = new("content-dir", "<dir>",
        "Folder with the ARES content files (ARES ships content/), passed to every raid", "content");
    internal static HostOption StashUpgradeCoins { get; } = new("stash-upgrade-coins", "<list>", "Coin price of each stash upgrade, comma-separated from the first",
        DefaultNote: "the game's prices");
    internal static HostOption ClientFeatureFlags { get; } = new("client-feature-flags", "<list>",
        "Client feature flags turned on, comma-separated; !name turns one off", ClientFeatureFlagNames.PeriodicSyncOfInventory);
    internal static HostOption AuthKeyFile { get; } = new("auth-key-file", "<f>", "Identity signing key, 32-4096 bytes", DefaultNote: "none; tokens are not checked");
    internal static HostOption ArchiveKeysFile { get; } = new("archive-keys-file", "<f>", "Game archive keys for ARES-Launcher, required", "archive-keys.txt");
    internal static HostOption ServerName { get; } = new("server-name", "<text>", "Shown in launcher server lists", HostSettings.DefaultServerName);
    internal static HostOption Motd { get; } = new("motd", "<text>", "Message of the day for launcher server lists", "");
    internal static HostOption GameBuildId { get; } = new("game-build", "<digits>", "Steam build id this server supports",
        GameBuild.SteamBuildId.ToString(CultureInfo.InvariantCulture));
    internal static HostOption RaidCapacity { get; } = new("raid-capacity", "<n>", "Most raids Host runs at once",
        HostSettings.DefaultRaidCapacity.ToString(CultureInfo.InvariantCulture));
    internal static HostOption LogFile { get; } = new("log-file", "<path>", "Append JSON events to a file while keeping the console interactive");
    internal static HostOption Console { get; } = new("console", "<mode>", "Console: auto, interactive, plain or json", "auto");
    internal static HostOption Help { get; } = new("help", "", "Show this help");
    internal static HostOption Version { get; } = new("version", "", "Show the ARES version and supported game build");

    /// <summary>The settings ares-host.conf holds, in help order.</summary>
    internal static IReadOnlyList<HostOption> Settings { get; } =
    [
        Port, Bind, PublicOrigin, AllowNetworkClients, MongoDbUri, MongoDbUriFile, MongoDbDatabase, RaidExecutable, SessionControlTokenFile,
        RaidPlayers, RaidFillSeconds, RaidPort, RaidControlPort, RaidLighting, LootCullDistance, ExtractionSeconds, ExtractionReadySeconds, ExploitTrapResetSeconds, GameDataDirectory, ContentDirectory, StashUpgradeCoins, ClientFeatureFlags, AuthKeyFile,
        ArchiveKeysFile, ServerName, Motd, GameBuildId, RaidCapacity, LogFile, Console,
    ];

    /// <summary>Options that take no argument.</summary>
    internal static IReadOnlySet<string> Flags { get; } = new HashSet<string>([.. Settings.Append(Help).Append(Version).Where(option => option.IsFlag).Select(option => option.Key)]);

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Command-line help.</summary>
    internal static string Usage { get; } = $"""
        Usage: ARES-Host [options]

        ARES backend: synthetic identity, accounts, party and matchmaking. Unknown routes fail explicitly.

        {Config.HelpLine}

        Settings below override the file. File paths are relative to the configuration file;
        command-line paths are relative to the working directory. Restart Host to apply edits.

        {string.Join('\n', Settings.Select(option => option.HelpLine))}
        {Help.HelpLine}
        {Version.HelpLine}
        """;
}
