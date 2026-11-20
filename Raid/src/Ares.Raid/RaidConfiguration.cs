using System.Net;
using Ares.Common.CommandLine;
using Ares.Common.Endpoints;
using Ares.Common.Game;
using Ares.Common.Security;

namespace Ares.Raid;

/// <summary>Everything the Raid needs to start, read from the command line.</summary>
public sealed record RaidConfiguration
{
    /// <summary>UDP game port when <c>--port</c> is not given.</summary>
    private const int DefaultPort = 7777;

    /// <summary>Command-line help.</summary>
    public static readonly string Usage = $"""
        Usage: ARES-Raid [options]

        ARES raid server: hosts one Dam raid for the party the Host reserves it for, then exits.
        ARES-Host starts it during matchmaking.

          --port <n>                  UDP game port (default {DefaultPort})
          --bind <ip>                 Game listen address (default 127.0.0.1)
          --advertise-host <host>     Host name or IPv4 address players connect to (required)
          --control-port <n>          Allocation API port, on loopback (required)
          --control-token-file <f>    Operator token shared with the Host (required)
          --max-players <n>           Most players in one raid, across every party (default 1)
          --fill-seconds <n>          Seconds after the first party during which other parties may join (0 to 600, default 60)
          --stop-on-input-close       Stop when standard input closes (ARES-Host keeps it open while it runs)
          --lighting <tag>            Lighting of the raid (default: picked by the map's weights)
          --loot-cull-distance <cm>   Distance within which a loot container is relevant to a player (default {RaidSettings.DefaultLootCullDistance})
          --extraction-seconds <n>    Seconds an elevator stays ready before it leaves on its own (default {RaidSettings.DefaultExtractionSeconds})
          --extraction-ready-seconds <n>
                                      Seconds from the end of an elevator's call to its doors opening (default {RaidSettings.DefaultExtractionReadySeconds})
          --game-data-dir <dir>       Folder with the game data written by ARES-GameDataExtract (required to start)
          --content-dir <dir>         Folder with the ARES content files (required to start; ARES-Host passes its
                                      content-dir)
          --log-file <path>           Append JSON events to a file while keeping the console interactive
          --console <mode>            Console: auto, interactive, plain or json (default auto)
          --help                      Show this help
          --version                   Show the ARES version and supported game build
        """;

    /// <summary>Game listen address.</summary>
    public required IPAddress Bind { get; init; }

    /// <summary>UDP game port.</summary>
    public required int Port { get; init; }

    /// <summary>Host players connect to.</summary>
    public required string AdvertiseHost { get; init; }

    /// <summary>Allocation API port (loopback).</summary>
    public required int ControlPort { get; init; }

    /// <summary>Operator token of the allocation API.</summary>
    public required string OperatorToken { get; init; }

    /// <summary>Most players in one raid, across every party.</summary>
    public int MaxPlayers { get; private init; } = 1;

    /// <summary>Seconds after the first party during which other parties may join.</summary>
    public int FillSeconds { get; private init; } = 60;

    /// <summary>Stop when standard input closes, so a Raid never outlives the Host that started it.</summary>
    public bool StopOnInputClose { get; private init; }

    /// <summary>Lighting of the raid, or null for a weighted pick; checked against the map's lightings once the game data is loaded.</summary>
    public string? Lighting { get; private init; }

    /// <summary>The plain raid values Host passes on from its configuration.</summary>
    public RaidSettings Settings { get; private init; } = RaidSettings.Default;

    /// <summary>Folder with the game data, or null when none is configured.</summary>
    public string? GameDataDirectory { get; private init; }

    /// <summary>Folder with the operator's ARES content files, or null for the built-in defaults alone.</summary>
    public string? ContentDirectory { get; private init; }

    /// <summary>Optional structured log file, independent of terminal output.</summary>
    public string? LogFile { get; private init; }

    /// <summary>Operator console mode.</summary>
    public string ConsoleMode { get; private init; } = "auto";

    /// <summary>Parses and validates the command line.</summary>
    /// <param name="args">Arguments.</param>
    /// <returns>The configuration.</returns>
    /// <exception cref="CommandLineException">Invalid usage.</exception>
    public static RaidConfiguration Parse(IReadOnlyList<string> args)
    {
        CommandLineArguments arguments = CommandLineArguments.Parse(args, new HashSet<string> { "help", "version", "stop-on-input-close" });
        if (arguments.HelpRequested)
        {
            throw new HelpRequestedException();
        }

        if (arguments.VersionRequested)
        {
            throw new VersionRequestedException();
        }

        int port = arguments.GetInt32("port", DefaultPort);
        string bindText = arguments.GetString("bind", "127.0.0.1")!;
        string? advertiseHost = arguments.GetString("advertise-host");
        int controlPort = arguments.GetInt32("control-port", 0);
        string? tokenFile = arguments.GetString("control-token-file");
        string? lighting = arguments.GetString("lighting");
        string? gameDataDirectory = arguments.GetString("game-data-dir");
        string? contentDirectory = arguments.GetString("content-dir");
        int maxPlayers = arguments.GetInt32("max-players", 1);
        int fillSeconds = arguments.GetInt32("fill-seconds", 60);
        int lootCullDistance = arguments.GetInt32("loot-cull-distance", RaidSettings.DefaultLootCullDistance);
        int extractionSeconds = arguments.GetInt32("extraction-seconds", RaidSettings.DefaultExtractionSeconds);
        int extractionReadySeconds = arguments.GetInt32("extraction-ready-seconds", RaidSettings.DefaultExtractionReadySeconds);
        bool stopOnInputClose = arguments.Has("stop-on-input-close");
        string consoleMode = arguments.GetString("console", "auto")!;
        string? logFile = arguments.GetString("log-file");
        arguments.RejectUnknown();
        if (consoleMode is not ("auto" or "interactive" or "plain" or "json"))
        {
            throw new CommandLineException("--console must be auto, interactive, plain or json");
        }

        if (maxPlayers < 1)
        {
            throw new CommandLineException("--max-players must be at least 1");
        }

        if (fillSeconds is < 0 or > 600)
        {
            throw new CommandLineException("--fill-seconds must be from 0 to 600");
        }

        if (lootCullDistance < 1)
        {
            throw new CommandLineException("--loot-cull-distance must be at least 1");
        }

        if (extractionSeconds < 0 || extractionReadySeconds < 0)
        {
            throw new CommandLineException("--extraction-seconds and --extraction-ready-seconds must be 0 or more");
        }

        if (port is < 1 or > 65535 || controlPort is < 1 or > 65535 || port == controlPort)
        {
            throw new CommandLineException("--port and --control-port must be different ports from 1 to 65535");
        }

        if (!IPAddress.TryParse(bindText, out IPAddress? bind))
        {
            throw new CommandLineException("--bind must be an IP address");
        }

        if (advertiseHost is null || tokenFile is null)
        {
            throw new CommandLineException("--advertise-host and --control-token-file are required");
        }

        try
        {
            return new RaidConfiguration
            {
                ConsoleMode = consoleMode,
                LogFile = logFile,
                Bind = bind,
                Port = port,
                AdvertiseHost = ConnectionHost.Validate(advertiseHost),
                ControlPort = controlPort,
                OperatorToken = SecretFiles.ReadOperatorToken(tokenFile),
                Lighting = lighting,
                Settings = new RaidSettings(lootCullDistance, extractionSeconds, extractionReadySeconds),
                GameDataDirectory = gameDataDirectory is null ? null : Path.GetFullPath(gameDataDirectory),
                ContentDirectory = contentDirectory is null ? null : Path.GetFullPath(contentDirectory),
                MaxPlayers = maxPlayers,
                FillSeconds = fillSeconds,
                StopOnInputClose = stopOnInputClose,
            };
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        {
            throw new CommandLineException(error.Message);
        }
    }
}
