using System.Globalization;
using System.Net;
using Ares.Common.CommandLine;
using Ares.Common.Endpoints;
using Ares.Common.Security;
using Ares.Host.Services;
using Ares.Host.Services.Persistence;
using Ares.Host.Services.Status;
using Ares.Structs.Core;

namespace Ares.Host;

/// <summary>Everything Host needs to start, read from its configuration file and command-line overrides.</summary>
public sealed record HostConfiguration
{
    /// <summary>Default listen port shared with the launcher.</summary>
    private const int DefaultPort = 18080;

    /// <summary>Default archive keys file, relative to the configuration file.</summary>
    private const string DefaultArchiveKeysFile = "archive-keys.txt";

    /// <summary>Command-line help.</summary>
    public static readonly string Usage = $"""
        Usage: ARES-Host [options]

        ARES backend: synthetic identity, accounts, party and matchmaking. Unknown routes fail explicitly.

          --config <path>                   Existing configuration file (default ares-host.conf; auto-created)

        Settings below override the file. File paths are relative to the configuration file;
        command-line paths are relative to the working directory. Restart Host to apply edits.

          --port <n>                        Listen port (default {DefaultPort})
          --bind <ip>                       Listen address (default 127.0.0.1); non-loopback requires --allow-network-clients
          --public-origin <http://host:port>  Origin players configure; also the token issuer (default http://127.0.0.1:<port>)
          --allow-network-clients           Serve players beyond this machine (plain HTTP; trust unverified)
          --mongodb-uri <uri>               MongoDB connection URI
          --mongodb-uri-file <path>         Private URI file; takes precedence over mongodb-uri
          --mongodb-database <name>         Database for this Host/private server (default ares_host)
          --session-control-url <url>       Raid allocation API (loopback HTTP or HTTPS)
          --session-control-token-file <f>  Operator token shared with Raid
          --auth-key-file <f>               Identity signing key, 32-4096 bytes (default: none; tokens are not checked)
          --archive-keys-file <f>           Game archive keys for ARES-Launcher, required (default {DefaultArchiveKeysFile})
          --server-name <text>              Shown in launcher server lists (default {HostSettings.DefaultServerName})
          --motd <text>                     Message of the day for launcher server lists
          --game-build <digits>             Steam build id this server supports (default {GameBuild.SteamBuildId})
          --raid-capacity <n>               Concurrent raids this deployment hosts (default {HostSettings.DefaultRaidCapacity})
          --log-file <path>                 Append JSON events to a file while keeping the console interactive
          --console <mode>                  Console: auto, interactive, plain or json (default auto)
          --help                            Show this help
          --version                         Show the ARES version and supported game build
        """;

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

    /// <summary>Raid allocation API, or null to keep matchmaking in MATCHING.</summary>
    public Uri? SessionControlUrl { get; init; }

    /// <summary>Operator token for <see cref="SessionControlUrl"/>.</summary>
    public string? OperatorToken { get; init; }

    /// <summary>Optional structured log file, independent of terminal output.</summary>
    public string? LogFile { get; private init; }

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
        CommandLineArguments arguments = CommandLineArguments.Parse(args, new HashSet<string> { "allow-network-clients", "help", "version" });
        if (arguments.HelpRequested)
        {
            throw new HelpRequestedException();
        }

        if (arguments.VersionRequested)
        {
            throw new VersionRequestedException();
        }

        HostConfigurationFile file = HostConfigurationFile.Load(arguments, workingDirectory ?? Environment.CurrentDirectory);
        int port = file.GetInt32("port", DefaultPort);
        string bindText = file.GetString("bind", "127.0.0.1")!;
        bool allowNetwork = file.GetBoolean("allow-network-clients");
        string? publicOrigin = EmptyToNull(file.GetString("public-origin"));
        string serverName = file.GetString("server-name", HostSettings.DefaultServerName)!;
        string motd = file.GetString("motd", "")!;
        string gameBuild = EmptyToNull(file.GetString("game-build")) ?? GameBuild.SteamBuildId.ToString(CultureInfo.InvariantCulture);
        int raidCapacity = file.GetInt32("raid-capacity", HostSettings.DefaultRaidCapacity);
        string? controlUrl = EmptyToNull(file.GetString("session-control-url"));
        string? controlTokenFile = file.GetPath("session-control-token-file");
        string? authKeyFile = file.GetPath("auth-key-file");
        string archiveKeysFile = file.GetPath("archive-keys-file", DefaultArchiveKeysFile)
            ?? throw new CommandLineException("archive-keys-file must not be empty");
        string? mongoUriFile = file.GetPath("mongodb-uri-file");
        string mongoDatabase = file.GetString("mongodb-database", "ares_host")!;
        string? mongoUri = EmptyToNull(file.GetString("mongodb-uri"));
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

        string consoleMode = file.GetString("console", "auto")!;
        string? logFile = file.GetPath("log-file");
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

        Uri? sessionControl = null;
        string? operatorToken = null;
        if (controlUrl is not null || controlTokenFile is not null)
        {
            if (controlUrl is null || controlTokenFile is null)
            {
                throw new CommandLineException("Session allocation requires both control URL and operator token file");
            }

            try
            {
                sessionControl = new Uri(controlUrl, UriKind.Absolute);
                operatorToken = SecretFiles.ReadOperatorToken(controlTokenFile);
            }
            catch (Exception error) when (error is UriFormatException or IOException or ArgumentException or UnauthorizedAccessException)
            {
                throw new CommandLineException(error.Message);
            }
        }

        byte[]? identityKey = authKeyFile is null ? null : ReadIdentityKey(authKeyFile);
        ArchiveKeys archiveKeys = ReadArchiveKeys(archiveKeysFile, archiveKeysSha256);

        return new HostConfiguration
        {
            ConfigurationFile = file.Path,
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
            },
            ConsoleMode = consoleMode,
            LogFile = logFile,
            Bind = bind,
            Port = port,
            AllowNetworkClients = allowNetwork,
            SessionControlUrl = sessionControl,
            OperatorToken = operatorToken,
        };
    }

    private static string? EmptyToNull(string? value) => value == "" ? null : value;

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
