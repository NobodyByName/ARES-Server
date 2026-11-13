using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common.Control;
using Ares.Common.Logging;
using Ares.Host.Services.Matchmaking;

namespace Ares.Host;

/// <summary>How Host starts ARES-Raid servers.</summary>
/// <param name="Executable">Absolute path of the ARES-Raid executable.</param>
/// <param name="TokenFile">Absolute path of the operator token file both servers read.</param>
/// <param name="OperatorToken">The operator token.</param>
/// <param name="Bind">Game listen address.</param>
/// <param name="AdvertiseHost">Host name or address players connect to.</param>
/// <param name="GamePort">UDP game port of slot 0; slot n uses the port plus n.</param>
/// <param name="ControlPort">Loopback allocation API port of slot 0; slot n uses the port plus n.</param>
/// <param name="Players">Most players in one raid, across every party.</param>
/// <param name="FillSeconds">Seconds after a raid's first party during which other parties may join.</param>
/// <param name="Lighting">Lighting of every raid, or null for the map's weighted pick.</param>
/// <param name="MapDataDirectory">Absolute path of the folder with the map files, or null when none is configured.</param>
public sealed record RaidLaunch(
    string Executable,
    string TokenFile,
    string OperatorToken,
    IPAddress Bind,
    string AdvertiseHost,
    int GamePort,
    int ControlPort,
    int Players,
    int FillSeconds,
    string? Lighting,
    string? MapDataDirectory);

/// <summary>Starts ARES-Raid as child processes and forwards their events to the Host log.</summary>
internal sealed class RaidProcesses(RaidLaunch launch, JsonEventLog log) : IRaidStarter
{
    /// <inheritdoc />
    public IRaidServer Start(int slot)
    {
        int gamePort = launch.GamePort + slot;
        int controlPort = launch.ControlPort + slot;
        ProcessStartInfo info = new(launch.Executable)
        {
            UseShellExecute = false,
            // Kept open for the Raid's lifetime: it stops once the Host is gone.
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        string[] arguments =
        [
            "--port", Text(gamePort), "--bind", launch.Bind.ToString(), "--advertise-host", launch.AdvertiseHost,
            "--control-port", Text(controlPort), "--control-token-file", launch.TokenFile, "--max-players", Text(launch.Players),
            "--fill-seconds", Text(launch.FillSeconds),
            "--console", "json", "--stop-on-input-close",
        ];
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        if (launch.Lighting is not null)
        {
            info.ArgumentList.Add("--lighting");
            info.ArgumentList.Add(launch.Lighting);
        }

        if (launch.MapDataDirectory is not null)
        {
            info.ArgumentList.Add("--map-data-dir");
            info.ArgumentList.Add(launch.MapDataDirectory);
        }

        Process process = new() { StartInfo = info, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, line) => Forward(slot, line.Data);
        process.ErrorDataReceived += (_, line) => Forward(slot, line.Data);
        process.Exited += (_, _) => log.Write(new JsonObject { ["raid_server"] = "exited", ["slot"] = slot });
        try
        {
            process.Start();
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            log.Write(new JsonObject { ["raid_server"] = "start_failed", ["slot"] = slot, ["error"] = error.Message });
            throw new AllocationUnavailableException("ARES-Raid could not be started");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        log.Write(new JsonObject { ["raid_server"] = "started", ["slot"] = slot, ["pid"] = process.Id, ["game_port"] = gamePort });
        return new RaidProcess(process, new SessionAllocatorClient(new Uri($"http://127.0.0.1:{Text(controlPort)}"), launch.OperatorToken));
    }

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    private void Forward(int slot, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(line);
        }
        catch (JsonException)
        {
            parsed = null;
        }

        log.Write(new JsonObject { ["raid"] = slot, ["event"] = parsed is JsonObject ? parsed : line });
    }

    private sealed class RaidProcess(Process process, SessionAllocatorClient allocator) : IRaidServer
    {
        public SessionAllocatorClient Allocator => allocator;

        public bool HasExited => process.HasExited;

        public void Dispose()
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception error) when (error is InvalidOperationException or Win32Exception)
            {
            }

            allocator.Dispose();
            process.Dispose();
        }
    }
}
