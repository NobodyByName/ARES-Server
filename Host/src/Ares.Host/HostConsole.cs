using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common;
using Ares.Common.Logging;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Persistence;
using Ares.Host.Services.Players;
using Ares.Structs.Backend.Progression;
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
    private string[] _completionCandidates = [];
    private string _completionInput = "";
    private int _completionCursor;
    private int _completionStart;
    private int _completionLength;
    private int _completionIndex = -1;
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

    internal AccountDirectory? Accounts { get; set; }

    internal PlayerRegistry? Players { get; set; }

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

            AddLine($"> {SafeText(HasPassword(name) ? MaskPassword(name) : name)}");
            string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            switch (words[0].ToLowerInvariant())
            {
                case "help" when words.Length == 1:
                    AddLine("help    Show commands");
                    AddLine("status  Show Host configuration and latest activity");
                    AddLine("clear   Clear this console's log view");
                    AddLine("stop    Gracefully stop ARES-Host");
                    AddLine("account list  List accounts");
                    AddLine("account create <name> <password>  Create an account");
                    AddLine("account password <name> <password>  Set a new password");
                    AddLine("account rename <name> <new-name>  Rename an account (also its in-game name)");
                    AddLine("account ban <name> / account unban <name>  Block or allow sign-in");
                    AddLine("account delete <name>  Delete an account and all its player data");
                    AddLine("inventory <account>  Show stored items and the loadout reservation");
                    AddLine("inventory <account> import <path>  Replace an idle inventory from a JSON file");
                    AddLine("inventory <account> add <technical-name> [quantity]  Add one supported stack to the stash");
                    AddLine("Tab completes commands, subcommands and item names; Shift+Tab cycles backward");
                    break;
                case "status" when words.Length == 1:
                    AddLine($@"ARES-Host {AresVersion.Current} | Steam build {GameBuild.SteamBuildId} | uptime {DateTimeOffset.Now - _started:hh\:mm\:ss}");
                    foreach ((string key, string value) in _status)
                    {
                        AddLine($"{key}: {value}");
                    }

                    break;
                case "clear" when words.Length == 1:
                    _lines.Clear();
                    _scroll = 0;
                    _dirty = true;
                    break;
                case "stop" when words.Length == 1:
                    AddLine("Stopping ARES-Host...");
                    stop();
                    break;
                case "account":
                    RunGuarded("Account command", () => AccountCommand(words));
                    break;
                case "inventory" when words.Length >= 2:
                    RunGuarded("Inventory command", () => InventoryCommand(words, name));
                    break;
                default:
                    AddLine("Unknown command. Type help for commands.");
                    break;
            }
        }
    }

    private void RunGuarded(string what, Action command)
    {
        if (Accounts is null || Players is null)
        {
            AddLine("Accounts are unavailable.");
            return;
        }

        try
        {
            command();
        }
        catch (Exception error) when (error is PersistenceException or InvalidDataException)
        {
            AddLine($"{what} failed: {SafeText(error.Message)}");
        }
    }

    private void AccountCommand(string[] words)
    {
        AccountDirectory accounts = Accounts!;
        switch (words.Length > 1 ? words[1].ToLowerInvariant() : "")
        {
            case "list" when words.Length == 2:
                IReadOnlyList<Account> all = accounts.List();
                foreach (Account account in all)
                {
                    AddLine($"{account.Name}  id {account.UserId}{(account.Record.Banned ? "  banned" : "")}");
                }

                AddLine($"{all.Count} account(s).");
                break;
            case "create" when words.Length == 4:
                AddLine(accounts.Create(words[2], words[3]) switch
                {
                    AccountCreateResult.Created => $"Account {words[2]} created.",
                    AccountCreateResult.InvalidName => "Account names are 3-32 letters, digits, '_', '-' or '.'.",
                    AccountCreateResult.InvalidPassword => $"Passwords are {AccountDirectory.MinimumPasswordLength}-{AccountDirectory.MaximumPasswordLength} characters without spaces.",
                    AccountCreateResult.NameTaken => $"An account named {words[2]} already exists.",
                    _ => "Account not created.",
                });
                break;
            case "password" when words.Length == 4:
                AddLine(accounts.SetPassword(words[2], words[3])
                    ? $"Password of {words[2]} changed; its launch key was revoked."
                    : $"No account {words[2]}, or the password is not {AccountDirectory.MinimumPasswordLength}-{AccountDirectory.MaximumPasswordLength} characters.");
                break;
            case "rename" when words.Length == 4:
                (AccountRenameResult result, Account? renamed) = accounts.Rename(words[2], words[3]);
                if (renamed is not null)
                {
                    Players!.Update(renamed);
                }

                AddLine(result switch
                {
                    AccountRenameResult.Renamed => $"Account {words[2]} renamed to {renamed!.Name}. Players see the new name after restarting the game.",
                    AccountRenameResult.NotFound => $"No account {words[2]}.",
                    AccountRenameResult.InvalidName => "Account names are 3-32 letters, digits, '_', '-' or '.'.",
                    _ => $"An account named {words[3]} already exists.",
                });
                break;
            case "ban" or "unban" when words.Length == 3:
                bool banned = words[1].Equals("ban", StringComparison.OrdinalIgnoreCase);
                if (accounts.SetBanned(words[2], banned) is { } changed)
                {
                    Players!.Update(changed);
                    AddLine(banned ? $"Account {changed.Name} banned." : $"Account {changed.Name} unbanned.");
                }
                else
                {
                    AddLine($"No account {words[2]}.");
                }

                break;
            case "delete" when words.Length == 3:
                if (accounts.Delete(words[2]) is { } deleted)
                {
                    Players!.Forget(deleted.UserId);
                    AddLine($"Account {deleted.Name} and its player data deleted.");
                }
                else
                {
                    AddLine($"No account {words[2]}.");
                }

                break;
            default:
                AddLine("Use account list|create|password|rename|ban|unban|delete. Type help for details.");
                break;
        }
    }

    private void InventoryCommand(string[] words, string line)
    {
        if (Accounts!.Find(words[1]) is not { } account || Players!.Find(account.UserId) is not { } player)
        {
            AddLine($"No account {words[1]}.");
            return;
        }

        InventoryStore inventory = player.Inventory;
        switch (words.Length > 2 ? words[2].ToLowerInvariant() : "")
        {
            case "":
                PlayerInventory state = inventory.Inventory;
                AddLine($"Inventory revision {state.Revision}: {state.ItemCount} items, stash capacity {state.Stash.SlotCount}, backpack capacity {state.Loadout.BackpackCapacity}");
                AddLine($"QuickUse capacity {state.Loadout.QuickUseCapacity}, Safe Pocket capacity {state.Loadout.SafePocketCapacity}");
                AddLine($"Augment: {state.Loadout.Augment?.Type ?? "none"}");
                AddLine($"Reserved ticket: {inventory.ReservedTicketId ?? "none"}");
                break;
            case "import" when words.Length >= 4:
                ImportInventory(inventory, Remainder(line, 3).Trim('"'));
                break;
            case "add":
                AddInventoryItem(inventory, words[3..]);
                break;
            default:
                AddLine("Use inventory <account> [add|import]. Type help for details.");
                break;
        }
    }

    private void ImportInventory(InventoryStore inventory, string path)
    {
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

    private void AddInventoryItem(InventoryStore inventory, string[] parts)
    {
        try
        {
            long quantity = 1;
            if (parts.Length is < 1 or > 2 || parts.Length == 2 && !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out quantity))
            {
                throw new InvalidDataException("Use inventory <account> add <technical-name> [quantity]");
            }

            inventory.AddItem(parts[0], DateTimeOffset.UtcNow, quantity);
            AddLine($"Added {quantity} x {parts[0]} to the stash. Start a fresh client to refresh inventory.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException or OverflowException)
        {
            AddLine($"Inventory add rejected: {SafeText(error.Message)}");
        }
    }

    // The text after the first <paramref name="words"/> words.
    private static string Remainder(string line, int words)
    {
        string rest = line.Trim();
        for (int word = 0; word < words; word++)
        {
            int space = rest.IndexOf(' ');
            rest = space < 0 ? "" : rest[(space + 1)..].TrimStart();
        }

        return rest;
    }

    // Commands that carry a password: shown masked and kept out of the history.
    private static bool HasPassword(string command)
    {
        string[] words = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length >= 3 && words[0].Equals("account", StringComparison.OrdinalIgnoreCase)
            && (words[1].Equals("create", StringComparison.OrdinalIgnoreCase) || words[1].Equals("password", StringComparison.OrdinalIgnoreCase));
    }

    private static string MaskPassword(string command)
    {
        string[] words = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Take(3)) + (words.Length > 3 ? " ********" : "");
    }

    internal void HandleKey(ConsoleKeyInfo key, Action stop, int height)
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
                    if (!HasPassword(command) && (_history.Count == 0 || _history[^1] != command))
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
            IEnumerable<string> candidates = preceding switch
            {
                [] => ["help", "status", "clear", "stop", "account", "inventory"],
                [var command] when command.Equals("account", StringComparison.OrdinalIgnoreCase) => ["list", "create", "password", "rename", "ban", "unban", "delete"],
                [var command, _] when command.Equals("inventory", StringComparison.OrdinalIgnoreCase) => ["add", "import"],
                [var command, _, var subcommand] when command.Equals("inventory", StringComparison.OrdinalIgnoreCase)
                    && subcommand.Equals("add", StringComparison.OrdinalIgnoreCase) => ItemTechnicalNames.AssetIds.Keys,
                _ => [],
            };
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
