using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;
using Microsoft.Extensions.Logging;

namespace Ares.Host.Tests;

public sealed class ConsoleTests
{
    [Theory]
    [InlineData("inv", "inventory")]
    [InlineData("INV", "inventory")]
    [InlineData("he", "help")]
    [InlineData("cl", "clear")]
    [InlineData("inventory a", "inventory add")]
    [InlineData("inventory im", "inventory import")]
    [InlineData("  INVENTORY   a", "  INVENTORY   add")]
    [InlineData("inventory import inv", "inventory import inv")]
    [InlineData("unknown inv", "unknown inv")]
    public void Completion_uses_the_command_context(string input, string expected)
    {
        using HostConsole console = new("plain", TextWriter.Null);
        Type(console, input);
        Key(console, ConsoleKey.Tab);
        Assert.Contains("> " + expected + "\e[0m", console.Render(120, 24), StringComparison.Ordinal);
    }

    [Fact]
    public void Command_completion_cycles_ambiguous_matches_and_preserves_the_remaining_arguments()
    {
        using HostConsole console = new("plain", TextWriter.Null);
        Type(console, "st");
        Key(console, ConsoleKey.Tab);
        Assert.Contains("> status\e[0m", console.Render(120, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Tab);
        Assert.Contains("> stop\e[0m", console.Render(120, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Tab, shift: true);
        Assert.Contains("> status\e[0m", console.Render(120, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Escape);
        Type(console, "invINVALID add augment");
        Key(console, ConsoleKey.Home, control: true);
        for (int i = 0; i < 3; i++)
        {
            Key(console, ConsoleKey.RightArrow);
        }

        Key(console, ConsoleKey.Tab);
        Assert.Contains("> inventory add augment\e[0m", console.Render(120, 24), StringComparison.Ordinal);
    }

    [Fact]
    public void Completion_can_build_a_command_then_subcommand_then_match_an_item()
    {
        using HostConsole console = new("plain", TextWriter.Null);
        Type(console, "inv");
        Key(console, ConsoleKey.Tab);
        Type(console, " ");
        Key(console, ConsoleKey.Tab);
        Type(console, " augment");
        Key(console, ConsoleKey.Tab);
        Assert.Contains("> inventory add " + ItemTechnicalNames.CombatMk1 + "\e[0m", console.Render(120, 24), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("augment")]
    [InlineData("AuGmEnT")]
    public void Partial_name_completion_cycles_all_matching_items_and_excludes_unrelated_items(string query)
    {
        using HostConsole console = new("plain", TextWriter.Null);
        Type(console, "inventory add " + query);
        string[] matches = [ItemTechnicalNames.CombatMk1, ItemTechnicalNames.LootingMk1, ItemTechnicalNames.TacticalMk1, ItemTechnicalNames.CombatMk1];
        foreach (string name in matches)
        {
            Key(console, ConsoleKey.Tab);
            Assert.Contains("> inventory add " + name, console.Render(120, 24), StringComparison.Ordinal);
        }

        Key(console, ConsoleKey.Tab, shift: true);
        Assert.Contains("> inventory add " + ItemTechnicalNames.TacticalMk1, console.Render(120, 24), StringComparison.Ordinal);
    }

    [Fact]
    public void Item_name_completion_extends_the_common_prefix_then_cycles_in_both_directions()
    {
        using HostConsole console = new("plain", TextWriter.Null);
        Type(console, "inventory add da_item_aug");
        Key(console, ConsoleKey.Tab);
        Assert.Contains("> inventory add DA_Item_Augment_", console.Render(120, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Tab);
        Assert.Contains("> inventory add " + ItemTechnicalNames.CombatMk1, console.Render(120, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Tab);
        Assert.Contains("> inventory add " + ItemTechnicalNames.LootingMk1, console.Render(120, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Tab, shift: true);
        Assert.Contains("> inventory add " + ItemTechnicalNames.CombatMk1, console.Render(120, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Tab, shift: true);
        Assert.Contains("> inventory add " + ItemTechnicalNames.TacticalMk1, console.Render(120, 24), StringComparison.Ordinal);
    }

    [Fact]
    public void Completion_replaces_the_whole_name_at_the_cursor_and_resets_after_editing()
    {
        using HostConsole console = new("plain", TextWriter.Null);
        Type(console, "inventory add da_item_augment_combINVALID suffix");
        Key(console, ConsoleKey.Home, control: true);
        for (int i = 0; i < "inventory add da_item_augment_comb".Length; i++)
        {
            Key(console, ConsoleKey.RightArrow);
        }

        Key(console, ConsoleKey.Tab);
        Assert.Contains("> inventory add " + ItemTechnicalNames.CombatMk1 + " suffix", console.Render(120, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Escape);
        Type(console, "inventory add no_match");
        Key(console, ConsoleKey.Tab);
        Assert.Contains("> inventory add no_match", console.Render(120, 24), StringComparison.Ordinal);
    }

    [Fact]
    public void Completing_an_item_name_does_not_add_it_until_the_command_is_submitted()
    {
        using TestDatabase database = new();
        InventoryStore inventory = new(database.Store);
        inventory.InitializeLayout(DateTimeOffset.UnixEpoch);
        using HostConsole console = new("plain", TextWriter.Null);
        console.Inventory = inventory;
        Type(console, "inventory add combat");
        Key(console, ConsoleKey.Tab);
        Assert.Single(inventory.Inventory.Stash.Items);
        Key(console, ConsoleKey.Enter);
        Assert.Contains(new InventoryStore(database.Store).Inventory.Stash.Items, item => item.Type == ItemTechnicalNames.CombatMk1);
    }

    [Fact]
    public void Inventory_import_reads_the_operator_file_and_survives_a_restart()
    {
        using TestDatabase database = new();
        string directory = database.Directory;
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "Empty Inventory.json");
            File.WriteAllText(path, JsonSerializer.Serialize(PlayerInventory.Empty, InventoryStore.SerializationOptions));
            using StringWriter output = new();
            using HostConsole console = new("plain", output);
            console.Inventory = new InventoryStore(database.Store);
            console.ExecuteCommand($"inventory import \"{path}\"", () => { });
            console.ExecuteCommand("inventory", () => { });

            Assert.Equal(1, new InventoryStore(database.Store).Snapshot.Revision);
            Assert.Contains("Imported 0 items at revision 1", output.ToString(), StringComparison.Ordinal);
            Assert.Contains("Reserved ticket: none", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Readable_logs_keep_event_fields_and_escape_terminal_controls()
    {
        using StringWriter output = new();
        using HostConsole console = new("plain", output);
        console.Log.Write(new JsonObject { ["player_phase"] = "Ready", ["detail"] = "bad\e[2J\nline" });

        string text = output.ToString();
        Assert.Contains("INFO", text, StringComparison.Ordinal);
        Assert.Contains("player phase: Ready", text, StringComparison.Ordinal);
        Assert.Contains(@"detail: bad\u001b[2J\u000aline", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\e', text);
    }

    [Fact]
    public void Json_mode_preserves_machine_readable_events()
    {
        using StringWriter output = new();
        using HostConsole console = new("json", output);
        console.Log.Write(new JsonObject { ["player_phase"] = "Ready", ["count"] = 2 });

        JsonObject value = JsonNode.Parse(output.ToString())!.AsObject();
        Assert.Equal("Ready", value["player_phase"]!.GetValue<string>());
        Assert.Equal(2, value["count"]!.GetValue<int>());
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("json")]
    public void File_logs_append_and_flush_json_independently_of_console_output(string mode)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ares-console-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "nested", "server.log");
        try
        {
            using (HostConsole first = new(mode, TextWriter.Null, path))
            {
                first.Log.Write("run", 1);
            }

            using StringWriter output = new();
            using HostConsole console = new(mode, output, path);
            console.Log.Write("run", 2);
            console.CreateLogger("Test").LogWarning("persisted warning");
            console.ExecuteCommand("help", () => { });

            using FileStream file = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(file);
            string[] lines = reader.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(3, lines.Length);
            Assert.Equal(1, JsonNode.Parse(lines[0])!["run"]!.GetValue<int>());
            Assert.Equal(2, JsonNode.Parse(lines[1])!["run"]!.GetValue<int>());
            Assert.Equal("persisted warning", JsonNode.Parse(lines[2])!["message"]!.GetValue<string>());
            Assert.DoesNotContain('\e', string.Join("", lines));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Framework_warnings_use_the_same_log_and_low_levels_are_ignored()
    {
        using StringWriter output = new();
        using HostConsole console = new("plain", output);
        ILogger logger = console.CreateLogger("Test");
        logger.LogInformation("not displayed");
        logger.LogWarning("visible warning");

        Assert.Contains("WARNING", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("visible warning", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("not displayed", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Logs_do_not_overwrite_a_partially_edited_command()
    {
        using StringWriter output = new();
        using HostConsole console = new("plain", output);
        Type(console, "sttus");
        Key(console, ConsoleKey.Home, control: true);
        Key(console, ConsoleKey.RightArrow);
        Key(console, ConsoleKey.RightArrow);
        Type(console, "a");
        for (int index = 0; index < 1100; index++)
        {
            console.Log.Write("tick", index);
        }

        string frame = console.Render(80, 24);
        Assert.Contains("> status", frame, StringComparison.Ordinal);
        Assert.Contains("tick: 1099", frame, StringComparison.Ordinal);
        Assert.Contains("\e[24;6H", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void History_preserves_the_unsubmitted_draft_and_escape_clears_it()
    {
        using StringWriter output = new();
        using HostConsole console = new("plain", output);
        Type(console, "help");
        Key(console, ConsoleKey.Enter);
        Type(console, "draft");
        Key(console, ConsoleKey.UpArrow);
        Assert.Contains("> help", console.Render(80, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.DownArrow);
        Assert.Contains("> draft", console.Render(80, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Escape);
        Assert.DoesNotContain("draft", console.Render(80, 24), StringComparison.Ordinal);
    }

    [Fact]
    public void Home_and_end_page_logs_while_preserving_command_input()
    {
        using StringWriter output = new();
        using HostConsole console = new("plain", output);
        for (int index = 0; index < 50; index++)
        {
            console.Log.Write("tick", index);
        }

        Type(console, "draft");
        Key(console, ConsoleKey.Home);
        string paused = console.Render(80, 24);
        Assert.Contains("PAUSED", paused, StringComparison.Ordinal);
        Assert.DoesNotContain("tick: 49", paused, StringComparison.Ordinal);
        Key(console, ConsoleKey.Home);
        console.Render(80, 24);
        console.Log.Write("tick", 50);
        Assert.DoesNotContain("tick: 50", console.Render(80, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.End);
        Assert.Contains("PAUSED", console.Render(80, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.End);
        Assert.Contains("tick: 50", console.Render(80, 24), StringComparison.Ordinal);
        Type(console, "x");
        Assert.Contains("> draftx", console.Render(80, 24), StringComparison.Ordinal);
        Key(console, ConsoleKey.Home, control: true);
        Type(console, "y");
        Key(console, ConsoleKey.End, control: true);
        Type(console, "z");
        Assert.Contains("> ydraftxz", console.Render(80, 24), StringComparison.Ordinal);
    }

    [Fact]
    public void Commands_report_status_reject_unknown_input_and_stop_only_on_request()
    {
        using StringWriter output = new();
        using HostConsole console = new("plain", output);
        bool stopped = false;
        console.Log.Write("listening", "127.0.0.1:1234");
        console.ExecuteCommand("status", () => stopped = true);
        console.ExecuteCommand("unsupported", () => stopped = true);
        Assert.False(stopped);
        Assert.Contains("listening: 127.0.0.1:1234", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Unknown command", output.ToString(), StringComparison.Ordinal);
        console.ExecuteCommand(" stop ", () => stopped = true);
        Assert.True(stopped);
        console.ExecuteCommand("clear", () => { });
        Assert.DoesNotContain("unsupported", console.Render(80, 24), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 3)]
    [InlineData(40, 12)]
    [InlineData(120, 40)]
    public void Long_commands_and_logs_fit_after_a_resize(int width, int height)
    {
        using StringWriter output = new();
        using HostConsole console = new("plain", output);
        Type(console, new string('x', 600));
        console.Log.Write("detail", new string('y', 500));
        string frame = console.Render(width, height);
        Assert.Contains($"\e[{height};", frame, StringComparison.Ordinal);
        Assert.Equal(height - 1, frame.Split("\r\n").Length - 1);
    }

    [Fact]
    public async Task Concurrent_events_remain_complete_and_input_remains_editable()
    {
        await using StringWriter output = new();
        using HostConsole console = new("plain", output);
        Task[] workers = new Task[4];
        for (int worker = 0; worker < workers.Length; worker++)
        {
            workers[worker] = WriteEvents(console, worker);
        }

        await Task.WhenAll(workers);
        Type(console, "help");
        Assert.Equal(400, output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("> help", console.Render(80, 24), StringComparison.Ordinal);
    }

    private static Task WriteEvents(HostConsole console, int worker)
    {
        return Task.Run(() =>
        {
            for (int index = 0; index < 100; index++)
            {
                console.Log.Write(new JsonObject { ["worker"] = worker, ["index"] = index });
            }
        });
    }

    private static void Type(HostConsole console, string text)
    {
        foreach (char character in text)
        {
            console.HandleKey(new ConsoleKeyInfo(character, ConsoleKey.NoName, false, false, false), () => { }, 24);
        }
    }

    private static void Key(HostConsole console, ConsoleKey key, bool control = false, bool shift = false)
    {
        console.HandleKey(new ConsoleKeyInfo('\0', key, shift, false, control), () => { }, 24);
    }
}
