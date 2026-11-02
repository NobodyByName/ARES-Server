using System.Text.Json.Nodes;
using Ares.Common;
using Ares.Common.CommandLine;
using Ares.Common.Logging;
using Ares.Host;
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

JsonEventLog log = JsonEventLog.StandardOutput;
WebApplication app = HostApplication.Build(configuration, log);
await app.StartAsync().ConfigureAwait(false);
log.Write(new JsonObject
{
    ["service"] = "ARES-Host",
    ["version"] = AresVersion.Current,
    ["listening"] = $"{configuration.Bind}:{configuration.Port}",
    ["public_origin"] = configuration.Settings.PublicOrigin,
    ["network_clients"] = configuration.AllowNetworkClients,
    ["matchmaking"] = configuration.SessionControlUrl is not null,
    ["identity_tokens"] = configuration.Settings.IdentityKey is not null ? "operator-key" : "unchecked",
});
await app.WaitForShutdownAsync().ConfigureAwait(false);
return 0;
