using Ares.Raid.Replication;
using Ares.Raid.Transport;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;

namespace Ares.Raid.World;

/// <summary>
/// The items lying in the raid as this player sees them: drops placed into the world, pickups relevant to the player
/// delivered on their own channels (dormant once the client is up to date, woken when they change), destroyed for the
/// client once taken, and this player's pickup requests.
/// </summary>
public sealed partial class PlayerChannels
{
    /// <summary>Channels that carry pickups for this client; a pickup holds one only while it is delivered or changes.</summary>
    private const int PickupChannels = 32;

    private Dictionary<int, PickupLink> _pickupLinks = [];

    // The pickups this client has (also while dormant) with the revision it has of each.
    private Dictionary<int, int> _pickupKnown = [];
    private Dictionary<ulong, ReliableKey> _pickupExports = [];
    private HashSet<int> _failedPickups = [];
    private double _pickupRelevantAt;
    private List<string> _pickupRequests = [];

    // Where a pickup's channel stands: a pickup the client does not have yet is exported, spawned and named first;
    // an open channel sends the pickup's changes until the client has the latest, then closes dormant; a channel
    // closes for good when the pickup is destroyed.
    private enum PickupStage
    {
        Exporting,
        Spawning,
        Naming,
        Open,
        Closing,
    }

    /// <summary>Items this player dropped into the world.</summary>
    public int ItemsDropped { get; private set; }

    /// <summary>Pickup requests of this player that took items.</summary>
    public int PickupsTaken { get; private set; }

    /// <summary>Pickups whose actor and interaction this client has.</summary>
    public int PickupsReady => _pickupKnown.Count;

    /// <summary>Whether this client has a pickup (it keeps it while the pickup is dormant).</summary>
    /// <param name="pickup">Pickup index.</param>
    /// <returns>True once the pickup reached the client and until it is destroyed there.</returns>
    public bool HasPickup(int pickup) => _pickupKnown.ContainsKey(pickup);

    /// <summary>Pickup channels open now (or on the way).</summary>
    public int PickupChannelsInUse => _pickupLinks.Count;

    /// <summary>Requests received on pickup interactions, oldest first, as pickup:request@input frame.</summary>
    public IReadOnlyList<string> PickupRequests => _pickupRequests;

    /// <summary>What happened to the raid's pickups, oldest first.</summary>
    public IReadOnlyList<string> PickupEvents => _raid.Pickups.Events;

    // The first pickup channel: after every player's observed channels, for as many players as the raid holds.
    private int FirstPickupChannel => FirstObservedChannel + (2 * _raid.MaxPlayers);

    private void CopyPickups(PlayerChannels copy)
    {
        copy._pickupLinks = new Dictionary<int, PickupLink>(_pickupLinks);
        copy._pickupKnown = new Dictionary<int, int>(_pickupKnown);
        copy._pickupExports = new Dictionary<ulong, ReliableKey>(_pickupExports);
        copy._failedPickups = [.. _failedPickups];
        copy._pickupRequests = [.. _pickupRequests];
    }

    private int? PickupIndex(int channel) =>
        channel - FirstPickupChannel is var index and >= 0 and < PickupChannels ? index : null;

    private void PickupChannelFailed(int channel)
    {
        foreach ((int pickup, PickupLink link) in _pickupLinks)
        {
            if (link.Channel == channel)
            {
                _failedPickups.Add(pickup);
            }
        }
    }

    // Puts the items accepted drops removed from the inventory into the world.
    private void PlaceDrops(double now)
    {
        while (_member.Inventory.TryTakeDropped(out ContainerItem item))
        {
            (Vector3D location, Rotator rotation) = DropPlacement();
            _raid.Pickups.Drop(_member.Id, (int)Actor(_player.PlayerState).Properties[PropertyNames.PlayerId]!, item, location, rotation, ServerSeconds(now));
            ItemsDropped++;
        }
    }

    // Where a dropped item appears: ahead of the character along its facing, at the height of its capsule centre, in
    // whole centimetres as its replicated movement carries it, turned with the character. The reported position is at
    // the feet; before the first report the character stands on its spawn point, the spawn capsule centre lowered by
    // the capsule's half height.
    private (Vector3D Location, Rotator Rotation) DropPlacement()
    {
        Vector3D feet = InputFrame is { } latest && _input.Position(latest) is { } reported
            ? reported
            : _member.Spawn.Location with { Z = _member.Spawn.Location.Z - DamPlayerStarts.Capsule.HalfHeight };
        double yaw = InputYaw is { } radians ? radians * 180 / Math.PI : _member.Spawn.Yaw;
        double heading = yaw * Math.PI / 180;
        Vector3D ahead = new(
            feet.X + (Pickups.DropDistance * Math.Cos(heading)),
            feet.Y + (Pickups.DropDistance * Math.Sin(heading)),
            feet.Z + DamPlayerStarts.Capsule.HalfHeight);
        return (new Vector3D(Math.Round(ahead.X), Math.Round(ahead.Y), Math.Round(ahead.Z)), new Rotator(0, yaw, 0));
    }

    // Runs a request on a pickup's interaction: a press (once per held press) takes what fits into the inventory.
    private void RequestPickup(WorldPickup pickup, InteractionRequest request, bool repeated, double now)
    {
        if (repeated)
        {
            return;
        }

        LogRequest(_pickupRequests, $"{pickup.Index}:{request.Request}@{request.Frame}");
        if ((request.Request & PendulumInput.PressRequest) == 0
            || _raid.Pickups.PickUp(pickup.Index, _member.Id, _member.Inventory, _player.Pawn, now) <= 0)
        {
            return;
        }

        PickupsTaken++;
        _inventoryFrame = Math.Max(_inventoryFrame, request.Frame);
    }

    // The pickup an interaction target names, when this client has it.
    private WorldPickup? PickableUp(ulong target) =>
        _raid.Pickups.ByTarget(target) is { } pickup && _pickupKnown.ContainsKey(pickup.Index) && !_failedPickups.Contains(pickup.Index) ? pickup : null;

    private void ServicePickups(ReliableSender sender, double now)
    {
        if (!MapActorsAllowed(now))
        {
            return;
        }

        ServicePickupLinks(sender);

        // A dormant pickup that changed opens again with its whole state; one destroyed is opened and closed for good.
        foreach ((int index, int revision) in _pickupKnown.ToArray())
        {
            WorldPickup pickup = _raid.Pickups.All[index];
            if (_pickupLinks.ContainsKey(index) || (!pickup.Destroyed && pickup.Revision == revision))
            {
                continue;
            }

            if (FreePickupChannel() is not { } channel)
            {
                break;
            }

            if (pickup.Destroyed)
            {
                _pickupKnown.Remove(index);
                _pickupLinks[index] = new PickupLink(PickupStage.Closing, channel, sender.Queue(WorldPickups.DestroyBunch(pickup, channel)), revision);
                continue;
            }

            sender.Queue(_raid.Pickups.OpenBunch(pickup, channel, sender.NextSequence(channel)));
            _pickupLinks[index] = new PickupLink(PickupStage.Open, channel, sender.Queue(_raid.Pickups.InteractionBunch(pickup, channel)), pickup.Revision);
            _pickupKnown[index] = pickup.Revision;
        }

        if (now < _pickupRelevantAt)
        {
            return;
        }

        _pickupRelevantAt = now + LootRelevanceInterval;
        int started = 0;
        foreach (WorldPickup pickup in _raid.Pickups.Relevant(LootPosition))
        {
            if (started >= MaxLootStartsPerCheck)
            {
                break;
            }

            if (_pickupKnown.ContainsKey(pickup.Index) || _pickupLinks.ContainsKey(pickup.Index) || _failedPickups.Contains(pickup.Index)
                || FreePickupChannel() is not { } channel)
            {
                continue;
            }

            // The class default and each item's data asset go once per client.
            ulong[] missing = [.. PickupRoots(pickup).Where(root => !_pickupExports.ContainsKey(root))];
            if (missing.Length > 0)
            {
                ReliableKey key = sender.Queue(_raid.Pickups.ExportBunch(missing));
                foreach (ulong root in missing)
                {
                    _pickupExports[root] = key;
                }
            }

            _pickupLinks[pickup.Index] = new PickupLink(PickupStage.Exporting, channel, null, pickup.Revision);
            started++;
        }

        ServicePickupLinks(sender);
    }

    // Moves each pickup channel on by the steps whose bunches the client has.
    private void ServicePickupLinks(ReliableSender sender)
    {
        foreach ((int index, PickupLink current) in _pickupLinks.ToArray())
        {
            PickupLink link = current;
            WorldPickup pickup = _raid.Pickups.All[index];
            bool waiting = link.Stage == PickupStage.Exporting
                ? PickupRoots(pickup).Any(root => sender.Pending(_pickupExports[root]))
                : link.Key is { } key && sender.Pending(key);
            if (waiting && link.Stage != PickupStage.Open)
            {
                continue;
            }

            switch (link.Stage)
            {
                case PickupStage.Exporting when pickup.PickedUpBy is not null || pickup.Destroyed:
                    // Taken before its channel opened: this client never gets it.
                    _pickupLinks.Remove(index);
                    continue;
                case PickupStage.Exporting:
                    link = link with
                    {
                        Stage = PickupStage.Spawning,
                        Key = sender.Queue(_raid.Pickups.OpenBunch(pickup, link.Channel, sender.NextSequence(link.Channel))),
                        Sent = pickup.Revision,
                    };
                    break;
                case PickupStage.Spawning:
                    link = link with { Stage = PickupStage.Naming, Key = sender.Queue(WorldPickups.ComponentExportBunch(pickup)) };
                    break;
                case PickupStage.Naming:
                    link = link with { Stage = PickupStage.Open, Key = sender.Queue(_raid.Pickups.InteractionBunch(pickup, link.Channel)) };
                    _pickupKnown[index] = link.Sent;
                    break;
                case PickupStage.Open when pickup.Destroyed:
                    _pickupKnown.Remove(index);
                    link = link with { Stage = PickupStage.Closing, Key = sender.Queue(Bunches.Close(link.Channel, Bunches.Destroyed)) };
                    break;
                case PickupStage.Open when pickup.Revision != link.Sent:
                    link = link with { Key = sender.Queue(_raid.Pickups.UpdateBunch(pickup, link.Channel)), Sent = pickup.Revision };
                    _pickupKnown[index] = pickup.Revision;
                    break;
                case PickupStage.Open when !waiting:
                    // Nothing left to send: the pickup goes dormant.
                    link = link with { Stage = PickupStage.Closing, Key = sender.Queue(Bunches.Close(link.Channel, Bunches.Dormancy)) };
                    break;
                case PickupStage.Open:
                    break;
                case PickupStage.Closing:
                    _pickupLinks.Remove(index);
                    if (_failedPickups.Contains(index))
                    {
                        _pickupKnown.Remove(index);
                    }

                    continue;
            }

            _pickupLinks[index] = link;
        }
    }

    // The static objects a pickup's spawn needs: the class default and the item's data asset.
    private ulong[] PickupRoots(WorldPickup pickup) => [_raid.Pickups.Archetype, pickup.DataAsset];

    // A pickup channel no pickup uses.
    private int? FreePickupChannel()
    {
        for (int channel = FirstPickupChannel; channel < FirstPickupChannel + PickupChannels; channel++)
        {
            if (_pickupLinks.Values.All(link => link.Channel != channel))
            {
                return channel;
            }
        }

        return null;
    }

    // A pickup's channel on this connection: its stage, the bunch it waits for and the pickup revision it sent.
    private readonly record struct PickupLink(PickupStage Stage, int Channel, ReliableKey? Key, int Sent);
}
