using System.Globalization;
using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Replication;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.GameplayTags;

namespace Ares.Raid.World.Players;

/// <summary>
/// A player's departure with an extraction elevator: once an elevator left with the player inside (or an operator
/// extracted them), the end of their round on the controller, step by step, until the end of the round or the fallback
/// back to the main menu.
/// </summary>
internal sealed class PlayerDeparture
{
    // ARES's own choice, not a game value: how long the server waits after the end-of-round transition for the client
    // to say it is ready for the end of the round before sending it back to the main menu instead.
    private const double FallbackSeconds = 20.0;

    // This player's departure (the elevator's index, null for an operator extraction, and the server world time it
    // left), whether an operator asked for an extraction, the export naming the controller's end-of-round component,
    // and the server world times of each end-of-round step.
    private (int? Elevator, double At)? _departure;
    private bool _requested;
    private ReliableKey? _endOfRoundExport;
    private double? _transitionAt;
    private double? _readyAt;
    private double? _endAt;
    private double? _fallbackAt;

    /// <summary>Whether an elevator left with the player: their input no longer acts or moves in the raid.</summary>
    public bool Departed => _departure is not null;

    /// <summary>Whether the end-of-round transition started: the client may now say it is ready for the end of the round.</summary>
    public bool Transitioning => _transitionAt is not null;

    /// <summary>
    /// The departure as elevator@server time it left (operator@ for an operator extraction), then each end-of-round step with its server time: transition (the
    /// client starts its end of round), ready (the client is ready for it), end (the end of round sent) or fallback (no
    /// ready in time: sent back to the main menu). Empty before a departure.
    /// </summary>
    public string Summary => _departure is not { } departure ? string.Empty : string.Join(' ', ((string?[])[
        $"{departure.Elevator?.ToString(CultureInfo.InvariantCulture) ?? "operator"}@{PlayerChannels.Seconds(departure.At)}",
        _transitionAt is { } transition ? $"transition@{PlayerChannels.Seconds(transition)}" : null,
        _readyAt is { } ready ? $"ready@{PlayerChannels.Seconds(ready)}" : null,
        _endAt is { } end ? $"end@{PlayerChannels.Seconds(end)}" : null,
        _fallbackAt is { } fallback ? $"fallback@{PlayerChannels.Seconds(fallback)}" : null,
    ]).OfType<string>());

    /// <summary>Copies the departure for speculative packet processing; it holds values only.</summary>
    /// <returns>The copy.</returns>
    public PlayerDeparture Copy() => (PlayerDeparture)MemberwiseClone();

    /// <summary>An operator extracts the player: the departure follows at the next tick, without an elevator.</summary>
    /// <returns>False when the player has already departed or an extraction is already on its way.</returns>
    public bool Request()
    {
        if (_departure is not null || _requested)
        {
            return false;
        }

        _requested = true;
        return true;
    }

    /// <summary>The client says it is ready for the end of the round; the first time counts.</summary>
    /// <param name="raid">The raid, whose clock gives the time.</param>
    public void Ready(RaidWorld raid) => _readyAt ??= raid.WorldSeconds(raid.Clock());

    /// <summary>
    /// Ends this player's round once an elevator left with them inside (its doors closed): the extraction, the
    /// controller's end-of-round component and its quest progress, the end-of-round data (no experience, no journey),
    /// the transition to the end-of-round screen and the end of the data. The client then collects its data and says
    /// when it is ready; the end of the round follows, and the client returns to the main menu by itself. A client that
    /// never says it is ready is sent back to the main menu.
    /// </summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="elevators">The player's elevators, whose descend hold ends with the departure.</param>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="now">Raid clock.</param>
    public void Service(IPlayerChannelsContext player, PlayerElevators elevators, ReliableSender sender, double now)
    {
        RaidWorld raid = player.Raid;
        double at = raid.WorldSeconds(now);
        if (_departure is null)
        {
            if ((LeftWithElevator(player, at) ?? (_requested ? (null, at) : null)) is not { } departure)
            {
                return;
            }

            _departure = departure;
            elevators.Depart();
            player.Member.Depart();
            player.QueueControllerCall(sender, FunctionNames.ClientNotifyExtracted, new Dictionary<string, object?>
            {
                [PropertyNames.ExtractionType] = new GameplayTag(raid.GameData.Map.ExtractionElevator.ExtractionType),
            });
            _endOfRoundExport = sender.Queue(Bunches.Exports(raid.State.EndOfRoundExports(player.Player), [player.Player.EndOfRound]));
            return;
        }

        if (_transitionAt is null)
        {
            if (_endOfRoundExport is { } export && sender.Pending(export))
            {
                return;
            }

            sender.Queue(Bunches.Actor(ChannelLayout.Controller, raid.State.EndOfRoundCall(player.Player, FunctionNames.ClientFinalizeQuestProgress,
                new Dictionary<string, object?> { [PropertyNames.PlayerWon] = true }).Write));
            player.QueueControllerCall(sender, FunctionNames.ReceiveXpData, new Dictionary<string, object?>
            {
                [PropertyNames.XpEventLog] = null,
                [PropertyNames.XpBasedRewardsLog] = null,
            });
            player.QueueControllerCall(sender, FunctionNames.ClientClearReconnectData, []);
            player.QueueControllerCall(sender, FunctionNames.ClientStartEndOfRoundTransition, new Dictionary<string, object?> { [PropertyNames.PlayerWon] = true });
            player.QueueControllerCall(sender, FunctionNames.ReceiveEndOfRoundJourneySummary, new Dictionary<string, object?> { [PropertyNames.Summary] = null });
            player.QueueControllerCall(sender, FunctionNames.EndOfRoundJourneyDataTransferComplete, []);
            _transitionAt = at;
            return;
        }

        if (_endAt is not null || _fallbackAt is not null)
        {
            return;
        }

        if (_readyAt is not null)
        {
            player.QueueControllerCall(sender, FunctionNames.ClientStartEndOfRound, new Dictionary<string, object?> { [PropertyNames.PlayerWon] = true });
            _endAt = at;
        }
        else if (at >= _transitionAt + FallbackSeconds)
        {
            // As after a surrender: an empty return reason.
            player.QueueControllerCall(sender, FunctionNames.ClientReturnToMainMenuWithTextReason, new Dictionary<string, object?> { [PropertyNames.ReturnReason] = "" });
            _fallbackAt = at;
        }
    }

    // The elevator that left with this player inside and when it left (its doors closed), once it has.
    private static (int? Elevator, double At)? LeftWithElevator(IPlayerChannelsContext player, double at)
    {
        RaidWorld raid = player.Raid;
        foreach (ExtractionPoint point in raid.Extraction.Points)
        {
            if (raid.ExtractionStates.Departure(point.Index) is { } departure && departure.Players.Contains(player.Member.Id)
                && at >= departure.At + raid.GameData.Map.ExtractionElevator.DoorsSeconds)
            {
                return (point.Index, departure.At + raid.GameData.Map.ExtractionElevator.DoorsSeconds);
            }
        }

        return null;
    }
}
