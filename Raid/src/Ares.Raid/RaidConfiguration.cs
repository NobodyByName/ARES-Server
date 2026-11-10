using System.Net;
using Ares.Common.CommandLine;
using Ares.Common.Endpoints;
using Ares.Common.Security;
using Ares.Raid.World;

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

    /// <summary>Lighting of the raid, or null for a weighted pick.</summary>
    public string? Lighting { get; private init; }

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
        int maxPlayers = arguments.GetInt32("max-players", 1);
        int fillSeconds = arguments.GetInt32("fill-seconds", 60);
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

        if (lighting is not null && !DamWorlds.Lightings.Contains(lighting))
        {
            throw new CommandLineException($"--lighting must be one of: {string.Join(", ", DamWorlds.Lightings)}");
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
