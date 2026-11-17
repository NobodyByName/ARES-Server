using Ares.Raid.Transport;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Loot;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World.Players;

/// <summary>
/// The loot containers as one player sees them: containers relevant to the player delivered on their own channels
/// (dormant once the client is up to date, woken when they change), and the player's searches and open container.
/// The loot sees every interaction request first: a looting-menu close ends there.
/// </summary>
internal sealed class PlayerLoot : IPlayerFamily
{
    // Loot containers are dormant: a channel opens to send a container's state and closes again, so a few channels
    // serve them all. New deliveries start only while most of them are free; the rest stay free for state changes.
    private const int MaxDeliveries = 24;
    private const int RequestLog = 40;
    private const int FailureLog = 16;

    private readonly ChannelRange _channels;
    private byte[] _known = [];
    private ActorChannelPool<LootLink> _links;
    private Dictionary<ulong, ReliableKey> _classExports = [];
    private HashSet<int> _dirty = [];
    private HashSet<int> _failed = [];
    private BoundedLog _failures = new(FailureLog);
    private int _openings;
    private double _relevantAt;
    private BoundedLog _requests = new(RequestLog);
    private (LootContainer Container, bool DrawsTool)? _press;
    private bool _closeRequested;
    private (LootContainer Container, uint Frame, double RequestedAt, double HoldStartsAt, double OpensAt)? _search;
    private InteractionRequest? _lastRequest;

    // Whether the request being routed repeats the previous request on loot (a held press).
    private bool _repeated;
    private (LootContainer Container, int ItemContainer)? _open;

    /// <summary>Creates the loot of a connection that has none yet.</summary>
    /// <param name="channels">The channels the containers share.</param>
    public PlayerLoot(ChannelRange channels)
    {
        _channels = channels;
        _links = new ActorChannelPool<LootLink>(channels);
    }

    // What the client knows of a loot container: nothing yet, or it keeps the actor showing it closed or opened.
    private enum LootKnowledge : byte
    {
        None,
        Closed,
        Opened,
    }

    /// <summary>Loot containers whose actor and search interaction this client has.</summary>
    public int Ready => _known.Count(known => known != (byte)LootKnowledge.None);

    /// <summary>Loot channels open now (or on the way): containers being delivered or updated.</summary>
    public int ChannelsInUse => _links.Count;

    /// <summary>Loot channel opens so far: first deliveries and state changes.</summary>
    public int ChannelOpens { get; private set; }

    /// <summary>Loot containers whose channel the client closed: they are not delivered again to this player.</summary>
    public int Failed => _failed.Count;

    /// <summary>
    /// The first loot containers whose channel the client closed, oldest first, as source:class@stage (the step whose
    /// bunch the channel was on: Spawning = the open, Naming = the component names, Open = a state).
    /// </summary>
    public IReadOnlyList<string> Failures => _failures;

    /// <summary>Searches started on loot containers.</summary>
    public int Searches { get; private set; }

    /// <summary>Searches the player stopped by moving.</summary>
    public int CancelledSearches { get; private set; }

    /// <summary>Loot containers opened by this player.</summary>
    public int Opens { get; private set; }

    /// <summary>Requests received on loot search interactions, oldest first, as request@input frame.</summary>
    public IReadOnlyList<string> Requests => _requests;

    /// <summary>Whether this player searches a container now.</summary>
    public bool Searching => _search is not null;

    /// <summary>Whether this player has a container open now.</summary>
    public bool ContainerOpen => _open is not null;

    /// <summary>Whether this client has a loot container (it keeps it while the container is dormant).</summary>
    /// <param name="container">Container index.</param>
    /// <returns>True once the container's state reached the client.</returns>
    public bool Has(int container) => Known(container) != LootKnowledge.None;

    /// <summary>Loot containers relevant to the player that the client does not have yet.</summary>
    /// <param name="player">The player's channels.</param>
    /// <returns>Their count.</returns>
    public int Waiting(IPlayerChannelsContext player) =>
        player.Raid.Loot.Relevant(player.RelevancePosition).Count(container => Known(container.Index) == LootKnowledge.None && !_failed.Contains(container.Index));

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerLoot Copy()
    {
        PlayerLoot copy = (PlayerLoot)MemberwiseClone();
        copy._known = (byte[])_known.Clone();
        copy._links = _links.Copy();
        copy._classExports = new Dictionary<ulong, ReliableKey>(_classExports);
        copy._dirty = [.. _dirty];
        copy._failed = [.. _failed];
        copy._failures = _failures.Copy();
        copy._requests = _requests.Copy();
        return copy;
    }

    /// <inheritdoc />
    public bool ChannelFailed(IPlayerChannelsContext player, int channel)
    {
        if (!_channels.Contains(channel))
        {
            return false;
        }

        foreach ((int container, LootLink link) in _links)
        {
            if (link.Channel != channel || !_failed.Add(container) || _failures.IsFull)
            {
                continue;
            }

            LootContainer failed = player.Raid.Loot.Containers[container];
            _failures.Add($"{failed.Source}:{FreeStandingAreas.ShortName(failed.Class.Container.Class)}@{link.Stage}");
        }

        return true;
    }

    /// <summary>
    /// Sees every interaction request before the families take it and before its repeat is counted for the logs:
    /// follows the requests on loot, and takes the client's looting-menu close while a container is open.
    /// </summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="request">The request.</param>
    /// <returns>True when the request closed the looting menu: it ends here.</returns>
    public bool ClosesMenu(IPlayerChannelsContext player, InteractionRequest request)
    {
        LootContainer? loot = Searchable(player.Raid, request.Target);
        _repeated = _lastRequest is { } last && last.Frame + 1 == request.Frame && last.Target == request.Target && last.Request == request.Request;
        if (loot is not null || request.Target == 0 || _open is not null)
        {
            _lastRequest = request;
        }

        // An open container closes when the client says its looting menu closed.
        if (_open is null || (request.Request & PendulumInput.LootingMenuClosedRequest) == 0)
        {
            return false;
        }

        if (!_repeated)
        {
            _requests.Add($"{request.Request}@{request.Frame}");
        }

        _closeRequested = true;
        return true;
    }

    /// <inheritdoc />
    /// <remarks>A held press on loot is told by the requests on loot alone (see <see cref="ClosesMenu"/>).</remarks>
    public bool TryHandle(IPlayerChannelsContext player, InteractionRequest request, bool repeated, double now)
    {
        if (Searchable(player.Raid, request.Target) is not { } loot)
        {
            return false;
        }

        // While a container is open, the requests on loot do nothing.
        if (_open is not null)
        {
            return true;
        }

        // Like a breach, one press starts the search, also when it takes the raider tool out first.
        if (_repeated)
        {
            return true;
        }

        _requests.Add($"{request.Request}@{request.Frame}");
        if ((request.Request & PendulumInput.PressRequest) != 0 && _search is null)
        {
            _press = (loot, (request.Request & PendulumInput.EquipToolRequestFlag) != 0);
        }

        return true;
    }

    /// <inheritdoc />
    public bool Usable(IPlayerChannelsContext player, ulong target) => Searchable(player.Raid, target) is not null;

    /// <inheritdoc />
    public void Service(IPlayerChannelsContext player, ReliableSender sender, double now)
    {
        // Gives the client the loot containers once map actors are allowed. Containers are dormant: a channel opens
        // for a container that becomes relevant (within the net cull distance; nearest first, a few at a time) to export
        // its class, spawn it at its socket, name its components and send its state, then closes dormant while the
        // client keeps the actor. A later change (a first opening by anyone, this player's search) opens a channel
        // again for a client that has the container, wherever the player is, and sends the new state.
        if (!player.MapActorsAllowed(now))
        {
            return;
        }

        // Containers spawned during the raid extend the list; what the player knows of the others stays.
        RaidWorld raid = player.Raid;
        if (_known.Length != raid.Loot.Containers.Count)
        {
            Array.Resize(ref _known, raid.Loot.Containers.Count);
        }

        IReadOnlyList<int> openings = raid.LootStates.Openings;
        for (; _openings < openings.Count; _openings++)
        {
            if (Known(openings[_openings]) != LootKnowledge.None)
            {
                _dirty.Add(openings[_openings]);
            }
        }

        _links.Advance(sender, new LootSteps(this, raid));
        ServiceSearch(player, sender, now);
        foreach (int index in _dirty.ToArray())
        {
            if (_links.Contains(index))
            {
                continue;
            }

            // A dormant container shows the raid's state of it; it needs its channel again when that changed or this
            // player searches it.
            LootContainer container = raid.Loot.Containers[index];
            bool current = !Held(container) && DefaultKnowledge(raid, container) == Known(index);
            if (current || _failed.Contains(index) || Push(sender, raid, container))
            {
                _dirty.Remove(index);
            }
        }

        if (now < _relevantAt)
        {
            return;
        }

        _relevantAt = now + PlayerChannels.LootRelevanceInterval;
        int started = 0;
        foreach (LootContainer container in raid.Loot.Relevant(player.RelevancePosition))
        {
            if (_links.Count >= MaxDeliveries || started >= PlayerChannels.MaxLootStartsPerCheck)
            {
                break;
            }

            if (Known(container.Index) != LootKnowledge.None || _links.Contains(container.Index) || _failed.Contains(container.Index)
                || _links.FreeChannel() is not { } channel)
            {
                continue;
            }

            // The class default goes once per client; containers of a class wait for its export.
            ReliableKey? export = _classExports.TryGetValue(container.Archetype, out ReliableKey key) && sender.Pending(key) ? key : null;
            if (!_classExports.ContainsKey(container.Archetype))
            {
                export = _classExports[container.Archetype] = sender.Queue(raid.Loot.ExportBunch(container));
            }

            _links.Set(container.Index, new LootLink(ActorChannelStage.Exporting, channel, export, default));
            started++;
        }

        _links.Advance(sender, new LootSteps(this, raid));
    }

    // The client's copy of a container after it gets this view.
    private static LootKnowledge Knowledge(LootView view) => view.OpenedAt is null ? LootKnowledge.Closed : LootKnowledge.Opened;

    // What a dormant container shows when nobody on this connection searches it.
    private static LootKnowledge DefaultKnowledge(RaidWorld raid, LootContainer container) =>
        raid.LootStates.OpenedAt(container.Index) is null ? LootKnowledge.Closed : LootKnowledge.Opened;

    // Longest time to put the held item away and draw the raider tool.
    private static double ToolDrawSeconds(RaidItems items) => items.FerroI.UnequipSeconds + items.RaiderTool.EquipSeconds;

    // Runs this player's search: names the character's interactor once, starts a requested search (the client
    // lines up, then holds), cancels it on movement, opens the container when the hold ends, and closes it when
    // the player closes the looting menu or walks away. What the player leaves inside stays in the raid's loot state.
    private void ServiceSearch(IPlayerChannelsContext player, ReliableSender sender, double now)
    {
        RaidWorld raid = player.Raid;
        player.ExportInteractor(sender);
        if (_press is { } press && _search is null && _open is null)
        {
            (LootContainer pressed, bool drawsTool) = press;
            LootContainerClass type = pressed.Class;

            // An opened container has no breach left: it opens again once the character is lined up. A press
            // that takes the raider tool out first lines up only once the held item is put away and the tool drawn.
            double? opened = raid.LootStates.OpenedAt(pressed.Index);
            double draw = drawsTool && type.EquipsRaiderTool && opened is null ? ToolDrawSeconds(raid.GameData.Items) : 0;
            double requested = raid.WorldSeconds(now) + draw;
            _search = (pressed, player.InputFrame ?? 0, requested, requested + type.AlignmentSeconds,
                now + draw + type.AlignmentSeconds + (opened is null ? type.HoldSeconds : LootInteractions.OpenedHoldSeconds));
            Push(sender, raid, pressed);
            Searches++;
        }

        _press = null;
        if (_search is { } search)
        {
            (LootContainer container, uint frame, double requestedAt, _, double opensAt) = search;
            if (player.MovedSince(frame))
            {
                _search = null;
                Push(sender, raid, container);
                CancelledSearches++;
            }
            else if (now >= opensAt && player.InteractorExported)
            {
                int id = player.Member.Inventory.OpenWorldContainer(raid.LootStates.Items(container.Index), raid.GameData.Items.UnitWeights);
                raid.LootStates.Open(container.Index, raid.WorldSeconds(now));
                _search = null;
                _open = (container, id);
                Push(sender, raid, container);
                sender.Queue(Bunches.Actor(ChannelLayout.Pawn, raid.State.InteractorCall(player.Player, FunctionNames.ClientLootInteractionHoldCompleted, container.Interaction, container.InteractionClass).Write));
                player.RunInteraction(new PawnInteraction(container.Interaction, LastInteractionEvent.Restaged, requestedAt, requestedAt,
                    requestedAt + container.Class.AlignmentSeconds, player.NextInteractionSequence(), container.Class.SearchLocation, container.SearchAnimation));
                Opens++;
            }
        }

        if (_open is { } open)
        {
            Vector3D prompt = open.Container.Prompt;
            bool away = player.InputPosition is { } position
                && Math.Sqrt(Math.Pow(position.X - prompt.X, 2) + Math.Pow(position.Y - prompt.Y, 2) + Math.Pow(position.Z - prompt.Z, 2))
                    > open.Container.CloseDistance;
            if (away || _closeRequested)
            {
                raid.LootStates.Store(open.Container.Index, player.Member.Inventory.CloseWorldContainer());
                _open = null;
                Push(sender, raid, open.Container);
                player.SucceedInteraction();
                sender.Queue(Bunches.Actor(ChannelLayout.Pawn, raid.State.InteractorCall(player.Player, FunctionNames.ClientEndInteractionSuccessfullyImplementation, open.Container.Interaction, open.Container.InteractionClass).Write));
            }
        }

        _closeRequested = false;
    }

    // Brings the client's copy of a container up to date now: on its open channel, or by opening a channel again
    // for a dormant container. Returns false when no channel is free yet.
    private bool Push(ReliableSender sender, RaidWorld raid, LootContainer container)
    {
        if (_links.TryGet(container.Index, out LootLink link))
        {
            if (link.Stage == ActorChannelStage.Open)
            {
                _links.Set(container.Index, SendState(sender, raid, container, link));
            }
            else
            {
                _dirty.Add(container.Index);
            }

            return true;
        }

        if (Known(container.Index) == LootKnowledge.None || _links.FreeChannel() is not { } channel)
        {
            _dirty.Add(container.Index);
            return false;
        }

        sender.Queue(OpenBunch(sender, raid, container, channel));
        LootView view = ViewOf(raid, container);
        _links.Set(container.Index, new LootLink(ActorChannelStage.Open, channel, sender.Queue(raid.Loot.StateBunch(container, channel, view)), view));
        _known[container.Index] = (byte)Knowledge(view);
        return true;
    }

    // Sends a container's state on its open channel when the client's copy differs.
    private LootLink SendState(ReliableSender sender, RaidWorld raid, LootContainer container, LootLink link)
    {
        LootView view = ViewOf(raid, container);
        if (view == link.Sent)
        {
            return link;
        }

        _known[container.Index] = (byte)Knowledge(view);
        return link with { Key = sender.Queue(raid.Loot.StateBunch(container, link.Channel, view)), Sent = view };
    }

    private ChannelBunch OpenBunch(ReliableSender sender, RaidWorld raid, LootContainer container, int channel)
    {
        ChannelOpens++;
        return raid.Loot.OpenBunch(container, channel, sender.NextSequence(channel), raid.LootStates.OpenedAt(container.Index));
    }

    // What this player's client should show of a container: this player's search or open container, else the
    // raid's state of it.
    private LootView ViewOf(RaidWorld raid, LootContainer container)
    {
        double? opened = raid.LootStates.OpenedAt(container.Index);
        if (_open is { } open && open.Container == container)
        {
            return new LootView(CurrentInteractionState.Ongoing, null, opened, open.ItemContainer);
        }

        if (_search is { } search && search.Container == container)
        {
            return new LootView(CurrentInteractionState.Ongoing, search.HoldStartsAt, opened, null);
        }

        return new LootView(opened is null ? CurrentInteractionState.Idle : CurrentInteractionState.SuccessIdle, null, opened, null);
    }

    private LootKnowledge Known(int index) => index < _known.Length ? (LootKnowledge)_known[index] : LootKnowledge.None;

    // Whether this player searches the container or has it open: its channel stays open meanwhile.
    private bool Held(LootContainer container) => _search?.Container == container || _open?.Container == container;

    // The loot container of a search interaction the client may use: the client has the container.
    private LootContainer? Searchable(RaidWorld raid, ulong interaction) =>
        raid.Loot.ByInteraction(interaction) is { } container && Known(container.Index) != LootKnowledge.None && !_failed.Contains(container.Index) ? container : null;

    // A loot container's channel on this connection: its stage, the bunch it waits for and the state last sent.
    private readonly record struct LootLink(ActorChannelStage Stage, int Channel, ReliableKey? Key, LootView Sent) : IActorChannelLink;

    // What a loot container's channel sends: a container the client does not have yet is exported, spawned and named
    // first; an open channel sends the container's state until the client has the latest, then closes dormant.
    private readonly struct LootSteps(PlayerLoot loot, RaidWorld raid) : IActorChannelSteps<LootLink>
    {
        public bool Waits(ReliableSender sender, int index, LootLink link) => link.Key is { } key && sender.Pending(key);

        public bool Abandons(int index, LootLink link) => false;

        public LootLink Spawn(ReliableSender sender, int index, LootLink link) =>
            link with { Stage = ActorChannelStage.Spawning, Key = sender.Queue(loot.OpenBunch(sender, raid, raid.Loot.Containers[index], link.Channel)) };

        public LootLink Name(ReliableSender sender, int index, LootLink link) =>
            link with { Stage = ActorChannelStage.Naming, Key = sender.Queue(LootCatalog.ComponentExportBunch(raid.Loot.Containers[index])) };

        public LootLink Fill(ReliableSender sender, int index, LootLink link)
        {
            LootContainer container = raid.Loot.Containers[index];
            LootView first = loot.ViewOf(raid, container);
            link = link with { Stage = ActorChannelStage.Open, Key = sender.Queue(raid.Loot.StateBunch(container, link.Channel, first)), Sent = first };
            loot._known[index] = (byte)Knowledge(first);
            return link;
        }

        public LootLink Update(ReliableSender sender, int index, LootLink link, bool waiting)
        {
            LootContainer container = raid.Loot.Containers[index];
            link = loot.SendState(sender, raid, container, link);
            if (link.Key is { } sent && sender.Pending(sent))
            {
                return link;
            }

            // Nothing left to send: the container goes dormant, unless this player is searching it.
            return loot.Held(container)
                ? link
                : link with { Stage = ActorChannelStage.Closing, Key = sender.Queue(Bunches.Close(link.Channel, Bunches.Dormancy)) };
        }

        public void Closed(int index, LootLink link)
        {
            if (loot._failed.Contains(index))
            {
                loot._known[index] = (byte)LootKnowledge.None;
            }
            else if (loot.ViewOf(raid, raid.Loot.Containers[index]) != link.Sent)
            {
                loot._dirty.Add(index);
            }
        }
    }
}
