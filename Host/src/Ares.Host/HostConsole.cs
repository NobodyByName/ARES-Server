using System.Globalization;
using System.Text.Json.Nodes;
using Ares.Common.Logging;
using Ares.Common.Terminal;
using Ares.Structs.Core;

namespace Ares.Host;

/// <summary>The Host operator console: Host's status header and its account, inventory and raid commands.</summary>
internal sealed class HostConsole : OperatorConsole, ILoggerProvider
{
    private readonly HostCommands _commands;

    public HostConsole(string mode, TextWriter? output = null, string? logFile = null)
        : base("ARES-Host", GameBuild.SteamBuildId.ToString(CultureInfo.InvariantCulture), mode, output, logFile)
    {
        _commands = new HostCommands(Log);
    }

    protected override string ActivityLine => $"Listen {Status("listening", "starting")}   |   Matchmaking {Status("local_matchmaking", "idle")}";

    protected override IReadOnlyList<ConsoleCommand> Commands => _commands.All;

    protected override IReadOnlyList<string> HelpNotes => HostCommands.Notes;

    /// <summary>Gives the commands the services they act on, once Host is built.</summary>
    /// <param name="services">The services.</param>
    public void Attach(HostServices services) => _commands.Services = services;

    public ILogger CreateLogger(string categoryName) => new ServiceLogger(Log, categoryName);

    protected override string? Redact(string line) => HostCommands.Redact(line);

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
