using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using Ares.Common.Logging;
using Ares.Raid.Transport;
using Ares.Raid.World;

namespace Ares.Raid;

/// <summary>
/// The game socket loop: hands each datagram to the connection layer, sends replies, retries and
/// keepalives. Logs notable events at once, player phase changes, and a traffic summary every few seconds.
/// </summary>
/// <param name="socket">Bound UDP socket.</param>
/// <param name="connections">The connection layer (used by this loop only).</param>
/// <param name="log">Event log.</param>
/// <param name="clock">Monotonic clock in seconds.</param>
public sealed class RaidServer(Socket socket, RaidConnections connections, JsonEventLog log, Func<double> clock)
{
    private const int MaxDatagramBytes = 65536;
    private const int WaitMicroseconds = 50_000;
    private const double SummaryInterval = 5.0;

    private static readonly HashSet<ConnectionEvent> s_routine =
    [
        ConnectionEvent.Acknowledged,
        ConnectionEvent.ReplySent,
        ConnectionEvent.KeepaliveSent,
        ConnectionEvent.DirectStreamReceived,
        ConnectionEvent.DirectStreamDropped,
        ConnectionEvent.ReplayedPacket,
    ];

    private readonly Dictionary<ConnectionEvent, int> _counts = [];
    private readonly Dictionary<IPEndPoint, DeliveryPhase> _phases = [];
    private double _summaryAt;

    /// <summary>Serves until <paramref name="stop"/> is cancelled.</summary>
    /// <param name="stop">Stops the loop.</param>
    public void Run(CancellationToken stop)
    {
        byte[] buffer = new byte[MaxDatagramBytes];
        _summaryAt = clock() + SummaryInterval;
        while (!stop.IsCancellationRequested)
        {
            try
            {
                if (socket.Poll(WaitMicroseconds, SelectMode.SelectRead))
                {
                    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    int length = socket.ReceiveFrom(buffer, ref remote);
                    ConnectionResult result = connections.Handle(buffer[..length], (IPEndPoint)remote);
                    Send(result, remote);
                }

                foreach ((IPEndPoint peer, ConnectionResult result) in connections.Poll())
                {
                    Send(result, peer);
                }

                Report();
            }
            catch (SocketException error)
            {
                // A peer that went away (an unreachable-port report) must not stop the server.
                log.Write("socket_error", error.SocketErrorCode.ToString());
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                log.Write(new JsonObject { ["unexpected_error"] = error.GetType().Name, ["message"] = error.Message });
            }
        }
    }

    private void Send(ConnectionResult result, EndPoint peer)
    {
        if (result.Reply is { } reply)
        {
            socket.SendTo(reply, peer);
        }

        _counts[result.Event] = _counts.GetValueOrDefault(result.Event) + 1;
        if (!s_routine.Contains(result.Event))
        {
            log.Write(new JsonObject { ["raid_event"] = result.Event.ToString(), ["detail"] = result.Detail });
        }
    }

    private void Report()
    {
        foreach ((IPEndPoint peer, IPlayerChannels? player) in connections.Players)
        {
            if (player is not PlayerChannels channels || _phases.GetValueOrDefault(peer) == channels.Phase)
            {
                continue;
            }

            _phases[peer] = channels.Phase;
            log.Write("player_phase", channels.Phase.ToString());
        }

        foreach (IPEndPoint gone in _phases.Keys.Where(peer => connections.Players.All(entry => !entry.Peer.Equals(peer))).ToArray())
        {
            _phases.Remove(gone);
        }

        double now = clock();
        if (now < _summaryAt)
        {
            return;
        }

        _summaryAt = now + SummaryInterval;
        if (_counts.Count == 0)
        {
            return;
        }

        JsonObject events = [];
        foreach ((ConnectionEvent kind, int count) in _counts.OrderBy(entry => entry.Key))
        {
            events[kind.ToString()] = count;
        }

        JsonArray players = [];
        foreach (PlayerChannels channels in connections.Players.Select(entry => entry.Player).OfType<PlayerChannels>())
        {
            JsonObject calls = [];
            foreach ((string name, int count) in channels.IgnoredCalls.OrderByDescending(entry => entry.Value).Take(8))
            {
                calls[name] = count;
            }

            players.Add(new JsonObject
            {
                ["phase"] = channels.Phase.ToString(),
                ["input_frame"] = channels.InputFrame,
                ["input_decode_failures"] = channels.InputDecodeFailures,
                ["blackboard_frames"] = channels.BlackboardFrames,
                ["door_presses"] = channels.DoorPresses,
                ["ignored_door_requests"] = channels.IgnoredDoorRequests,
                ["unhandled_calls"] = calls,
            });
        }

        log.Write(new JsonObject { ["raid_traffic"] = events, ["connections"] = connections.Count, ["players"] = players });
        _counts.Clear();
    }
}
