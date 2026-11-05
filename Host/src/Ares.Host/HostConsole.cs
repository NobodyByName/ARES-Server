using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common;
using Ares.Common.Logging;
using Ares.Host.Services.Inventory;
using Ares.Structs.Core;

namespace Ares.Host;

/// <summary>The Host operator console and its local commands.</summary>
internal sealed class HostConsole : ILoggerProvider
{
    private const int HistoryLimit = 100;
    private const int LogLimit = 1000;
    private const int InputLimit = 512;
    private readonly Lock _gate = new();
    private readonly TextWriter _output;
    private readonly StreamWriter? _file;
    private bool _interactive;
    private readonly List<string> _lines = [];
    private readonly List<string> _history = [];
    private readonly Dictionary<string, string> _status = [];
    private readonly StringBuilder _input = new();
    private readonly DateTimeOffset _started = DateTimeOffset.Now;
    private int _cursor;
    private int _historyIndex;
    private string _draft = "";
    private int _scroll;
    private int _columns = 79;
    private bool _dirty = true;
    private bool _disposed;

    public HostConsole(string mode, TextWriter? output = null, string? logFile = null)
    {
        _output = output ?? Console.Out;
        if (logFile is not null)
        {
            string fullPath = Path.GetFullPath(logFile);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            _file = new StreamWriter(fullPath, append: true, new UTF8Encoding(false)) { AutoFlush = true };
        }

        _interactive = output is null && mode is "auto" or "interactive"
            && !Console.IsInputRedirected && !Console.IsOutputRedirected
            && Environment.GetEnvironmentVariable("TERM") != "dumb";
        Log = new JsonEventLog(new EventWriter(this, mode == "json"));
        if (_interactive)
        {
            _output.Write("\e[?1049h\e[2J\e[H");
            _output.Flush();
        }
        else if (mode == "interactive")
        {
            _output.WriteLine("Interactive input needs a terminal. Using readable logs; enable terminal emulation in your IDE.");
        }
    }

    public JsonEventLog Log { get; }

    internal InventoryStore? Inventory { get; set; }

    public async Task RunAsync(Action stop, CancellationToken cancellationToken)
    {
        if (!_interactive)
        {
            return;
        }

        try
        {
            int previousWidth = 0;
            int previousHeight = 0;
            long previousSecond = -1;
            while (!cancellationToken.IsCancellationRequested)
            {
                bool stopRequested = false;
                lock (_gate)
                {
                    int width = Math.Max(1, Console.WindowWidth);
                    int height = Math.Max(1, Console.WindowHeight);
                    // Bound work per frame even when a large paste is waiting.
                    for (int count = 0; count < 64 && Console.KeyAvailable; count++)
                    {
                        HandleKey(Console.ReadKey(true), () => stopRequested = true, height);
                    }

                    long second = (long)(DateTimeOffset.Now - _started).TotalSeconds;
                    if (_dirty || width != previousWidth || height != previousHeight || second != previousSecond)
                    {
                        _output.Write(Render(width, height));
                        _output.Flush();
                        _dirty = false;
                        previousWidth = width;
                        previousHeight = height;
                        previousSecond = second;
                    }
                }

                if (stopRequested)
                {
                    stop();
                }

                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception error) when (error is IOException or InvalidOperationException)
        {
            // Keep the service running when terminal input closes.
            lock (_gate)
            {
                LeaveTerminal();
            }

            Log.Write("console_error", error.Message);
        }
    }

    public ILogger CreateLogger(string categoryName) => new ServiceLogger(this, categoryName);

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                LeaveTerminal();
            }
            finally
            {
                _file?.Dispose();
            }
        }
    }

    private void LeaveTerminal()
    {
        if (!_interactive)
        {
            return;
        }

        _interactive = false;
        _output.Write("\e[0m\e[?1049l");
        foreach (string line in _lines.TakeLast(30))
        {
            _output.WriteLine(line);
        }

        _output.Flush();
    }

    internal void ExecuteCommand(string command, Action stop)
    {
        lock (_gate)
        {
            string name = command.Trim();
            if (name.Length == 0)
            {
                return;
            }

            AddLine($"> {SafeText(name)}");
            switch (name.ToLowerInvariant())
            {
                case "help":
                    AddLine("help    Show commands");
                    AddLine("status  Show Host configuration and latest activity");
                    AddLine("clear   Clear this console's log view");
                    AddLine("stop    Gracefully stop ARES-Host");
                    AddLine("inventory  Show stored items and the loadout reservation");
                    AddLine("inventory import <path>  Replace an idle inventory from a JSON file");
                    AddLine("inventory add <technical-name>  Add one supported item to the stash");
                    break;
                case "status":
                    AddLine($@"ARES-Host {AresVersion.Current} | Steam build {GameBuild.SteamBuildId} | uptime {DateTimeOffset.Now - _started:hh\:mm\:ss}");
                    foreach ((string key, string value) in _status)
                    {
                        AddLine($"{key}: {value}");
                    }

                    break;
                case "clear":
                    _lines.Clear();
                    _scroll = 0;
                    _dirty = true;
                    break;
                case "stop":
                    AddLine("Stopping ARES-Host...");
                    stop();
                    break;
                case "inventory":
                    if (Inventory is { } inventory)
                    {
                        PlayerInventory state = inventory.Inventory;
                        AddLine($"Inventory revision {state.Revision}: {state.ItemCount} items, stash capacity {state.Stash.SlotCount}, backpack capacity {state.Loadout.BackpackCapacity}");
                        AddLine($"QuickUse capacity {state.Loadout.QuickUseCapacity}, Safe Pocket capacity {state.Loadout.SafePocketCapacity}");
                        AddLine($"Augment: {state.Loadout.Augment?.Type ?? "none"}");
                        AddLine($"Reserved ticket: {inventory.ReservedTicketId ?? "none"}");
                    }
                    else
                    {
                        AddLine("Inventory is unavailable.");
                    }

                    break;
                case not null when name.StartsWith("inventory import ", StringComparison.OrdinalIgnoreCase):
                    ImportInventory(name["inventory import ".Length..].Trim().Trim('"'));
                    break;
                case not null when name.StartsWith("inventory add ", StringComparison.OrdinalIgnoreCase):
                    AddInventoryItem(name["inventory add ".Length..].Trim());
                    break;
                default:
                    AddLine("Unknown command. Type help for commands.");
                    break;
            }
        }
    }

    private void ImportInventory(string path)
    {
        if (Inventory is not { } inventory)
        {
            AddLine("Inventory is unavailable.");
            return;
        }

        try
        {
            inventory.ImportFile(path, inventory.Inventory.Revision);
            PlayerInventory state = inventory.Inventory;
            AddLine($"Imported {state.ItemCount} items at revision {state.Revision}. Start a fresh client to refresh capacity changes.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or OverflowException)
        {
            AddLine($"Inventory import rejected: {SafeText(error.Message)}");
        }
    }

    private void AddInventoryItem(string technicalName)
    {
        if (Inventory is not { } inventory)
        {
            AddLine("Inventory is unavailable.");
            return;
        }

        try
        {
            inventory.AddItem(technicalName, DateTimeOffset.UtcNow);
            AddLine($"Added {technicalName} to the stash. Start a fresh client to refresh inventory.");
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or OverflowException)
        {
            AddLine($"Inventory add rejected: {SafeText(error.Message)}");
        }
    }

    internal void HandleKey(ConsoleKeyInfo key, Action stop, int height)
    {
        _dirty = true;
        switch (key.Key)
        {
            case ConsoleKey.Enter:
                string command = _input.ToString();
                _input.Clear();
                _cursor = 0;
                if (!string.IsNullOrWhiteSpace(command))
                {
                    if (_history.Count == 0 || _history[^1] != command)
                    {
                        _history.Add(command);
                        if (_history.Count > HistoryLimit)
                        {
                            _history.RemoveAt(0);
                        }
                    }

                    ExecuteCommand(command, stop);
                }

                _historyIndex = _history.Count;
                _draft = "";
                _scroll = 0;
                break;
            case ConsoleKey.Backspace when _cursor > 0:
                _input.Remove(--_cursor, 1);
                break;
            case ConsoleKey.Delete when _cursor < _input.Length:
                _input.Remove(_cursor, 1);
                break;
            case ConsoleKey.LeftArrow:
                _cursor = Math.Max(0, _cursor - 1);
                break;
            case ConsoleKey.RightArrow:
                _cursor = Math.Min(_input.Length, _cursor + 1);
                break;
            case ConsoleKey.Home when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _cursor = 0;
                break;
            case ConsoleKey.End when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _cursor = _input.Length;
                break;
            case ConsoleKey.Escape:
                _input.Clear();
                _cursor = 0;
                _historyIndex = _history.Count;
                _draft = "";
                break;
            case ConsoleKey.UpArrow when _historyIndex > 0:
                if (_historyIndex == _history.Count)
                {
                    _draft = _input.ToString();
                }

                SetInput(_history[--_historyIndex]);
                break;
            case ConsoleKey.DownArrow when _historyIndex < _history.Count:
                _historyIndex++;
                SetInput(_historyIndex == _history.Count ? _draft : _history[_historyIndex]);
                break;
            case ConsoleKey.Home:
            case ConsoleKey.PageUp:
                _scroll += Math.Max(1, height - 6);
                break;
            case ConsoleKey.End:
            case ConsoleKey.PageDown:
                _scroll = Math.Max(0, _scroll - Math.Max(1, height - 6));
                break;
            default:
                if (key.KeyChar is >= ' ' and <= '~' && !key.Modifiers.HasFlag(ConsoleModifiers.Control) && _input.Length < InputLimit)
                {
                    _input.Insert(_cursor++, key.KeyChar);
                }

                break;
        }
    }

    internal string Render(int width, int height)
    {
        // Leave the last column unused to avoid the terminal's automatic line wrap.
        int columns = Math.Max(1, width - 1);
        _columns = columns;
        int logRows = Math.Max(0, height - 6);
        List<string> wrapped = [];
        foreach (string line in _lines)
        {
            for (int offset = 0; offset < line.Length; offset += columns)
            {
                wrapped.Add(line.Substring(offset, Math.Min(columns, line.Length - offset)));
            }
        }

        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, wrapped.Count - logRows));
        int end = wrapped.Count - _scroll;
        int start = Math.Max(0, end - logRows);
        string endpoint = _status.GetValueOrDefault("listening", "starting");
        string activity = _status.GetValueOrDefault("local_matchmaking", "idle");
        List<string> rows =
        [
            $@"ARES-Host  {AresVersion.Current}   |   uptime {DateTimeOffset.Now - _started:hh\:mm\:ss}",
            $"Listen {endpoint}   |   Matchmaking {activity}",
            new('-', columns),
            .. wrapped.Skip(start).Take(logRows),
        ];
        while (rows.Count < height - 3)
        {
            rows.Add("");
        }

        rows.Add(new string('-', columns));
        rows.Add(_scroll == 0 ? "LIVE | help | Up/Down history | Home/End logs | Ctrl+C stop" : $"PAUSED {_scroll} rows above live | Home/End logs");
        int inputColumns = Math.Max(1, columns - 2);
        int inputStart = Math.Max(0, _cursor - inputColumns + 1);
        rows.Add("> " + _input.ToString(inputStart, Math.Min(inputColumns, _input.Length - inputStart)));
        if (height < 7)
        {
            rows = [.. Enumerable.Repeat("", Math.Max(0, height - 1)), rows[^1]];
        }

        StringBuilder frame = new("\e[?25l\e[H");
        for (int row = 0; row < height; row++)
        {
            string text = rows[row];
            string color = row < 2 ? "\e[1;36m" : text.Contains(" ERROR", StringComparison.Ordinal) || text.Contains(" CRITICAL", StringComparison.Ordinal)
                ? "\e[31m" : text.Contains(" WARNING", StringComparison.Ordinal) ? "\e[33m" : "\e[0m";
            frame.Append("\e[2K").Append(color).Append(text.AsSpan(0, Math.Min(columns, text.Length))).Append("\e[0m");
            if (row < height - 1)
            {
                frame.Append("\r\n");
            }
        }

        int cursorColumn = Math.Min(columns, 3 + _cursor - inputStart);
        frame.Append(CultureInfo.InvariantCulture, $"\e[{height};{cursorColumn}H\e[?25h");
        return frame.ToString();
    }

    private void SetInput(string value)
    {
        _input.Clear().Append(value);
        _cursor = _input.Length;
    }

    private void WriteEvent(JsonObject value)
    {
        lock (_gate)
        {
            foreach ((string key, JsonNode? item) in value)
            {
                if (key is "listening" or "advertised" or "allocation_api" or "public_origin" or "lighting"
                    or "network_clients" or "matchmaking" or "identity_tokens" or "local_matchmaking"
                    or "connections" or "player_phase" or "players")
                {
                    _status[key] = SafeText(DisplayValue(item));
                }
            }

            bool error = value.Any(entry => entry.Key.Contains("error", StringComparison.OrdinalIgnoreCase));
            bool warning = value["status"] is JsonValue status && status.TryGetValue(out int statusCode) && statusCode >= 400;
            string level = value["level"]?.ToString() ?? (error ? "ERROR" : warning ? "WARNING" : "INFO");
            string fields = string.Join(" | ", value.Where(entry => entry.Key != "level")
                .Select(entry => $"{entry.Key.Replace('_', ' ')}: {DisplayValue(entry.Value)}"));
            AddLine($"{DateTimeOffset.Now:HH:mm:ss} {level,-7} {SafeText(fields)}");
        }
    }

    private static string DisplayValue(JsonNode? value)
    {
        return value switch
        {
            JsonObject item => string.Join(", ", item.Select(entry => $"{entry.Key.Replace('_', ' ')}={DisplayValue(entry.Value)}")),
            JsonArray items => "[" + string.Join("; ", items.Select(DisplayValue)) + "]",
            _ => value?.ToString() ?? "null",
        };
    }

    private void AddLine(string line)
    {
        if (_disposed)
        {
            return;
        }

        _lines.Add(line);
        if (_lines.Count > LogLimit)
        {
            _lines.RemoveAt(0);
        }

        if (_scroll > 0)
        {
            _scroll += Math.Max(1, (line.Length + _columns - 1) / _columns);
        }

        _dirty = true;
        if (_interactive)
        {
            return;
        }

        _output.WriteLine(line);
        _output.Flush();
    }

    private static string SafeText(string text)
    {
        // Escape terminal controls and non-ASCII glyphs so untrusted text cannot move the cursor
        // and every rendered character occupies exactly one column.
        StringBuilder safe = new();
        foreach (char character in text.Take(8192))
        {
            if (character is >= ' ' and <= '~')
            {
                safe.Append(character);
            }
            else
            {
                safe.Append(CultureInfo.InvariantCulture, $"\\u{(int)character:x4}");
            }
        }

        return safe.ToString();
    }

    private sealed class EventWriter(HostConsole owner, bool json) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string? value)
        {
            if (value is null)
            {
                return;
            }

            lock (owner._gate)
            {
                if (owner._disposed)
                {
                    return;
                }

                owner._file?.WriteLine(value);
                if (json)
                {
                    owner._output.WriteLine(value);
                    owner._output.Flush();
                }
                else if (JsonNode.Parse(value) is JsonObject item)
                {
                    owner.WriteEvent(item);
                }
            }
        }
    }

    private sealed class ServiceLogger(HostConsole owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                owner.Log.Write(new JsonObject
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
