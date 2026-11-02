using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common.Logging;
using Ares.Host.Services.Content;
using Ares.Host.Services.Identity;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Identity;
using Ares.Structs.Backend.Profiles;
using Ares.Structs.Backend.Social;

namespace Ares.Host.Services.Social;

/// <summary>The local player's single party and its ordered notification stream (long-polled by the client).</summary>
public sealed class PartyService(BackendGate gate, ProfileFactory profiles, JsonEventLog log, TimeProvider time)
{
    /// <summary>Notifications kept for clients that poll late.</summary>
    private const int RetainedNotifications = 128;

    /// <summary>Longest wait of one notifications poll.</summary>
    private static readonly TimeSpan s_pollTimeout = TimeSpan.FromSeconds(10);

    private readonly List<Notification> _notifications = [];
    private PartyState? _party;
    private long _sequence;
    private TaskCompletionSource _published = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>True when a party exists. Caller holds the gate.</summary>
    internal bool HasPartyLocked => _party is not null;

    /// <summary>The party leader. Caller holds the gate and checked <see cref="HasPartyLocked"/>.</summary>
    internal long LeaderLocked => _party!.Leader;

    /// <summary><c>POST /v1/shared/social/party/v2/create</c>.</summary>
    public async Task<ServiceReply> CreateAsync(CreatePartyRequest request)
    {
        using BackendGate.Releaser held = await gate.EnterAsync().ConfigureAwait(false);
        EmbarkProfile profile = profiles.Create();
        _party = new PartyState(LocalPlayer.TenancyUserId, profile)
        {
            CrossplayEnabled = request.CrossplayEnabled ?? true,
            PlatformSessionId = request.PlatformSessionId ?? "",
            Language = request.Language ?? "en",
            InvitePermission = request.InvitePermission ?? InvitePermissions.AnyMember,
        };
        Party party = _party.Snapshot();
        Publish(new PartyArrangeNotification(party.PartyId, party.LeaderTenancyUserId, [party.LeaderTenancyUserId],
            [new PartyArrangeMemberData(ExternalProviders.SteamName, _party.PlatformSessionId, _party.CrossplayEnabled, profiles.Create(), _party.Language)],
            profiles.Create(), party));
        return ServiceReply.Ok(new CreatePartyResponse(party, 3));
    }

    /// <summary><c>PATCH /v1/shared/social/party/v2/update-party-attributes</c>.</summary>
    public async Task<ServiceReply> UpdateAttributesAsync(UpdatePartyAttributesRequest request)
    {
        using BackendGate.Releaser held = await gate.EnterAsync().ConfigureAwait(false);
        if (_party is null || request.PartyId != LocalPlayer.PartyId)
        {
            return ServiceReply.Error(409, "unknown_local_party");
        }

        IReadOnlyDictionary<string, JsonElement> modified = request.Attributes ?? new Dictionary<string, JsonElement>();
        foreach ((string key, JsonElement value) in modified)
        {
            _party.Attributes[key] = value.Clone();
        }

        Publish(new PartyAttributesChangedNotification(_party.Leader, LocalPlayer.PartyId,
            new Dictionary<string, JsonElement>(modified), new Dictionary<string, JsonElement>(_party.Attributes)));
        return ServiceReply.Ok(EmptyResponse.Instance);
    }

    /// <summary><c>POST /v1/shared/social/party/v2/update-party-member-data</c>.</summary>
    public async Task<ServiceReply> UpdateMemberAsync(UpdatePartyMemberDataRequest request)
    {
        using BackendGate.Releaser held = await gate.EnterAsync().ConfigureAwait(false);
        if (_party is null || request.PartyId != LocalPlayer.PartyId)
        {
            return ServiceReply.Error(409, "unknown_local_party");
        }

        _party.CrossplayEnabled = request.CrossplayEnabled ?? _party.CrossplayEnabled;
        _party.PlatformSessionId = request.PlatformSessionId ?? _party.PlatformSessionId;
        _party.Language = request.Language ?? _party.Language;
        _party.Ready = request.ReadyForMatchmaking ?? _party.Ready;
        if (request.PlatformSessionId is not null)
        {
            Publish(new PartyPlayerPlatformSessionIdNotification(LocalPlayer.PartyId, _party.Leader, _party.PlatformSessionId));
        }

        if (request.CrossplayEnabled is not null)
        {
            Publish(new PartyPlayerCrossplayEnabledNotification(LocalPlayer.PartyId, _party.Leader, _party.CrossplayEnabled));
        }

        return ServiceReply.Ok(EmptyResponse.Instance);
    }

    /// <summary><c>POST /v1/shared/social/party/set-ready-for-matchmaking</c>.</summary>
    public async Task<ServiceReply> SetReadyAsync(SetReadyForMatchmakingRequest request)
    {
        if (request.IsReady is not { } ready)
        {
            return ServiceReply.Error(400, "invalid_local_readiness");
        }

        using BackendGate.Releaser held = await gate.EnterAsync().ConfigureAwait(false);
        return SetReadyLocked(ready);
    }

    /// <summary>Sets readiness and notifies. Caller holds the gate.</summary>
    internal ServiceReply SetReadyLocked(bool ready)
    {
        if (_party is null)
        {
            return ServiceReply.Error(409, "no_local_party");
        }

        _party.Ready = ready;
        _party.ReadinessReported = true;
        Publish(new PartyPlayerReadyForMatchmakingNotification(_party.Leader, LocalPlayer.PartyId, ready));
        return ServiceReply.Ok(EmptyResponse.Instance);
    }

    /// <summary><c>POST /v1/shared/notifications</c>: waits up to <see cref="s_pollTimeout"/> for notifications after the cursor.</summary>
    public async Task<ServiceReply> PollAsync(NotificationsRequest request, CancellationToken cancellationToken)
    {
        string text = string.IsNullOrEmpty(request.FromNotificationId) ? "0" : request.FromNotificationId.Trim();
        if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long cursor))
        {
            return ServiceReply.Error(400, "invalid_notification_cursor");
        }

        DateTimeOffset deadline = time.GetUtcNow() + s_pollTimeout;
        while (true)
        {
            Task published;
            using (await gate.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                TimeSpan remaining = deadline - time.GetUtcNow();
                if (_sequence > cursor || remaining <= TimeSpan.Zero)
                {
                    Notification[] pending = [.. _notifications.Where(item => long.Parse(item.Id, CultureInfo.InvariantCulture) > cursor)];
                    long next = Math.Max(cursor, _sequence);
                    log.Write("local_notification_delivery", new JsonObject { ["cursor"] = cursor, ["next"] = next, ["count"] = pending.Length });
                    return ServiceReply.Ok(new NotificationsResponse(pending, next.ToString(CultureInfo.InvariantCulture)));
                }

                published = _published.Task;
            }

            TimeSpan wait = deadline - time.GetUtcNow();
            if (wait <= TimeSpan.Zero)
            {
                continue;
            }

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task timer = Task.Delay(wait, time, timeout.Token);
            await Task.WhenAny(published, timer).ConfigureAwait(false);
            await timeout.CancelAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private void Publish(NotificationPayload payload)
    {
        _sequence++;
        _notifications.Add(new Notification(_sequence.ToString(CultureInfo.InvariantCulture), payload.Name, payload));
        if (_notifications.Count > RetainedNotifications)
        {
            _notifications.RemoveRange(0, _notifications.Count - RetainedNotifications);
        }

        log.Write(new JsonObject { ["local_notification_created"] = _sequence, ["type"] = payload.Name });
        TaskCompletionSource published = _published;
        _published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        published.SetResult();
    }

    private sealed class PartyState(long leader, EmbarkProfile leaderProfile)
    {
        public long Leader { get; } = leader;

        public Dictionary<string, JsonElement> Attributes { get; } = new(StringComparer.Ordinal);

        public bool CrossplayEnabled { get; set; }

        public string PlatformSessionId { get; set; } = "";

        public string Language { get; set; } = "en";

        public string InvitePermission { get; init; } = InvitePermissions.AnyMember;

        public bool Ready { get; set; }

        /// <summary>Readiness is listed only after it was first set.</summary>
        public bool ReadinessReported { get; set; }

        public Party Snapshot()
        {
            return new Party(
                LocalPlayer.PartyId,
                Leader,
                [new PartyMember(leaderProfile)],
                ReadinessReported ? [new PartyMemberReadiness(Leader, Ready)] : [],
                [new PartyMemberData(Leader, Ready, CrossplayEnabled, ExternalProviders.SteamName, PlatformSessionId, Language)],
                new Dictionary<string, JsonElement>(Attributes),
                "ares-host-only",
                PartyPrivacies.InviteOnly,
                InvitePermission);
        }
    }
}
