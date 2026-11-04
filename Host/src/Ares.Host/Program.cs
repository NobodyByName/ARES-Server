using System.Text.Json.Nodes;
using Ares.Common;
using Ares.Common.CommandLine;
using Ares.Common.Logging;
using Ares.Host;
using Ares.Host.Services.Inventory;
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
await using WebApplication app = HostApplication.Build(configuration, log, loggerProvider: console);
console.Inventory = app.Services.GetRequiredService<InventoryStore>();
await app.StartAsync().ConfigureAwait(false);
log.Write(new JsonObject
{
    ["service"] = "ARES-Host",
    ["version"] = AresVersion.Current,
    ["configuration_file"] = configuration.ConfigurationFile,
    ["listening"] = $"{configuration.Bind}:{configuration.Port}",
    ["public_origin"] = configuration.Settings.PublicOrigin,
    ["network_clients"] = configuration.AllowNetworkClients,
    ["matchmaking"] = configuration.SessionControlUrl is not null,
    ["identity_tokens"] = configuration.Settings.IdentityKey is not null ? "operator-key" : "unchecked",
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
