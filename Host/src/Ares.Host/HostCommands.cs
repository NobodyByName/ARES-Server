using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common.Control;
using Ares.Common.Logging;
using Ares.Common.Terminal;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Matchmaking;
using Ares.Host.Services.Persistence;
using Ares.Host.Services.Players;
using Ares.Structs.Game.Enemies;

namespace Ares.Host;

/// <summary>The services the Host console commands act on, available once Host is built.</summary>
/// <param name="Accounts">Account directory.</param>
/// <param name="Players">Player registry.</param>
/// <param name="Raids">Running raids; null when matchmaking starts none.</param>
/// <param name="MenuInventorySync">Whether clients at the main menu fetch their inventory again every 15 seconds.</param>
internal sealed record HostServices(AccountDirectory Accounts, PlayerRegistry Players, RaidFleet? Raids, bool MenuInventorySync = false);

/// <summary>The Host console's commands: account, inventory and raid administration.</summary>
/// <param name="log">Event log, for raid replies that arrive later.</param>
internal sealed class HostCommands(JsonEventLog log)
{
    private const string ListRaids = "list";
    private static readonly string[] s_accountCommands = ["list", "create", "password", "rename", "ban", "unban", "delete"];
    private static readonly string[] s_inventoryCommands = ["add", "import"];

    /// <summary>The services, once attached; commands that need them say so until then.</summary>
    internal HostServices? Services { get; set; }

    /// <summary>The commands, in help order.</summary>
    internal IReadOnlyList<ConsoleCommand> All =>
    [
        new("account",
        [
            "account list  List accounts",
            "account create <name> <password>  Create an account",
            "account password <name> <password>  Set a new password",
            "account rename <name> <new-name>  Rename an account (also its in-game name)",
            "account ban <name> / account unban <name>  Block or allow sign-in",
            "account delete <name>  Delete an account and all its player data",
        ], (words, _, reply) => RunGuarded("Account command", reply, services => Account(services, words, reply)),
            Complete: preceding => preceding.Length == 1 ? s_accountCommands : []),
        new("inventory",
        [
            "inventory <account>  Show stored items and the loadout reservation",
            "inventory <account> import <path>  Replace an idle inventory from a JSON file",
            "inventory <account> add <technical-name> [quantity]  Add one supported stack to the stash (currencies: to the wallet)",
        ], (words, line, reply) => RunGuarded("Inventory command", reply, services => Inventory(services, words, line, reply)), MinimumWords: 2,
            Complete: preceding => preceding switch
            {
                [_, _] => s_inventoryCommands,
                [_, _, var subcommand] when subcommand.Equals("add", StringComparison.OrdinalIgnoreCase) => Services?.Players.Catalog.ItemNames ?? [],
                _ => [],
            }),
        new("raid",
        [
            $"raid {ListRaids}  List the running raids: id and players",
            .. AdminApi.Usage.Select(usage => $"raid <raid-id> {usage}"),
        ], Raid, MinimumWords: 2, Complete: RaidCompletions),
    ];

    /// <summary>Help lines shown after the commands.</summary>
    internal static IReadOnlyList<string> Notes { get; } =
    [
        "A raid id is the id the game shows in its bottom-right corner; its first characters are enough",
        "Tab completes commands, subcommands, raid ids, players and item names; Shift+Tab cycles backward",
    ];

    /// <summary>The command line with its password masked, or null when it carries none.</summary>
    /// <param name="line">The trimmed command line.</param>
    /// <returns>The masked line, or null.</returns>
    internal static string? Redact(string line)
    {
        string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool secret = words.Length >= 3 && words[0].Equals("account", StringComparison.OrdinalIgnoreCase)
            && (words[1].Equals("create", StringComparison.OrdinalIgnoreCase) || words[1].Equals("password", StringComparison.OrdinalIgnoreCase));
        return secret ? string.Join(' ', words.Take(3)) + (words.Length > 3 ? " ********" : "") : null;
    }

    // Lists the running raids, or sends an admin command to one raid; its reply arrives as an event (and in the log file)
    // when it answers.
    private void Raid(string[] words, string line, Action<string> reply)
    {
        if (Services?.Raids is not { } raids)
        {
            reply("No raids: matchmaking is not configured.");
            return;
        }

        if (words is [_, var list] && list.Equals(ListRaids, StringComparison.OrdinalIgnoreCase))
        {
            IReadOnlyList<RunningRaid> running = raids.Running();
            if (running.Count == 0)
            {
                reply("No raid is running.");
            }

            foreach (RunningRaid raid in running)
            {
                string players = string.Join(", ", raid.Players.Select(PlayerName));
                reply($"{raid.MatchId ?? "(starting)"}  slot {raid.Slot}  players: {(players.Length == 0 ? "-" : OperatorConsole.SafeText(players))}");
            }

            return;
        }

        if (words.Length < 3 || !IsRaidId(words[1]))
        {
            reply($"Usage: raid {ListRaids} | raid <raid-id> <command> [arguments]. Type help for the commands.");
            return;
        }

        string id = words[1];
        AdminCommand command = new(words[2].ToLowerInvariant(), words[3..]);
        string text = string.Join(' ', words[1..]);
        _ = Task.Run(async () =>
        {
            (string? MatchId, int StatusCode, AdminReply Reply) answer;
            try
            {
                answer = await raids.AdminAsync(id, command, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            log.Write(new JsonObject
            {
                ["admin_command"] = text,
                ["raid"] = answer.MatchId,
                ["status"] = answer.StatusCode,
                ["error"] = answer.Reply.Error,
                ["result"] = string.Join(" | ", answer.Reply.Lines),
            });
        });
    }

    // A raid id or the start of one: the hexadecimal digits and dashes of a match id.
    private static bool IsRaidId(string word) => word.All(character => char.IsAsciiHexDigit(character) || character == '-');

    // A player as the operator names them: the account name, else the user id.
    private string PlayerName(long userId) => Services?.Accounts.Find(userId)?.Name ?? userId.ToString(CultureInfo.InvariantCulture);

    // Completions after "raid": list or a raid id, then a command, then the command's own arguments (ARC names, then the
    // names of the chosen raid's players where the command takes a player).
    private IEnumerable<string> RaidCompletions(string[] preceding)
    {
        IReadOnlyList<RunningRaid> running = Services?.Raids?.Running() ?? [];
        if (preceding.Length == 1)
        {
            return [ListRaids, .. running.Select(raid => raid.MatchId).OfType<string>()];
        }

        if (!IsRaidId(preceding[1]))
        {
            return [];
        }

        if (preceding.Length == 2)
        {
            return AdminApi.Commands;
        }

        string command = preceding[2].ToLowerInvariant();
        int argument = preceding.Length - 3;
        if (command == AdminApi.SpawnArc && argument == 0)
        {
            return Arcs.All.Select(arc => arc.Name);
        }

        return AdminApi.PlayerArgument.TryGetValue(command, out int player) && player == argument
            && running.Where(raid => raid.MatchId?.StartsWith(preceding[1], StringComparison.OrdinalIgnoreCase) == true).ToArray() is [{ } chosen]
            ? chosen.Players.Select(PlayerName)
            : [];
    }

    private void RunGuarded(string what, Action<string> reply, Action<HostServices> command)
    {
        if (Services is not { } services)
        {
            reply("Accounts are unavailable.");
            return;
        }

        try
        {
            command(services);
        }
        catch (Exception error) when (error is PersistenceException or InvalidDataException)
        {
            reply($"{what} failed: {OperatorConsole.SafeText(error.Message)}");
        }
    }

    private static void Account(HostServices services, string[] words, Action<string> reply)
    {
        AccountDirectory accounts = services.Accounts;
        switch (words.Length > 1 ? words[1].ToLowerInvariant() : "")
        {
            case "list" when words.Length == 2:
                IReadOnlyList<Account> all = accounts.List();
                foreach (Account account in all)
                {
                    reply($"{account.Name}  id {account.UserId}{(account.Record.Banned ? "  banned" : "")}");
                }

                reply($"{all.Count} account(s).");
                break;
            case "create" when words.Length == 4:
                reply(accounts.Create(words[2], words[3]) switch
                {
                    AccountCreateResult.Created => $"Account {words[2]} created.",
                    AccountCreateResult.InvalidName => "Account names are 3-32 letters, digits, '_', '-' or '.'.",
                    AccountCreateResult.InvalidPassword => $"Passwords are {AccountDirectory.MinimumPasswordLength}-{AccountDirectory.MaximumPasswordLength} characters without spaces.",
                    AccountCreateResult.NameTaken => $"An account named {words[2]} already exists.",
                    _ => "Account not created.",
                });
                break;
            case "password" when words.Length == 4:
                reply(accounts.SetPassword(words[2], words[3])
                    ? $"Password of {words[2]} changed; its launch key was revoked."
                    : $"No account {words[2]}, or the password is not {AccountDirectory.MinimumPasswordLength}-{AccountDirectory.MaximumPasswordLength} characters.");
                break;
            case "rename" when words.Length == 4:
                (AccountRenameResult result, Account? renamed) = accounts.Rename(words[2], words[3]);
                if (renamed is not null)
                {
                    services.Players.Update(renamed);
                }

                reply(result switch
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
                    services.Players.Update(changed);
                    reply(banned ? $"Account {changed.Name} banned." : $"Account {changed.Name} unbanned.");
                }
                else
                {
                    reply($"No account {words[2]}.");
                }

                break;
            case "delete" when words.Length == 3:
                if (accounts.Delete(words[2]) is { } deleted)
                {
                    services.Players.Forget(deleted.UserId);
                    reply($"Account {deleted.Name} and its player data deleted.");
                }
                else
                {
                    reply($"No account {words[2]}.");
                }

                break;
            default:
                reply($"Use account {string.Join('|', s_accountCommands)}. Type help for details.");
                break;
        }
    }

    private static void Inventory(HostServices services, string[] words, string line, Action<string> reply)
    {
        if (services.Accounts.Find(words[1]) is not { } account || services.Players.Find(account.UserId) is not { } player)
        {
            reply($"No account {words[1]}.");
            return;
        }

        InventoryStore inventory = player.Inventory;
        switch (words.Length > 2 ? words[2].ToLowerInvariant() : "")
        {
            case "":
                PlayerInventory state = inventory.Inventory;
                InventoryCapacities capacities = inventory.Catalog.Capacities(state);
                reply($"Inventory revision {state.Revision}: {state.ItemCount} items, stash capacity {capacities.Stash}, backpack capacity {capacities.Backpack}");
                reply($"QuickUse capacity {capacities.QuickUse}, Safe Pocket capacity {capacities.SafePockets}");
                reply($"Augment: {state.Loadout.Augment?.Type ?? "none"}");
                reply($"Reserved ticket: {inventory.ReservedTicketId ?? "none"}");
                break;
            case "import" when words.Length >= 4:
                ImportInventory(inventory, Remainder(line, 3).Trim('"'), reply);
                break;
            case "add":
                AddInventoryItem(inventory, words[3..], services.MenuInventorySync, reply);
                break;
            default:
                reply($"Use inventory <account> [{string.Join('|', s_inventoryCommands)}]. Type help for details.");
                break;
        }
    }

    private static void ImportInventory(InventoryStore inventory, string path, Action<string> reply)
    {
        try
        {
            inventory.ImportFile(path, inventory.Inventory.Revision);
            PlayerInventory state = inventory.Inventory;
            reply($"Imported {state.ItemCount} items at revision {state.Revision}. Start a fresh client to refresh capacity changes.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or OverflowException)
        {
            reply($"Inventory import rejected: {OperatorConsole.SafeText(error.Message)}");
        }
    }

    private static void AddInventoryItem(InventoryStore inventory, string[] parts, bool menuInventorySync, Action<string> reply)
    {
        try
        {
            long quantity = 1;
            if (parts.Length is < 1 or > 2 || parts.Length == 2 && !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out quantity))
            {
                throw new InvalidDataException("Use inventory <account> add <technical-name> [quantity]");
            }

            inventory.AddItem(parts[0], DateTimeOffset.UtcNow, quantity);
            reply(menuInventorySync
                ? $"Added {quantity} x {parts[0]}. Clients at the main menu show it within 15 seconds."
                : $"Added {quantity} x {parts[0]}. Start a fresh client to refresh inventory.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException or OverflowException)
        {
            reply($"Inventory add rejected: {OperatorConsole.SafeText(error.Message)}");
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
}
