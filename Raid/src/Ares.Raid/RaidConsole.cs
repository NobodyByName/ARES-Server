using System.Globalization;
using System.Text.Json.Nodes;
using Ares.Common.Logging;
using Ares.Common.Terminal;
using Ares.Structs.Core;

namespace Ares.Raid;

/// <summary>The Raid operator console: Raid's status header and the built-in commands.</summary>
internal sealed class RaidConsole(string mode, TextWriter? output = null, string? logFile = null)
    : OperatorConsole("ARES-Raid", GameBuild.SteamBuildId.ToString(CultureInfo.InvariantCulture), mode, output, logFile), ILoggerProvider
{
    protected override string ActivityLine
    {
        get
        {
            string connections = Status("connections", "0");
            string activity = connections == "0" ? "waiting for player" : Status("player_phase", "connecting");
            return $"Listen {Status("listening", "starting")}   |   Connections {connections}   |   Player {activity}";
        }
    }

    public ILogger CreateLogger(string categoryName) => new ServiceLogger(Log, categoryName);

    // Forwards framework warnings and errors to the console's event log.
    private sealed class ServiceLogger(JsonEventLog log, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                log.Write(new JsonObject
                {
                    ["level"] = logLevel.ToString().ToUpperInvariant(),
                    ["source"] = category,
                    ["message"] = formatter(state, exception),
                    ["exception"] = exception?.ToString(),
                });
            }
        }
    }
}
