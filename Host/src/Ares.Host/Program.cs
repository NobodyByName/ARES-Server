using System.Text.Json.Nodes;
using Ares.Common;
using Ares.Common.CommandLine;
using Ares.Common.Logging;
using Ares.Host;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Persistence;
using Ares.Host.Services.Players;
using Ares.Structs.Core;

HostConfiguration configuration;
try
{
    configuration = HostConfiguration.Parse(args);
}
catch (HelpRequestedException)
{
    Console.WriteLine(HostConfiguration.Usage);
    return 0;
}
catch (VersionRequestedException)
{
    Console.WriteLine($"ARES-Host {AresVersion.Current} (Steam build {GameBuild.SteamBuildId})");
    return 0;
}
catch (CommandLineException error)
{
    Console.Error.WriteLine(HostConfiguration.Usage);
    Console.Error.WriteLine($"ARES-Host: error: {error.Message}");
    return 2;
}

using HostConsole console = new(configuration.ConsoleMode, logFile: configuration.LogFile);
JsonEventLog log = console.Log;
WebApplication application;
try
{
    application = HostApplication.Build(configuration, log, loggerProvider: console);
}
catch (Exception error) when (error is PersistenceException or InvalidDataException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine("ARES-Host: cannot load player data. Check MongoDB and the player document structure.");
    return 2;
}

await using WebApplication app = application;
console.Accounts = app.Services.GetRequiredService<AccountDirectory>();
console.Players = app.Services.GetRequiredService<PlayerRegistry>();
await app.StartAsync().ConfigureAwait(false);
log.Write(new JsonObject
{
    ["service"] = "ARES-Host",
    ["version"] = AresVersion.Current,
    ["configuration_file"] = configuration.ConfigurationFile,
    ["listening"] = $"{configuration.Bind}:{configuration.Port}",
    ["public_origin"] = configuration.Settings.PublicOrigin,
    ["network_clients"] = configuration.AllowNetworkClients,
    ["matchmaking"] = configuration.Raids is not null,
    ["raid_capacity"] = configuration.Settings.RaidCapacity,
    ["raid_players"] = configuration.Raids?.Players,
    ["mongodb_database"] = configuration.Settings.MongoDbDatabase,
    ["identity_tokens"] = configuration.Settings.IdentityKey is not null ? "operator-key" : "per-run-key",
});
using CancellationTokenSource consoleStopping = CancellationTokenSource.CreateLinkedTokenSource(app.Lifetime.ApplicationStopping);
Task consoleTask = console.RunAsync(app.Lifetime.StopApplication, consoleStopping.Token);
try
{
    await app.WaitForShutdownAsync().ConfigureAwait(false);
}
finally
{
    await consoleStopping.CancelAsync().ConfigureAwait(false);
    await consoleTask.ConfigureAwait(false);
}
return 0;
