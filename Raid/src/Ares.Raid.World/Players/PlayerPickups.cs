using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;

namespace Ares.Raid.World.Players;

/// <summary>
/// The items lying in the raid as one player sees them: drops placed into the world, pickups relevant to the player
/// delivered on their own channels (dormant once the client is up to date, woken when they change), destroyed for the
/// client once taken, and the player's pickup requests.
/// </summary>
internal sealed class PlayerPickups : IPlayerFamily
{
    private readonly ChannelRange _channels;

    // The channels that carry pickups for this client; a pickup holds one only while it is delivered or changes.
    private ActorChannelPool<PickupLink> _links;

    // The pickups this client has (also while dormant) with the revision it has of each.
    private Dictionary<int, int> _known = [];
    private Dictionary<ulong, ReliableKey> _exports = [];
    private HashSet<int> _failed = [];
    private double _relevantAt;
    private BoundedLog _requests = new(PlayerChannels.InteractionRequestLog);

    /// <summary>Creates the pickups of a connection that has none yet.</summary>
    /// <param name="channels">The channels the pickups share.</param>
    public PlayerPickups(ChannelRange channels)
    {
        _channels = channels;
        _links = new ActorChannelPool<PickupLink>(channels);
    }

    /// <summary>Items this player dropped into the world.</summary>
    public int ItemsDropped { get; private set; }

    /// <summary>Pickup requests of this player that took items.</summary>
    public int Taken { get; private set; }

    /// <summary>Pickups whose actor and interaction this client has.</summary>
    public int Ready => _known.Count;

    /// <summary>Pickup channels open now (or on the way).</summary>
    public int ChannelsInUse => _links.Count;

    /// <summary>Requests received on pickup interactions, oldest first, as pickup:request@input frame.</summary>
    public IReadOnlyList<string> Requests => _requests;

    /// <summary>Whether this client has a pickup (it keeps it while the pickup is dormant).</summary>
    /// <param name="pickup">Pickup index.</param>
    /// <returns>True once the pickup reached the client and until it is destroyed there.</returns>
    public bool Has(int pickup) => _known.ContainsKey(pickup);

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerPickups Copy()
    {
        PlayerPickups copy = (PlayerPickups)MemberwiseClone();
        copy._links = _links.Copy();
        copy._known = new Dictionary<int, int>(_known);
        copy._exports = new Dictionary<ulong, ReliableKey>(_exports);
        copy._failed = [.. _failed];
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

        foreach ((int pickup, PickupLink link) in _links)
        {
            if (link.Channel == channel)
            {
                _failed.Add(pickup);
            }
        }

        return true;
    }

    /// <summary>Puts the items accepted drops removed from the inventory into the world.</summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="now">Raid clock.</param>
    public void PlaceDrops(IPlayerChannelsContext player, double now)
    {
        RaidPlayer member = player.Member;
        while (member.Inventory.TryTakeDropped(out ContainerItem item))
        {
            (Vector3D location, Rotator rotation) = DropPlacement(player);
            player.Raid.Pickups.Drop(member.Id, (int)player.Raid.State.Actor(player.Player.PlayerState).Properties[PropertyNames.PlayerId]!, item, location, rotation,
                player.Raid.WorldSeconds(now));
            ItemsDropped++;
        }
    }

    /// <inheritdoc />
    public bool TryHandle(IPlayerChannelsContext player, InteractionRequest request, bool repeated, double now)
    {
        // A press on a pickup's interaction (once per held press) takes what fits into the inventory.
        if (PickableUp(player.Raid, request.Target) is not { } pickup)
        {
            return false;
        }

        if (repeated)
        {
            return true;
        }

        _requests.Add($"{pickup.Index}:{request.Request}@{request.Frame}");
        if ((request.Request & PendulumInput.PressRequest) == 0
            || player.Raid.Pickups.PickUp(pickup.Index, player.Member.Id, player.Member.Inventory, player.Player.Pawn, now) <= 0)
        {
            return true;
        }

        Taken++;
        player.InventoryChangedAt(request.Frame);
        return true;
    }

    /// <inheritdoc />
    public bool Usable(IPlayerChannelsContext player, ulong target) => PickableUp(player.Raid, target) is not null;

    /// <inheritdoc />
    public void Service(IPlayerChannelsContext player, ReliableSender sender, double now)
    {
        RaidWorld raid = player.Raid;
        _links.Advance(sender, new PickupSteps(this, raid));

        // A dormant pickup that changed opens again with its whole state; one destroyed is opened and closed for good.
        foreach ((int index, int revision) in _known.ToArray())
        {
            WorldPickup pickup = raid.Pickups.All[index];
            if (_links.Contains(index) || (!pickup.Destroyed && pickup.Revision == revision))
            {
                continue;
            }

            if (_links.FreeChannel() is not { } channel)
            {
                break;
            }

            if (pickup.Destroyed)
            {
                _known.Remove(index);
                _links.Set(index, new PickupLink(ActorChannelStage.Closing, channel, sender.Queue(WorldPickups.DestroyBunch(pickup, channel)), revision));
                continue;
            }

            sender.Queue(raid.Pickups.OpenBunch(pickup, channel, sender.NextSequence(channel)));
            _links.Set(index, new PickupLink(ActorChannelStage.Open, channel, sender.Queue(raid.Pickups.InteractionBunch(pickup, channel)), pickup.Revision));
            _known[index] = pickup.Revision;
        }

        if (now < _relevantAt)
        {
            return;
        }

        _relevantAt = now + PlayerChannels.LootRelevanceInterval;
        int started = 0;
        foreach (WorldPickup pickup in raid.Pickups.Relevant(player.RelevancePosition))
        {
            if (started >= PlayerChannels.MaxLootStartsPerCheck)
            {
                break;
            }

            if (_known.ContainsKey(pickup.Index) || _links.Contains(pickup.Index) || _failed.Contains(pickup.Index)
                || _links.FreeChannel() is not { } channel)
            {
                continue;
            }

            // The class default and each item's data asset go once per client.
            ulong[] missing = [.. Roots(raid, pickup).Where(root => !_exports.ContainsKey(root))];
            if (missing.Length > 0)
            {
                ReliableKey key = sender.Queue(raid.Pickups.ExportBunch(missing));
                foreach (ulong root in missing)
                {
                    _exports[root] = key;
                }
            }

            _links.Set(pickup.Index, new PickupLink(ActorChannelStage.Exporting, channel, null, pickup.Revision));
            started++;
        }

        _links.Advance(sender, new PickupSteps(this, raid));
    }

    // Where a dropped item appears: ahead of the character along its facing, at the height of its capsule centre, in
    // whole centimetres as its replicated movement carries it, turned with the character. The reported position is at
    // the feet; before the first report the character stands on its spawn point, the spawn capsule centre lowered by
    // the capsule's half height.
    private static (Vector3D Location, Rotator Rotation) DropPlacement(IPlayerChannelsContext player)
    {
        RaidPlayer member = player.Member;
        double halfHeight = player.Raid.GameData.Map.PlayerStartCapsule.HalfHeight;
        Vector3D feet = player.InputPosition
            ?? member.Spawn.Location with { Z = member.Spawn.Location.Z - halfHeight };
        double yaw = player.InputYaw is { } radians ? radians * 180 / Math.PI : member.Spawn.Yaw;
        double heading = yaw * Math.PI / 180;
        Vector3D ahead = new(
            feet.X + (Pickups.DropDistance * Math.Cos(heading)),
            feet.Y + (Pickups.DropDistance * Math.Sin(heading)),
            feet.Z + halfHeight);
        return (new Vector3D(Math.Round(ahead.X), Math.Round(ahead.Y), Math.Round(ahead.Z)), new Rotator(0, yaw, 0));
    }

    // The static objects a pickup's spawn needs: the class default and the item's data asset.
    private static ulong[] Roots(RaidWorld raid, WorldPickup pickup) => [raid.Pickups.Archetype, pickup.DataAsset];

    // The pickup an interaction target names, when this client has it.
    private WorldPickup? PickableUp(RaidWorld raid, ulong target) =>
        raid.Pickups.ByTarget(target) is { } pickup && _known.ContainsKey(pickup.Index) && !_failed.Contains(pickup.Index) ? pickup : null;

    // A pickup's channel on this connection: its stage, the bunch it waits for and the pickup revision it sent.
    private readonly record struct PickupLink(ActorChannelStage Stage, int Channel, ReliableKey? Key, int Sent) : IActorChannelLink;

    // What a pickup's channel sends: a pickup the client does not have yet is exported, spawned and named first; an
    // open channel sends the pickup's changes until the client has the latest, then closes dormant; a channel closes
    // for good when the pickup is destroyed.
    private readonly struct PickupSteps(PlayerPickups pickups, RaidWorld raid) : IActorChannelSteps<PickupLink>
    {
        public bool Waits(ReliableSender sender, int index, PickupLink link)
        {
            if (link.Stage != ActorChannelStage.Exporting)
            {
                return link.Key is { } key && sender.Pending(key);
            }

            Dictionary<ulong, ReliableKey> exports = pickups._exports;
            return Roots(raid, raid.Pickups.All[index]).Any(root => sender.Pending(exports[root]));
        }

        // Taken before its channel opened: this client never gets it.
        public bool Abandons(int index, PickupLink link) =>
            link.Stage == ActorChannelStage.Exporting && raid.Pickups.All[index] is { PickedUpBy: not null } or { Destroyed: true };

        public PickupLink Spawn(ReliableSender sender, int index, PickupLink link)
        {
            WorldPickup pickup = raid.Pickups.All[index];
            return link with
            {
                Stage = ActorChannelStage.Spawning,
                Key = sender.Queue(raid.Pickups.OpenBunch(pickup, link.Channel, sender.NextSequence(link.Channel))),
                Sent = pickup.Revision,
            };
        }

        public PickupLink Name(ReliableSender sender, int index, PickupLink link) =>
            link with { Stage = ActorChannelStage.Naming, Key = sender.Queue(WorldPickups.ComponentExportBunch(raid.Pickups.All[index])) };

        public PickupLink Fill(ReliableSender sender, int index, PickupLink link)
        {
            link = link with { Stage = ActorChannelStage.Open, Key = sender.Queue(raid.Pickups.InteractionBunch(raid.Pickups.All[index], link.Channel)) };
            pickups._known[index] = link.Sent;
            return link;
        }

        public PickupLink Update(ReliableSender sender, int index, PickupLink link, bool waiting)
        {
            WorldPickup pickup = raid.Pickups.All[index];
            if (pickup.Destroyed)
            {
                pickups._known.Remove(index);
                return link with { Stage = ActorChannelStage.Closing, Key = sender.Queue(Bunches.Close(link.Channel, Bunches.Destroyed)) };
            }

            if (pickup.Revision == link.Sent)
            {
                // Nothing left to send: the pickup goes dormant.
                return waiting ? link : link with { Stage = ActorChannelStage.Closing, Key = sender.Queue(Bunches.Close(link.Channel, Bunches.Dormancy)) };
            }

            link = link with { Key = sender.Queue(raid.Pickups.UpdateBunch(pickup, link.Channel)), Sent = pickup.Revision };
            pickups._known[index] = pickup.Revision;
            return link;
        }

        public void Closed(int index, PickupLink link)
        {
            if (pickups._failed.Contains(index))
            {
                pickups._known.Remove(index);
            }
        }
    }
}
