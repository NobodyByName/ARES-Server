using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Ares.Common.Logging;

namespace Ares.Common.Terminal;

/// <summary>Runs one operator command.</summary>
/// <param name="words">The command line split into words; the first is the command name.</param>
/// <param name="line">The trimmed command line.</param>
/// <param name="reply">Adds a line to the console.</param>
public delegate void ConsoleCommandHandler(string[] words, string line, Action<string> reply);

/// <summary>An operator command: its name, help, accepted word counts, handler and argument completion.</summary>
/// <param name="Name">Command name, matched case-insensitively.</param>
/// <param name="Help">Help lines.</param>
/// <param name="Run">Runs the command.</param>
/// <param name="MinimumWords">Fewest words the command line has, the name included.</param>
/// <param name="MaximumWords">Most words the command line has; null for no limit.</param>
/// <param name="Complete">Completions for the word after the given preceding words (the name first); null for none.</param>
public sealed record ConsoleCommand(
    string Name,
    IReadOnlyList<string> Help,
    ConsoleCommandHandler Run,
    int MinimumWords = 1,
    int? MaximumWords = null,
    Func<string[], IEnumerable<string>>? Complete = null);

/// <summary>
/// A server's operator console: an interactive terminal (log view, status header, command input with history and
/// completion) or plain/JSON log output, an optional JSON log file, and the built-in help, status, clear and stop commands.
/// </summary>
public abstract class OperatorConsole : IDisposable
{
    private const int HistoryLimit = 100;
    private const int LogLimit = 1000;
    private const int InputLimit = 512;
    private readonly Lock _gate = new();
    private readonly TextWriter _output;
    private readonly StreamWriter? _file;
    private readonly string _product;
    private readonly string _build;
    private readonly List<string> _lines = [];
    private readonly List<string> _history = [];
    private readonly Dictionary<string, string> _status = [];
    private readonly StringBuilder _input = new();
    private readonly DateTimeOffset _started = DateTimeOffset.Now;
    private string[] _completionCandidates = [];
    private string _completionInput = "";
    private int _completionCursor;
    private int _completionStart;
    private int _completionLength;
    private int _completionIndex = -1;
    private bool _interactive;
    private int _cursor;
    private int _historyIndex;
    private string _draft = "";
    private int _scroll;
    private int _columns = 79;
    private bool _dirty = true;
    private bool _disposed;

    /// <summary>Opens the console.</summary>
    /// <param name="product">Product name shown in the header and status.</param>
    /// <param name="build">Supported game build, shown by the status command.</param>
    /// <param name="mode">auto, interactive, plain or json.</param>
    /// <param name="output">Output writer; null for the process console (which may become interactive).</param>
    /// <param name="logFile">JSON event log file to append to; null for none.</param>
    protected OperatorConsole(string product, string build, string mode, TextWriter? output, string? logFile)
    {
        _product = product;
        _build = build;
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

    /// <summary>The event log shown by this console (and written to its log file).</summary>
    public JsonEventLog Log { get; }

    /// <summary>The second header line: the server's current activity.</summary>
    protected abstract string ActivityLine { get; }

    /// <summary>Commands beyond the built-in ones, in help order; read once, on first use.</summary>
    protected virtual IReadOnlyList<ConsoleCommand> Commands => [];

    /// <summary>Help lines shown after the commands.</summary>
    protected virtual IReadOnlyList<string> HelpNotes => [];

    /// <summary>Reads keys and redraws the terminal until cancelled; returns at once when the console is not interactive.</summary>
    /// <param name="stop">Called when the operator asks to stop.</param>
    /// <param name="cancellationToken">Stops the loop.</param>
    /// <returns>The loop.</returns>
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

    /// <summary>Restores the terminal and closes the log file.</summary>
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

        GC.SuppressFinalize(this);
    }

    /// <summary>Runs one command line.</summary>
    /// <param name="command">The command line.</param>
    /// <param name="stop">Called by the stop command.</param>
    public void ExecuteCommand(string command, Action stop)
    {
        lock (_gate)
        {
            string line = command.Trim();
            if (line.Length == 0)
            {
                return;
            }

            AddLine($"> {SafeText(Redact(line) ?? line)}");
            string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            ConsoleCommand? match = Find(words[0]);
            if (match is null || words.Length < match.MinimumWords || words.Length > match.MaximumWords)
            {
                AddLine("Unknown command. Type help for commands.");
                return;
            }

            match.Run(words, line, AddLine);
            if (match.Name == "stop")
            {
                stop();
            }
        }
    }

    /// <summary>Handles one key of the interactive input.</summary>
    /// <param name="key">The key.</param>
    /// <param name="stop">Called by the stop command.</param>
    /// <param name="height">Terminal height, for page scrolling.</param>
    public void HandleKey(ConsoleKeyInfo key, Action stop, int height)
    {
        _dirty = true;
        if (key.Key != ConsoleKey.Tab)
        {
            _completionCandidates = [];
        }

        switch (key.Key)
        {
            case ConsoleKey.Tab:
                CompleteInput(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
                break;
            case ConsoleKey.Enter:
                string command = _input.ToString();
                _input.Clear();
                _cursor = 0;
                if (!string.IsNullOrWhiteSpace(command))
                {
                    if (Redact(command.Trim()) is null && (_history.Count == 0 || _history[^1] != command))
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

    /// <summary>One full terminal frame.</summary>
    /// <param name="width">Terminal width.</param>
    /// <param name="height">Terminal height.</param>
    /// <returns>The frame, with terminal control sequences.</returns>
    public string Render(int width, int height)
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
        List<string> rows =
        [
            $@"{_product}  {AresVersion.Current}   |   uptime {DateTimeOffset.Now - _started:hh\:mm\:ss}",
            ActivityLine,
            new('-', columns),
            .. wrapped.Skip(start).Take(logRows),
        ];
        while (rows.Count < height - 3)
        {
            rows.Add("");
        }

        rows.Add(new string('-', columns));
        rows.Add(_scroll == 0 ? "LIVE | help | Tab completion | Up/Down history | Home/End logs | Ctrl+C stop" : $"PAUSED {_scroll} rows above live | Home/End logs");
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

    /// <summary>Escapes terminal controls and non-ASCII glyphs, so untrusted text cannot move the cursor and every character takes one column.</summary>
    /// <param name="text">Untrusted text.</param>
    /// <returns>Printable ASCII.</returns>
    public static string SafeText(string text)
    {
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

    /// <summary>The latest value of a status field from the event log.</summary>
    /// <param name="key">Event field.</param>
    /// <param name="fallback">Value before any event set it.</param>
    /// <returns>The value.</returns>
    protected string Status(string key, string fallback) => _status.GetValueOrDefault(key, fallback);

    /// <summary>The command line as echoed when it carries a secret (it is then also kept out of the history); null otherwise.</summary>
    /// <param name="line">The trimmed command line.</param>
    /// <returns>The masked line, or null.</returns>
    protected virtual string? Redact(string line) => null;

    private IReadOnlyList<ConsoleCommand> AllCommands => field ??=
    [
        new ConsoleCommand("help", ["help    Show commands"], (_, _, _) => ShowHelp(), MaximumWords: 1),
        new ConsoleCommand("status", [$"status  Show {_product[(_product.IndexOf('-', StringComparison.Ordinal) + 1)..]} configuration and latest activity"],
            (_, _, _) => ShowStatus(), MaximumWords: 1),
        new ConsoleCommand("clear", ["clear   Clear this console's log view"], (_, _, _) => Clear(), MaximumWords: 1),
        new ConsoleCommand("stop", [$"stop    Gracefully stop {_product}"], (_, _, _) => AddLine($"Stopping {_product}..."), MaximumWords: 1),
        .. Commands,
    ];

    private ConsoleCommand? Find(string name) => AllCommands.FirstOrDefault(command => command.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private void ShowHelp()
    {
        foreach (string line in AllCommands.SelectMany(command => command.Help).Concat(HelpNotes))
        {
            AddLine(line);
        }
    }

    private void ShowStatus()
    {
        AddLine($@"{_product} {AresVersion.Current} | Steam build {_build} | uptime {DateTimeOffset.Now - _started:hh\:mm\:ss}");
        foreach ((string key, string value) in _status)
        {
            AddLine($"{key}: {value}");
        }
    }

    private void Clear()
    {
        _lines.Clear();
        _scroll = 0;
        _dirty = true;
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

    private void CompleteInput(bool reverse)
    {
        string input = _input.ToString();
        bool cycling = _completionCandidates.Length > 0 && input == _completionInput && _cursor == _completionCursor;
        if (!cycling)
        {
            _completionStart = _cursor;
            while (_completionStart > 0 && input[_completionStart - 1] != ' ')
            {
                _completionStart--;
            }

            string[] preceding = input[.._completionStart].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<string> candidates = preceding.Length == 0 ? AllCommands.Select(command => command.Name)
                : Find(preceding[0])?.Complete?.Invoke(preceding) ?? [];
            string query = input[_completionStart.._cursor];
            _completionCandidates = [.. candidates.Where(name => name.Contains(query, StringComparison.OrdinalIgnoreCase)).Order(StringComparer.Ordinal)];
            if (_completionCandidates.Length == 0)
            {
                return;
            }

            int end = _cursor;
            while (end < input.Length && input[end] != ' ')
            {
                end++;
            }

            _completionLength = end - _completionStart;
            _completionIndex = -1;
            string common = _completionCandidates[0];
            foreach (string name in _completionCandidates.Skip(1))
            {
                int length = 0;
                while (length < common.Length && length < name.Length && common[length] == name[length])
                {
                    length++;
                }

                common = common[..length];
            }

            if (!reverse && common.StartsWith(query, StringComparison.OrdinalIgnoreCase) && common.Length > query.Length)
            {
                ApplyCompletion(common);
                return;
            }
        }

        _completionIndex = reverse ? (_completionIndex <= 0 ? _completionCandidates.Length - 1 : _completionIndex - 1)
            : (_completionIndex + 1) % _completionCandidates.Length;
        ApplyCompletion(_completionCandidates[_completionIndex]);
    }

    private void ApplyCompletion(string name)
    {
        if (_input.Length - _completionLength + name.Length > InputLimit)
        {
            return;
        }

        _input.Remove(_completionStart, _completionLength).Insert(_completionStart, name);
        _completionLength = name.Length;
        _cursor = _completionStart + name.Length;
        _completionInput = _input.ToString();
        _completionCursor = _cursor;
    }

    private void SetInput(string value)
    {
        _input.Clear().Append(value);
        _cursor = _input.Length;
    }

    private void WriteEvent(JsonObject value)
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

        bool error = value.Any(entry => entry.Value is not null && entry.Key.Contains("error", StringComparison.OrdinalIgnoreCase));
        bool warning = value["status"] is JsonValue status && status.TryGetValue(out int statusCode) && statusCode >= 400;
        string level = value["level"]?.ToString() ?? (error ? "ERROR" : warning ? "WARNING" : "INFO");
        string fields = string.Join(" | ", value.Where(entry => entry.Key != "level")
            .Select(entry => $"{entry.Key.Replace('_', ' ')}: {DisplayValue(entry.Value)}"));
        AddLine($"{DateTimeOffset.Now:HH:mm:ss} {level,-7} {SafeText(fields)}");
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

    private sealed class EventWriter(OperatorConsole owner, bool json) : TextWriter
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
}
