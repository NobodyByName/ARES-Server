using System.Globalization;
using System.Net;
using Ares.Common.Archives;
using Ares.Common.CommandLine;
using Ares.Common.Endpoints;
using Ares.Common.Security;
using Ares.Host.Services;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Persistence;
using Ares.Structs.Core;

namespace Ares.Host;

/// <summary>Everything Host needs to start, read from its configuration file and command-line overrides.</summary>
public sealed record HostConfiguration
{
    /// <summary>Command-line help.</summary>
    public static string Usage => HostOptions.Usage;

    /// <summary>Absolute path of the loaded configuration file.</summary>
    public string? ConfigurationFile { get; private init; }

    /// <summary>Validated service settings.</summary>
    public required HostSettings Settings { get; init; }

    /// <summary>Listen address.</summary>
    public required IPAddress Bind { get; init; }

    /// <summary>Listen port.</summary>
    public required int Port { get; init; }

    /// <summary>Whether non-loopback clients were explicitly allowed.</summary>
    public bool AllowNetworkClients { get; private init; }

    /// <summary>How Host starts Raid servers, or null to keep matchmaking in MATCHING.</summary>
    public RaidLaunch? Raids { get; private init; }

    /// <summary>Optional structured log file, independent of terminal output.</summary>
    public string? LogFile { get; private init; }

    /// <summary>Settings the configuration file leaves out, which take their defaults.</summary>
    public IReadOnlyList<string> MissingSettings { get; private init; } = [];

    /// <summary>Operator console mode.</summary>
    public string ConsoleMode { get; private init; } = "auto";

    /// <summary>Loads configuration, applies command-line overrides, validates settings and reads key files.</summary>
    /// <exception cref="CommandLineException">Invalid usage, or missing or wrong archive keys.</exception>
    public static HostConfiguration Parse(IReadOnlyList<string> args)
    {
        return Parse(args, GameBuild.ArchiveKeysSha256);
    }

    /// <summary>Loads configuration and verifies archive keys against the supplied fingerprint.</summary>
    internal static HostConfiguration Parse(IReadOnlyList<string> args, string archiveKeysSha256, string? workingDirectory = null)
    {
        CommandLineArguments arguments = CommandLineArguments.Parse(args, HostOptions.Flags);
        if (arguments.HelpRequested)
        {
            throw new HelpRequestedException();
        }

        if (arguments.VersionRequested)
        {
            throw new VersionRequestedException();
        }

        HostConfigurationFile file = HostConfigurationFile.Load(arguments, workingDirectory ?? Environment.CurrentDirectory);
        int port = file.GetInt32(HostOptions.Port);
        string bindText = file.GetString(HostOptions.Bind)!;
        bool allowNetwork = file.GetBoolean(HostOptions.AllowNetworkClients);
        string? publicOrigin = EmptyToNull(file.GetString(HostOptions.PublicOrigin));
        string serverName = file.GetString(HostOptions.ServerName)!;
        string motd = file.GetString(HostOptions.Motd)!;
        string gameBuild = EmptyToNull(file.GetString(HostOptions.GameBuildId)) ?? HostOptions.GameBuildId.Default!;
        int raidCapacity = file.GetInt32(HostOptions.RaidCapacity);
        string? raidExecutable = file.GetPath(HostOptions.RaidExecutable);
        string? controlTokenFile = file.GetPath(HostOptions.SessionControlTokenFile);
        int raidPlayers = file.GetInt32(HostOptions.RaidPlayers);
        int raidFillSeconds = file.GetInt32(HostOptions.RaidFillSeconds);
        int raidPort = file.GetInt32(HostOptions.RaidPort);
        int raidControlPort = file.GetInt32(HostOptions.RaidControlPort);
        string? raidLighting = EmptyToNull(file.GetString(HostOptions.RaidLighting));
        string? gameDataDirectory = file.GetPath(HostOptions.GameDataDirectory);
        string? authKeyFile = file.GetPath(HostOptions.AuthKeyFile);
        string archiveKeysFile = file.GetPath(HostOptions.ArchiveKeysFile)
            ?? throw new CommandLineException("archive-keys-file must not be empty");
        string? mongoUriFile = file.GetPath(HostOptions.MongoDbUriFile);
        string mongoDatabase = file.GetString(HostOptions.MongoDbDatabase)!;
        string? mongoUri = EmptyToNull(file.GetString(HostOptions.MongoDbUri));
        if (mongoUriFile is not null)
        {
            try
            {
                mongoUri = File.ReadAllText(mongoUriFile).Trim();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                throw new CommandLineException("Cannot read mongodb-uri-file");
            }
        }

        if (string.IsNullOrEmpty(mongoUri))
        {
            throw new CommandLineException("MongoDB connection requires mongodb-uri or mongodb-uri-file");
        }

        try
        {
            HostDatabase.ValidateConfiguration(mongoUri, mongoDatabase);
        }
        catch (ArgumentException error)
        {
            throw new CommandLineException(error.Message);
        }

        string? stashUpgradeCoins = EmptyToNull(file.GetString(HostOptions.StashUpgradeCoins));
        string consoleMode = file.GetString(HostOptions.Console)!;
        string? logFile = file.GetPath(HostOptions.LogFile);
        file.RejectUnknown();
        if (consoleMode is not ("auto" or "interactive" or "plain" or "json"))
        {
            throw new CommandLineException("--console must be auto, interactive, plain or json");
        }

        if (port is < 1 or > 65535)
        {
            throw new CommandLineException("--port must be 1-65535");
        }

        if (serverName.Length > 64 || motd.Length > 200 || gameBuild.Length == 0 || !gameBuild.All(char.IsAsciiDigit))
        {
            throw new CommandLineException("Invalid --server-name/--motd/--game-build");
        }

        if (raidCapacity is < 0 or > 10000)
        {
            throw new CommandLineException("Invalid --raid-capacity");
        }

        if (!IPAddress.TryParse(bindText, out IPAddress? bind))
        {
            throw new CommandLineException("--bind must be an IP address");
        }

        if (!IPAddress.IsLoopback(bind) && !allowNetwork)
        {
            throw new CommandLineException("Non-loopback --bind requires --allow-network-clients");
        }

        string origin = publicOrigin ?? $"http://127.0.0.1:{port.ToString(CultureInfo.InvariantCulture)}";
        if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? originUri) || originUri.Scheme != "http" || originUri.Host.Length == 0
            || originUri.Port != port || originUri.AbsolutePath is not ("" or "/") || originUri.Query.Length > 0
            || originUri.Fragment.Length > 0 || originUri.UserInfo.Length > 0 || !originUri.Authority.Contains(':', StringComparison.Ordinal))
        {
            throw new CommandLineException("--public-origin must be http://host:<--port> with no path");
        }

        if (!Loopback.IsLoopbackHost(originUri.Host) && !allowNetwork)
        {
            throw new CommandLineException("A non-loopback --public-origin requires --allow-network-clients");
        }

        RaidLaunch? raids = ReadRaidLaunch(raidExecutable, controlTokenFile, raidCapacity, raidPlayers, raidFillSeconds, raidPort, raidControlPort, raidLighting,
            gameDataDirectory, bind, originUri.Host, port);
        StashUpgradePrices stashUpgrades = stashUpgradeCoins is null ? StashUpgradePrices.Default : ReadStashUpgrades(stashUpgradeCoins);
        byte[]? identityKey = authKeyFile is null ? null : ReadIdentityKey(authKeyFile);
        ArchiveKeys archiveKeys = ReadArchiveKeys(archiveKeysFile, archiveKeysSha256);

        return new HostConfiguration
        {
            ConfigurationFile = file.Path,
            MissingSettings = file.Missing(),
            Settings = new HostSettings
            {
                ServerName = serverName,
                Motd = motd,
                GameBuild = gameBuild,
                RaidCapacity = raidCapacity,
                PublicOrigin = origin.TrimEnd('/'),
                IdentityKey = identityKey,
                ArchiveKeys = archiveKeys,
                MongoDbUri = mongoUri,
                MongoDbDatabase = mongoDatabase,
                StashUpgrades = stashUpgrades,
            },
            ConsoleMode = consoleMode,
            LogFile = logFile,
            Bind = bind,
            Port = port,
            AllowNetworkClients = allowNetwork,
            Raids = raids,
        };
    }

    private static string? EmptyToNull(string? value) => value == "" ? null : value;

    private static StashUpgradePrices ReadStashUpgrades(string text)
    {
        string[] parts = text.Split(',', StringSplitOptions.TrimEntries);
        int expected = StashUpgradePrices.Default.Coins.Count;
        long[] coins = [.. parts.Select(part => long.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out long price) ? price : -1)];
        return coins.Length == expected && coins.All(price => price >= 0) ? new StashUpgradePrices(coins)
            : throw new CommandLineException($"--stash-upgrade-coins expects {expected} comma-separated coin prices of zero or more");
    }

    private static RaidLaunch? ReadRaidLaunch(string? executable, string? tokenFile, int capacity, int players, int fillSeconds, int gamePort,
        int controlPort, string? lighting, string? gameDataDirectory, IPAddress bind, string advertiseHost, int hostPort)
    {
        if (players < 1)
        {
            throw new CommandLineException("--raid-players must be at least 1");
        }

        if (fillSeconds is < 0 or > 600)
        {
            throw new CommandLineException("--raid-fill-seconds must be from 0 to 600");
        }

        int last = Math.Max(capacity, 1) - 1;
        if (gamePort < 1 || gamePort + last > 65535 || controlPort < 1 || controlPort + last > 65535)
        {
            throw new CommandLineException("--raid-port and --raid-control-port must leave one port from 1 to 65535 for each raid of --raid-capacity");
        }

        if (hostPort >= controlPort && hostPort <= controlPort + last)
        {
            throw new CommandLineException("--raid-control-port range must not include the Host --port");
        }

        if (executable is null && tokenFile is null)
        {
            return null;
        }

        if (executable is null || tokenFile is null)
        {
            throw new CommandLineException("Starting raids requires both raid-executable and session-control-token-file");
        }

        string fullExecutable = Path.GetFullPath(executable);
        if (!File.Exists(fullExecutable))
        {
            throw new CommandLineException($"raid-executable not found: {fullExecutable}");
        }

        try
        {
            string fullTokenFile = Path.GetFullPath(tokenFile);
            return new RaidLaunch(fullExecutable, fullTokenFile, SecretFiles.ReadOperatorToken(fullTokenFile), bind,
                ConnectionHost.Validate(advertiseHost), gamePort, controlPort, players, fillSeconds, lighting,
                gameDataDirectory is null ? null : Path.GetFullPath(gameDataDirectory));
        }
        catch (Exception error) when (error is IOException or ArgumentException or UnauthorizedAccessException)
        {
            throw new CommandLineException(error.Message);
        }
    }

    private static ArchiveKeys ReadArchiveKeys(string path, string expectedSha256)
    {
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new CommandLineException(
                $"Archive keys file not found: {fullPath}. ARES-Host needs the game's archive keys in the configured archive-keys-file.");
        }

        ArchiveKeys keys;
        try
        {
            keys = ArchiveKeys.Parse(File.ReadAllText(fullPath));
        }
        catch (Exception error) when (error is IOException or FormatException or UnauthorizedAccessException)
        {
            throw new CommandLineException($"Archive keys file {fullPath}: {error.Message}");
        }

        return keys.Fingerprint() == expectedSha256
            ? keys
            : throw new CommandLineException($"Archive keys file {fullPath}: these keys do not match Steam build {GameBuild.SteamBuildId}");
    }

    private static byte[] ReadIdentityKey(string path)
    {
        try
        {
            return SecretFiles.ReadKey(path);
        }
        catch (Exception error) when (error is IOException or ArgumentException or UnauthorizedAccessException)
        {
            throw new CommandLineException(error.Message);
        }
    }
}
