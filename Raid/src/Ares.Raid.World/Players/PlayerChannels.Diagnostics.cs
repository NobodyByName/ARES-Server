using System.Globalization;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Players;

/// <summary>
/// What tests and operators read of a player's channels: the client's input and the character simulation, and per
/// family of world actors what the client has, what the player did with it and what the server refused.
/// </summary>
public sealed partial class PlayerChannels
{
    /// <summary>Input payloads that could not be decoded (they never reject the packet carrying them).</summary>
    public int InputDecodeFailures { get; private set; }

    /// <summary>Reason the latest undecoded input payload was rejected, if any.</summary>
    public string? LastInputDecodeError { get; private set; }

    /// <summary>The newest weapon controls the client sent: what it holds or wants to equip.</summary>
    public WeaponInput? LatestWeaponInput { get; private set; }

    /// <summary>The object the client focuses in its latest input frame (its prompt shows), or 0.</summary>
    public ulong InputFocus => _input.Focus;

    /// <summary>The character position the client predicted in its latest input frame, if any.</summary>
    public Vector3D? InputPosition => InputFrame is { } frame ? _input.Position(frame) : null;

    /// <summary>The heading of the client's control rotation in its latest input frame, in radians, if any.</summary>
    public double? InputYaw => InputFrame is { } frame ? _input.Yaw(frame) : null;

    /// <summary>Pawn blackboard frames sent (the first frame and focus updates).</summary>
    public int BlackboardFrames => _frames.BlackboardFrames;

    /// <summary>
    /// The character's time offset in its newest frame (see <see cref="PawnBlackboard.TimeOffsetField"/>), or null
    /// before its first frame.
    /// </summary>
    public long? TimeOffset => _frames.TimeOffset;

    /// <summary>The character's simulated state after the latest simulated input frame, or null before the reset frame.</summary>
    public CharacterState? SimulatedCharacter => _frames.Movement?.Latest;

    /// <summary>Input frames the character simulation ran without the client's input.</summary>
    public int MissingInputFrames => _frames.Movement?.MissingFrames ?? 0;

    /// <summary>Simulated frames after which the character continued from the position its client reported.</summary>
    public int PositionReseeds => _frames.Movement?.Reseeds ?? 0;

    /// <summary>The latest such frame: its number, how far the simulation was off and whether it had the character in the air.</summary>
    public (uint Frame, double Distance, bool Airborne)? LastPositionReseed => _frames.Movement?.LastReseed;

    /// <summary>Largest distance the simulation was off on such a frame.</summary>
    public double WorstPositionReseed => _frames.Movement?.WorstReseed ?? 0;

    /// <summary>Door presses applied.</summary>
    public int DoorPresses => _doors.Presses;

    /// <summary>Door requests ignored (not a press, door not open on this connection, or still moving).</summary>
    public int IgnoredDoorRequests => _doors.IgnoredRequests;

    /// <summary>Door breaches started.</summary>
    public int DoorBreaches => _doors.Breaches;

    /// <summary>Door breaches this player stopped by moving.</summary>
    public int CancelledBreaches => _doors.CancelledBreaches;

    /// <summary>Request values received on breach buttons, with their counts (a hold sends more than a press).</summary>
    public IReadOnlyDictionary<int, int> BreachRequests => _doors.BreachRequests;

    /// <summary>Whole-stack inventory swaps accepted.</summary>
    public int InventoryMoves { get; private set; }

    public int ShotsFired => _member.Weapons.ShotsFired;
    public int ReloadsCompleted => _member.Weapons.ReloadsCompleted;

    /// <summary>Accepted requests to draw a quick-use item.</summary>
    public int QuickItemRequests => _member.Weapons.QuickItemRequests;

    /// <summary>Snap Hook throws (each costs gadget power).</summary>
    public int HookThrows => _member.Weapons.HookThrows;

    /// <summary>Times pawn frames started being withheld because a thrown Snap Hook was out.</summary>
    public int QuickItemHolds => _frames.QuickItemHolds;

    /// <summary>The latest such hold: the first and the last withheld frame tick (none while it lasts).</summary>
    public (uint From, uint? To)? LastQuickItemHold => _frames.LastQuickItemHold;

    /// <summary>Inventory requests rejected and corrected.</summary>
    public int RejectedInventoryRequests { get; private set; }

    /// <summary>
    /// The latest rejected inventory requests, oldest first, as type from->to (container r revision/slot) xamount
    /// reason@input frame.
    /// </summary>
    public IReadOnlyList<string> RejectedInventoryLog => _rejectedInventory;

    /// <summary>Ladders whose climbable actor and interactions this client has.</summary>
    public int LaddersReady => _ladders.Ready;

    /// <summary>Ladders whose delivery started and has not finished.</summary>
    public int LaddersInProgress => _ladders.InProgress;

    /// <summary>Loot containers whose actor and search interaction this client has.</summary>
    public int LootReady => _loot.Ready;

    /// <summary>Whether this client has a loot container (it keeps it while the container is dormant).</summary>
    /// <param name="container">Container index.</param>
    /// <returns>True once the container's state reached the client.</returns>
    public bool HasLoot(int container) => _loot.Has(container);

    /// <summary>Loot channels open now (or on the way): containers being delivered or updated.</summary>
    public int LootChannelsInUse => _loot.ChannelsInUse;

    /// <summary>Loot channel opens so far: first deliveries and state changes.</summary>
    public int LootChannelOpens => _loot.ChannelOpens;

    /// <summary>Loot containers whose channel the client closed: they are not delivered again to this player.</summary>
    public int LootFailed => _loot.Failed;

    /// <summary>
    /// The first loot containers whose channel the client closed, oldest first, as source:class@stage (the step whose
    /// bunch the channel was on: Spawning = the open, Naming = the component names, Open = a state).
    /// </summary>
    public IReadOnlyList<string> LootFailures => _loot.Failures;

    /// <summary>Loot containers relevant to the player that the client does not have yet.</summary>
    public int LootWaiting => _loot.Waiting(this);

    /// <summary>First openings of the raid's containers with what they held, oldest first.</summary>
    public IReadOnlyList<string> LootEvents => _raid.LootStates.Events;

    /// <summary>Describes a container of the raid and what it holds now.</summary>
    /// <param name="index">Container index.</param>
    /// <returns>One line.</returns>
    public string DescribeLoot(int index) => _raid.LootStates.Describe(index);

    /// <summary>Searches started on loot containers.</summary>
    public int LootSearches => _loot.Searches;

    /// <summary>Searches the player stopped by moving.</summary>
    public int CancelledSearches => _loot.CancelledSearches;

    /// <summary>Loot containers opened by this player.</summary>
    public int LootOpens => _loot.Opens;

    /// <summary>Requests received on loot search interactions, oldest first, as request@input frame.</summary>
    public IReadOnlyList<string> LootRequests => _loot.Requests;

    /// <summary>Items this player dropped into the world.</summary>
    public int ItemsDropped => _pickups.ItemsDropped;

    /// <summary>Pickup requests of this player that took items.</summary>
    public int PickupsTaken => _pickups.Taken;

    /// <summary>Pickups whose actor and interaction this client has.</summary>
    public int PickupsReady => _pickups.Ready;

    /// <summary>Whether this client has a pickup (it keeps it while the pickup is dormant).</summary>
    /// <param name="pickup">Pickup index.</param>
    /// <returns>True once the pickup reached the client and until it is destroyed there.</returns>
    public bool HasPickup(int pickup) => _pickups.Has(pickup);

    /// <summary>Pickup channels open now (or on the way).</summary>
    public int PickupChannelsInUse => _pickups.ChannelsInUse;

    /// <summary>Requests received on pickup interactions, oldest first, as pickup:request@input frame.</summary>
    public IReadOnlyList<string> PickupRequests => _pickups.Requests;

    /// <summary>What happened to the raid's pickups, oldest first.</summary>
    public IReadOnlyList<string> PickupEvents => _raid.Pickups.Events;

    /// <summary>ARCs whose actor and ability system this client has.</summary>
    public int ArcsReady => _arcs.Ready;

    /// <summary>Whether this client has an ARC.</summary>
    /// <param name="arc">ARC index.</param>
    /// <returns>True from the ability system's content until the channel closes.</returns>
    public bool HasArc(int arc) => _arcs.Has(arc);

    /// <summary>ARC channels open now (or on the way).</summary>
    public int ArcChannelsInUse => _arcs.ChannelsInUse;

    /// <summary>ARCs whose channel the client refused, as index@stage.</summary>
    public IReadOnlyList<string> ArcFailures => _arcs.Failures;

    /// <summary>Extraction elevators whose state and buttons this client has.</summary>
    public int ElevatorsReady => _elevators.Ready;

    /// <summary>
    /// Elevator states this client has, by elevator index: the state (Called while the call press runs) and the
    /// server world time it started, with the descend switch held (+switch@press, ~hold@hold start once the hold runs)
    /// or, once the elevator left, the one that sent it (^switch).
    /// </summary>
    public string ElevatorStates => _elevators.States;

    /// <summary>Calls of an elevator by this player.</summary>
    public int ElevatorCalls => _elevators.Calls;

    /// <summary>Elevator state changes this client got, oldest first, as elevator:state@server time (see <see cref="ElevatorStates"/>).</summary>
    public IReadOnlyList<string> ElevatorChanges => _elevators.Changes;

    /// <summary>Descend holds started by this player.</summary>
    public int ElevatorDescents => _elevators.Descents;

    /// <summary>Descend holds this player stopped by moving.</summary>
    public int CancelledDescents => _elevators.CancelledDescents;

    /// <summary>Descend holds of this player that sent an elevator off.</summary>
    public int CompletedDescents => _elevators.CompletedDescents;

    /// <summary>
    /// Players inside each elevator when it last left (its doors closed), as elevator@server time the doors started
    /// closing:user ids.
    /// </summary>
    public string ElevatorDepartures => string.Join(", ", _raid.Extraction.Points
        .Select(point => (point.Index, Departure: _raid.ExtractionStates.Departure(point.Index)))
        .Where(entry => entry.Departure is not null)
        .Select(entry => $"{entry.Index}@{entry.Departure!.At.ToString("0.000", CultureInfo.InvariantCulture)}:{string.Join(' ', entry.Departure.Players)}"));

    /// <summary>
    /// This player's departure with an elevator, as elevator@server time it left, then each end-of-round step with its
    /// server time: transition (the client starts its end of round), ready (the client is ready for it), end (the end
    /// of round sent) or fallback (no ready in time: sent back to the main menu). Empty before a departure.
    /// </summary>
    public string Departure => _departure.Summary;

    /// <summary>Requests received on elevator buttons, oldest first, as elevator/button request@input frame.</summary>
    public IReadOnlyList<string> ElevatorRequests => _elevators.Requests;

    /// <summary>
    /// Presses on no target or on an object the server does not know, oldest first, as target request@input frame.
    /// </summary>
    public IReadOnlyList<string> UnknownInteractionRequests => _unknownRequests;

    /// <summary>Client calls received and not handled yet, by name.</summary>
    public IReadOnlyDictionary<string, int> IgnoredCalls => _ignoredCalls;
}
