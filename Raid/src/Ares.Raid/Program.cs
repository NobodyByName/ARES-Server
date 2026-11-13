using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using Ares.Common;
using Ares.Common.CommandLine;
using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Logging;
using Ares.Common.Maps;
using Ares.Raid;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Raid.World.Collision;
using Ares.Structs.Core;
using Ares.Structs.Game.Network;
using Ares.Structs.Game.World;

RaidConfiguration configuration;
try
{
    configuration = RaidConfiguration.Parse(args);
}
catch (HelpRequestedException)
{
    Console.WriteLine(RaidConfiguration.Usage);
    return 0;
}
catch (VersionRequestedException)
{
    Console.WriteLine($"ARES-Raid {AresVersion.Current} (Steam build {GameBuild.SteamBuildId})");
    return 0;
}
catch (CommandLineException error)
{
    Console.Error.WriteLine(RaidConfiguration.Usage);
    Console.Error.WriteLine($"ARES-Raid: error: {error.Message}");
    return 2;
}

using RaidConsole console = new(configuration.ConsoleMode, logFile: configuration.LogFile);
JsonEventLog log = console.Log;
long started = Stopwatch.GetTimestamp();

using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
socket.Bind(new IPEndPoint(configuration.Bind, configuration.Port));
int port = ((IPEndPoint)socket.LocalEndPoint!).Port;

MapCollision? collision = MapCollisionLoader.Load(configuration.MapDataDirectory, DamMap.WorldName, out string? collisionProblem);
log.Write(collision is null
    ? new JsonObject { ["map_collision"] = "unavailable", ["warning"] = collisionProblem }
    : new JsonObject { ["map_collision"] = "loaded", ["placements"] = collision.Placements.Length, ["height_fields"] = collision.HeightFields.Length });
DamWorlds worlds = new(Clock, configuration.Lighting, collision: collision);
RaidAllocations allocations = new(new GameServerEndpoint("ares-raid-dam", configuration.AdvertiseHost, port, "", "", false, "LOCAL"), worlds, Clock,
    configuration.MaxPlayers, fillWindow: configuration.FillSeconds, log: log);
HandshakeServer handshake = new(GameBuild.NetworkChecksum, 0, clock: Clock);
RaidConnections connections = new(handshake, new RoutingAuthenticator(allocations), worlds, packet => UdpPadding.Remove(packet), Clock);

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [] });
builder.Logging.ClearProviders();
builder.Logging.AddProvider(console);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Listen(IPAddress.Loopback, configuration.ControlPort);
});
await using WebApplication app = builder.Build();
app.MapAllocationApi(allocations, configuration.OperatorToken);
RaidAdmin admin = new(() => connections.Players.Select(entry => entry.Player).OfType<PlayerChannels>(), log);
app.MapAdminApi(admin, configuration.OperatorToken);
await app.StartAsync().ConfigureAwait(false);

log.Write(new JsonObject
{
    ["service"] = "ARES-Raid",
    ["version"] = AresVersion.Current,
    ["listening"] = $"{configuration.Bind}:{port}",
    ["advertised"] = $"{configuration.AdvertiseHost}:{port}",
    ["allocation_api"] = $"127.0.0.1:{configuration.ControlPort}",
    ["network_version"] = GameBuild.NetworkChecksum,
    ["lighting"] = configuration.Lighting ?? "weighted",
    ["max_players"] = configuration.MaxPlayers,
    ["fill_seconds"] = configuration.FillSeconds,
});

RaidServer server = new(socket, connections, log, Clock, admin);
CancellationToken stopping = app.Lifetime.ApplicationStopping;
using CancellationTokenSource consoleStopping = CancellationTokenSource.CreateLinkedTokenSource(stopping);
Task consoleTask = console.RunAsync(app.Lifetime.StopApplication, consoleStopping.Token);
Task closingTask = StopWhenClosedAsync(app.Lifetime.StopApplication, stopping);
Task parentTask = configuration.StopOnInputClose ? StopWhenInputClosesAsync(app.Lifetime.StopApplication, stopping) : Task.CompletedTask;
try
{
    await Task.Factory.StartNew(() => server.Run(stopping), stopping, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);
}
finally
{
    await consoleStopping.CancelAsync().ConfigureAwait(false);
    await consoleTask.ConfigureAwait(false);
    await closingTask.ConfigureAwait(false);

    // A pending read of standard input cannot be cancelled; when the raid stopped for another reason, don't wait for it.
    if (parentTask.IsCompleted)
    {
        await parentTask.ConfigureAwait(false);
    }
    await app.StopAsync().ConfigureAwait(false);
}
return 0;

// One raid per process: stop once it is over, so the Host can start a fresh one.
async Task StopWhenClosedAsync(Action stop, CancellationToken cancellationToken)
{
    using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
    try
    {
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!allocations.Closed)
            {
                continue;
            }

            log.Write("raid", "closed");
            stop();
            return;
        }
    }
    catch (OperationCanceledException)
    {
    }
}

// The Host keeps standard input open while it runs; end of input means it is gone.
async Task StopWhenInputClosesAsync(Action stop, CancellationToken cancellationToken)
{
    await using Stream input = Console.OpenStandardInput();
    byte[] buffer = new byte[256];
    try
    {
        while (await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false) > 0)
        {
        }

        log.Write("raid", "host_gone");
        stop();
    }
    catch (OperationCanceledException)
    {
    }
}

double Clock() => 1 + Stopwatch.GetElapsedTime(started).TotalSeconds;
