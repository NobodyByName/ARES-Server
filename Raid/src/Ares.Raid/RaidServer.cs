using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using Ares.Common.Logging;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Raid.World.Loot;
using Ares.Raid.World.Players;
using Ares.Structs.Game.World;

namespace Ares.Raid;

/// <summary>
/// The game socket loop: hands each datagram to the connection layer, sends replies, retries and
/// keepalives. Logs notable events at once, the raid's loot spawn counts once, player phase changes, and a traffic
/// summary every few seconds.
/// </summary>
/// <param name="socket">Bound UDP socket.</param>
/// <param name="connections">The connection layer (used by this loop only).</param>
/// <param name="log">Event log.</param>
/// <param name="clock">Monotonic clock in seconds.</param>
/// <param name="admin">Admin commands, run between packets; null for none.</param>
/// <param name="currentRaid">The raid being served, if any; null to log no raid-level events.</param>
public sealed class RaidServer(Socket socket, RaidConnections connections, JsonEventLog log, Func<double> clock, RaidAdmin? admin = null, Func<RaidWorld?>? currentRaid = null)
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

        // A client keeps sending after its connection was dropped; the traffic summary counts these.
        ConnectionEvent.PacketBeforeHandshake,
    ];

    private readonly Dictionary<ConnectionEvent, int> _counts = [];
    private readonly Dictionary<IPEndPoint, DeliveryPhase> _phases = [];
    private (RaidWorld? World, int Pickups, int LootOpens) _raid;
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

                admin?.RunPending();
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
        ReportRaid();
        ReportPhases();
        ReportTraffic();
    }

    // Raid-level events are logged once each from the raid itself, with its loot spawn summary when the raid appears.
    private void ReportRaid()
    {
        if (currentRaid?.Invoke() is not { } raid)
        {
            return;
        }

        if (_raid.World != raid)
        {
            _raid = (raid, 0, 0);
            LogLootSpawn(raid);
        }

        foreach (string entry in raid.Pickups.Events.Skip(_raid.Pickups))
        {
            log.Write("raid_pickup", entry);
        }

        // First openings of containers, with what they held.
        foreach (string entry in raid.LootStates.Events.Skip(_raid.LootOpens))
        {
            log.Write("raid_loot_open", entry);
        }

        _raid = (raid, raid.Pickups.Events.Count, raid.LootStates.Events.Count);
    }

    private void LogLootSpawn(RaidWorld raid)
    {
        LootSpawnCounts loot = raid.Loot.Counts;
        log.Write(new JsonObject
        {
            ["raid_loot"] = new JsonObject
            {
                ["sockets"] = loot.Sockets,
                ["spawned"] = loot.Spawned,
                ["classes"] = loot.Classes,
                ["skipped_inactive_layer"] = loot.InactiveLayer,
                ["skipped_no_container_type"] = loot.NoContainerType,
                ["skipped_unsupported_class"] = loot.Unsupported,
                ["skipped_by_roll"] = loot.SkippedByRoll,
                ["skipped_by_group_cap"] = loot.SkippedByGroupCap,
                ["skipped_by_limiter"] = loot.SkippedByLimiter,
                ["spawned_by_loot_value"] = new JsonObject
                {
                    ["untiered"] = loot.SpawnedByLootValue[PointOfInterestLootValue.Unknown],
                    ["low"] = loot.SpawnedByLootValue[PointOfInterestLootValue.Low],
                    ["medium"] = loot.SpawnedByLootValue[PointOfInterestLootValue.Medium],
                    ["high"] = loot.SpawnedByLootValue[PointOfInterestLootValue.High],
                },
                ["spawners"] = new JsonObject
                {
                    ["spawners"] = loot.Spawners.Spawners,
                    ["run"] = loot.Spawners.Run,
                    ["spawned"] = loot.Spawners.Spawned,
                    ["skipped_group_not_picked"] = loot.Spawners.NotPicked,
                    ["skipped_inactive_layer"] = loot.Spawners.InactiveLayer,
                    ["skipped_class_disabled"] = loot.Spawners.ClassDisabled,
                    ["skipped_class_unsupported"] = loot.Spawners.ClassUnsupported,
                },
                ["placed"] = Placed(loot.Placed),
                ["seed"] = raid.Loot.Seed,
                ["free_standing"] = FreeStanding(raid.Loot.FreeStanding),
                ["placeholder_classes"] = Placeholders(raid.Loot.Placeholders),
                ["loose"] = Loose(raid.LooseLoot.Counts),
            },
        });
    }

    private void ReportPhases()
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
    }

    private void ReportTraffic()
    {
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
        foreach ((IPEndPoint peer, IPlayerChannels? player) in connections.Players)
        {
            if (player is not PlayerChannels channels)
            {
                continue;
            }

            JsonObject traffic = PlayerTraffic(channels);
            if (connections.TakeDeliveryTimings(peer) is { } delivery)
            {
                traffic["delivery"] = DeliveryTraffic(delivery);
            }

            players.Add(traffic);
        }

        log.Write(new JsonObject { ["raid_traffic"] = events, ["connections"] = connections.Count, ["players"] = players });
        _counts.Clear();
    }

    // Reliable delivery over the interval, in milliseconds: queued to acknowledged (average, worst and the worst one's
    // channel), queued to first sent, most sends of one bunch, and what still waits.
    private static JsonObject DeliveryTraffic(DeliveryTimings delivery) => new()
    {
        ["delivered"] = delivery.Delivered,
        ["average_ms"] = Milliseconds(delivery.AverageSeconds),
        ["max_ms"] = Milliseconds(delivery.MaxSeconds),
        ["max_channel"] = delivery.MaxChannel,
        ["max_send_wait_ms"] = Milliseconds(delivery.MaxSendWaitSeconds),
        ["max_attempts"] = delivery.MaxAttempts,
        ["backlog"] = delivery.Backlog,
        ["oldest_pending_ms"] = Milliseconds(delivery.OldestPendingSeconds),
    };

    private static long Milliseconds(double seconds) => (long)Math.Round(seconds * 1000);

    private static JsonObject PlayerTraffic(PlayerChannels channels)
    {
        JsonObject calls = [];
        foreach ((string name, int count) in channels.IgnoredCalls.OrderByDescending(entry => entry.Value).Take(8))
        {
            calls[name] = count;
        }

        return new JsonObject
        {
            ["phase"] = channels.Phase.ToString(),
            ["input_frame"] = channels.InputFrame,
            ["input_position"] = channels.InputPosition is { } position
                ? new JsonArray(Math.Round(position.X), Math.Round(position.Y), Math.Round(position.Z))
                : null,
            ["input_yaw_degrees"] = channels.InputYaw is { } yaw ? Math.Round(yaw * 180 / Math.PI, 1) : null,
            ["input_decode_failures"] = channels.InputDecodeFailures,
            ["input_decode_error"] = channels.LastInputDecodeError,
            ["input_equip_target"] = channels.LatestWeaponInput?.EquipTarget,
            ["input_equip_change"] = channels.LatestWeaponInput?.EquipChange,
            ["input_weapon_flags"] = channels.LatestWeaponInput is { } weapon ? $"0x{weapon.Flags:x}" : null,
            ["blackboard_frames"] = channels.BlackboardFrames,
            ["simulated_speed"] = channels.SimulatedCharacter is { } character
                ? Math.Round(Math.Sqrt((character.Velocity.X * character.Velocity.X) + (character.Velocity.Y * character.Velocity.Y)))
                : null,
            ["simulated_stance"] = channels.SimulatedCharacter?.Stance.ToString(),
            ["simulated_locomotion"] = channels.SimulatedCharacter?.Locomotion.ToString(),
            ["simulated_flashlight"] = channels.SimulatedCharacter?.Flashlight,
            ["simulated_missing_frames"] = channels.MissingInputFrames,
            ["simulated_airborne"] = channels.SimulatedCharacter?.Body?.Airborne,
            ["position_reseeds"] = channels.PositionReseeds,
            ["position_reseed_worst"] = Math.Round(channels.WorstPositionReseed, 1),
            ["position_reseed_last"] = channels.LastPositionReseed is { } reseed
                ? new JsonArray(reseed.Frame, Math.Round(reseed.Distance, 1), reseed.Airborne ? "air" : "ground")
                : null,
            ["door_presses"] = channels.DoorPresses,
            ["ignored_door_requests"] = channels.IgnoredDoorRequests,
            ["door_breaches"] = channels.DoorBreaches,
            ["cancelled_breaches"] = channels.CancelledBreaches,
            ["breach_requests"] = string.Join(' ', channels.BreachRequests.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}x{entry.Value}")),
            ["door_unlocks"] = channels.DoorUnlocks,
            ["missing_door_keys"] = channels.MissingDoorKeys,
            ["unlock_requests"] = string.Join(' ', channels.UnlockRequests.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}x{entry.Value}")),
            ["inventory_moves"] = channels.InventoryMoves,
            ["shots_fired"] = channels.ShotsFired,
            ["reloads_completed"] = channels.ReloadsCompleted,
            ["quick_item_requests"] = channels.QuickItemRequests,
            ["hook_throws"] = channels.HookThrows,
            ["quick_item_frame_holds"] = channels.QuickItemHolds,
            ["quick_item_last_hold"] = channels.LastQuickItemHold is { } hold ? new JsonArray(hold.From, hold.To) : null,
            ["rejected_inventory_requests"] = channels.RejectedInventoryRequests,
            ["rejected_inventory_log"] = string.Join(", ", channels.RejectedInventoryLog),
            ["ladders_ready"] = channels.LaddersReady,
            ["ladders_in_progress"] = channels.LaddersInProgress,
            ["loot_ready"] = channels.LootReady,
            ["loot_waiting"] = channels.LootWaiting,
            ["loot_failed"] = channels.LootFailed,
            ["loot_failures"] = string.Join(' ', channels.LootFailures),
            ["loot_channels"] = channels.LootChannelsInUse,
            ["loot_channel_opens"] = channels.LootChannelOpens,
            ["loot_requests"] = string.Join(' ', channels.LootRequests),
            ["items_dropped"] = channels.ItemsDropped,
            ["pickups_taken"] = channels.PickupsTaken,
            ["pickups_ready"] = channels.PickupsReady,
            ["pickup_channels"] = channels.PickupChannelsInUse,
            ["pickup_requests"] = string.Join(' ', channels.PickupRequests),
            ["arcs_ready"] = channels.ArcsReady,
            ["arc_channels"] = channels.ArcChannelsInUse,
            ["arc_frames"] = $"{channels.ArcFrames.Sent} sent, {channels.ArcFrames.Confirmed} confirmed",
            ["arc_failures"] = string.Join(' ', channels.ArcFailures),
            ["elevators_ready"] = channels.ElevatorsReady,
            ["elevator_states"] = channels.ElevatorStates,
            ["elevator_calls"] = channels.ElevatorCalls,
            ["elevator_changes"] = string.Join(", ", channels.ElevatorChanges),
            ["elevator_descents"] = channels.ElevatorDescents,
            ["cancelled_descents"] = channels.CancelledDescents,
            ["completed_descents"] = channels.CompletedDescents,
            ["elevator_departures"] = channels.ElevatorDepartures,
            ["departure"] = channels.Departure,
            ["elevator_requests"] = string.Join(", ", channels.ElevatorRequests),
            ["unknown_interaction_requests"] = string.Join(", ", channels.UnknownInteractionRequests),
            ["unhandled_calls"] = calls,
        };
    }

    private static JsonArray FreeStanding(IReadOnlyList<FreeStandingContainer> containers)
    {
        JsonArray entries = [];
        foreach (FreeStandingContainer container in containers)
        {
            entries.Add(new JsonObject
            {
                ["class"] = container.Class,
                ["area"] = container.Area,
                ["parent_area"] = container.ParentArea,
                ["location"] = new JsonArray(Math.Round(container.Location.X), Math.Round(container.Location.Y), Math.Round(container.Location.Z)),
            });
        }

        return entries;
    }

    private static JsonObject Placeholders(IReadOnlyDictionary<string, int> classes)
    {
        JsonObject entries = [];
        foreach ((string name, int count) in classes)
        {
            entries[name] = count;
        }

        return entries;
    }

    private static JsonObject Loose(LooseLootCounts counts) => new()
    {
        ["sockets"] = counts.Sockets,
        ["spawned"] = counts.Spawned,
        ["grounded"] = counts.Grounded,
        ["skipped_inactive_layer"] = counts.InactiveLayer,
        ["skipped_nothing_to_hold"] = counts.NothingToHold,
        ["skipped_by_roll"] = counts.SkippedByRoll,
        ["skipped_by_limiter"] = counts.SkippedByLimiter,
        ["decided_by_rule"] = counts.DecidedByRule,
        ["spawned_by_loot_value"] = new JsonObject
        {
            ["untiered"] = counts.SpawnedByLootValue[PointOfInterestLootValue.Unknown],
            ["low"] = counts.SpawnedByLootValue[PointOfInterestLootValue.Low],
            ["medium"] = counts.SpawnedByLootValue[PointOfInterestLootValue.Medium],
            ["high"] = counts.SpawnedByLootValue[PointOfInterestLootValue.High],
        },
    };

    private static JsonObject Placed(LootPlacedCounts counts) => new()
    {
        ["containers"] = counts.Containers,
        ["spawned"] = counts.Spawned,
        ["skipped_inactive_layer"] = counts.InactiveLayer,
        ["skipped_unsupported_class"] = counts.Unsupported,
        ["skipped_no_container_type"] = counts.NoContainerType,
        ["skipped_by_roll"] = counts.SkippedByRoll,
    };
}
