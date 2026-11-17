using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Raid.Tests.World;
using Ares.Raid.World.Loot;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Enemies;
using Ares.Structs.Game.Loot;

namespace Ares.Raid.Tests;

public sealed class RaidAdminTests
{
    [Theory]
    [InlineData("players")]
    [InlineData("PLAYERS")]
    [InlineData("teleport", "42", "1", "-2.5", "3e2")]
    [InlineData("teleport", "Alice", "1", "2", "3", "90")]
    [InlineData("teleport-to-loot", "42", "17")]
    [InlineData("teleport-to-loot", "42", "free-standing", "3")]
    [InlineData("spawn-loot", "42", "BP_SocketContainer_Rover_01")]
    [InlineData("spawn-arc", "wasp", "42")]
    [InlineData("spawn-arc", "WASP", "Alice")]
    [InlineData("despawn-arcs")]
    public void Valid_commands_are_read(string name, params string[] arguments)
    {
        (AdminRequest? request, IReadOnlyList<string> problem) = RaidAdmin.Parse(TestGameData.Loot, new AdminCommand(name, arguments));
        Assert.NotNull(request);
        Assert.Empty(problem);
        Assert.Equal(name.ToLowerInvariant(), request.Name);
    }

    [Theory]
    [InlineData("players", "42")]
    [InlineData("teleport", "42", "1", "2")]
    [InlineData("teleport", "42", "1", "2", "NaN")]
    [InlineData("teleport", "42", "1,5", "2", "3")]
    [InlineData("teleport-to-loot", "42", "-1")]
    [InlineData("teleport-to-loot", "42", "free", "1")]
    [InlineData("spawn-loot", "42", "NoSuchContainerClass")]
    [InlineData("spawn-arc", "42", "wasp")]
    [InlineData("spawn-arc", "wasp")]
    [InlineData("despawn-arcs", "42")]
    [InlineData("kill", "42")]
    public void Invalid_commands_explain_themselves(string name, params string[] arguments)
    {
        (AdminRequest? request, IReadOnlyList<string> problem) = RaidAdmin.Parse(TestGameData.Loot, new AdminCommand(name, arguments));
        Assert.Null(request);
        Assert.NotEmpty(problem);
    }

    [Fact]
    public void Arguments_keep_their_values()
    {
        AdminRequest teleport = RaidAdmin.Parse(TestGameData.Loot, new AdminCommand("teleport", ["42", "1", "-2.5", "3e2", "90"])).Request!;
        AdminRequest free = RaidAdmin.Parse(TestGameData.Loot, new AdminCommand("teleport-to-loot", ["Alice", "free-standing", "3"])).Request!;
        LootContainerClass rover = LootSocketClasses.ServedClasses(TestGameData.Loot)[0];

        Assert.Equal(("42", new Vector3D(1, -2.5, 300), 90.0), (teleport.Player, teleport.Position!.Value, teleport.YawDegrees!.Value));
        Assert.Equal(("Alice", 3, (int?)null), (free.Player, free.FreeStanding!.Value, free.Container));
        AdminRequest arc = RaidAdmin.Parse(TestGameData.Loot, new AdminCommand("spawn-arc", ["Wasp", "Alice"])).Request!;
        Assert.Equal(("Alice", Arcs.Wasp), (arc.Player, arc.Arc));
        Assert.Same(rover, Assert.Single(RaidAdmin.Classes(TestGameData.Loot, rover.Container.Class)));
        Assert.Same(rover, Assert.Single(RaidAdmin.Classes(TestGameData.Loot, rover.Container.Class[(rover.Container.Class.LastIndexOf('.') + 1)..])));
    }

    [Fact]
    public async Task Commands_run_on_the_game_loop_and_report_players()
    {
        ScriptedPlayer session = new();
        session.Possess();
        session.Input(30, 33, position: session.Member.Spawn.Location);
        RaidAdmin admin = new(TestGameData.Loot, () => [session.Channels]);

        AdminResult players = await Run(admin, "players");
        AdminResult unknown = await Run(admin, "teleport", "43", "0", "0", "0");
        AdminResult usage = await Run(admin, "teleport", "42");
        AdminResult teleport = await Run(admin, "teleport", "42", "1000", "2000", "300");
        AdminResult wasp = await Run(admin, "spawn-arc", "wasp", "42");
        AdminResult despawn = await Run(admin, "despawn-arcs");

        Assert.Equal(200, players.StatusCode);
        Assert.StartsWith("42 - Possessed at (", Assert.Single(players.Reply.Lines), StringComparison.Ordinal);
        Assert.Equal((404, AdminApi.PlayerNotFound), (unknown.StatusCode, unknown.Reply.Error));
        Assert.Equal((400, AdminApi.InvalidCommand), (usage.StatusCode, usage.Reply.Error));
        Assert.Equal(200, teleport.StatusCode);
        Assert.Equal(new Vector3D(1000, 2000, 300), session.Channels.CharacterPosition);
        Assert.Equal(200, wasp.StatusCode);
        Assert.StartsWith("Spawned ARC 0 (wasp) id 0x", Assert.Single(wasp.Reply.Lines), StringComparison.Ordinal);
        Assert.Equal((200, "Despawned 1 ARCs."), (despawn.StatusCode, Assert.Single(despawn.Reply.Lines)));
    }

    [Fact]
    public async Task A_command_the_game_loop_does_not_take_in_time_is_reported_busy()
    {
        RaidAdmin admin = new(TestGameData.Loot, () => [], timeout: TimeSpan.FromMilliseconds(10));
        AdminResult result = await admin.RunAsync(new AdminCommand("players", []), TestContext.Current.CancellationToken);
        admin.RunPending();
        Assert.Equal((503, AdminApi.Busy), (result.StatusCode, result.Reply.Error));
    }

    private static async Task<AdminResult> Run(RaidAdmin admin, string name, params string[] arguments)
    {
        Task<AdminResult> running = admin.RunAsync(new AdminCommand(name, arguments), TestContext.Current.CancellationToken);
        admin.RunPending();
        return await running;
    }
}
