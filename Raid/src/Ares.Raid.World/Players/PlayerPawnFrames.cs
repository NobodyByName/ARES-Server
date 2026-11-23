using Ares.Raid.Transport;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World.Players;

/// <summary>
/// The character's pawn frames on a player's connection: the blackboard frames that give the client its character's
/// state (with the held items created and linked first), the character simulation they follow, the holds that keep
/// frames back while the client plays an interaction or a thrown Snap Hook, the server-run interaction they show, and
/// the character's movement as other players get it.
/// </summary>
internal sealed class PlayerPawnFrames
{
    private ReliableKey[] _blackboardKeys = [];
    private IReadOnlyDictionary<int, PendulumSlot> _blackboardState = new Dictionary<int, PendulumSlot>();
    private ulong _blackboardFocus;
    private double _framesHeldUntil;
    private uint _weaponVersion;
    private bool _populatedInventorySent;
    private bool _weaponCreationSent;
    private bool _weaponItemsCreated;
    private ReliableKey[] _weaponCreationKeys = [];
    private bool _weaponReferencesSent;
    private ReliableKey[] _weaponReferenceKeys = [];
    private uint _inventoryFrame;
    private uint _inventoryVersion = 1;
    private ContainerItem? _shield;

    // The interaction the server runs for the character (a container search, a descend hold), times in raid world
    // seconds.
    private PawnInteraction? _interaction;
    private uint _interactionCount;
    private ReliableKey? _interactorExport;

    /// <summary>Pawn blackboard frames sent (the first frame and focus updates).</summary>
    public int BlackboardFrames { get; private set; }

    /// <summary>
    /// The character's time offset in its newest frame (see <see cref="PawnBlackboard.TimeOffsetField"/>), or null
    /// before its first frame.
    /// </summary>
    public long? TimeOffset { get; private set; }

    /// <summary>Times pawn frames started being withheld because a thrown Snap Hook was out.</summary>
    public int QuickItemHolds { get; private set; }

    /// <summary>The latest such hold: the first and the last withheld frame tick (none while it lasts).</summary>
    public (uint From, uint? To)? LastQuickItemHold { get; private set; }

    /// <summary>The input tick of the newest frame sent, or 0 before the first frame started the character simulation.</summary>
    public uint Frame { get; private set; }

    /// <summary>The character simulation, from the first frame on.</summary>
    public PlayerMovement? Movement { get; private set; }

    /// <summary>Whether the client has the held items created and linked to their item objects.</summary>
    public bool ItemsLinked { get; private set; }

    /// <summary>Whether the client has the character's interactor named, so it takes the interaction calls on it.</summary>
    public bool InteractorExported { get; private set; }

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PlayerPawnFrames Copy()
    {
        PlayerPawnFrames copy = (PlayerPawnFrames)MemberwiseClone();
        copy.Movement = Movement?.Copy();
        return copy;
    }

    /// <summary>Whether the newest frame waits for the client's acknowledgement.</summary>
    /// <param name="sender">The connection's sender.</param>
    /// <returns>True while it does.</returns>
    public bool Pending(ReliableSender sender) => _blackboardKeys.Any(sender.Pending);

    /// <summary>The client got the inventory's shield: frames send it again once it changes.</summary>
    /// <param name="shield">The shield the client has.</param>
    public void ShieldSent(ContainerItem? shield) => _shield = shield;

    /// <summary>A request of an input frame changed the inventory: no pawn frame before that frame goes out.</summary>
    /// <param name="frame">Input frame.</param>
    public void InventoryChangedAt(uint frame) => _inventoryFrame = Math.Max(_inventoryFrame, frame);

    /// <summary>Sends no pawn frame before a time (replaces an earlier hold).</summary>
    /// <param name="until">Raid clock.</param>
    public void HoldFrames(double until) => _framesHeldUntil = until;

    /// <summary>Sends no pawn frame before a time (keeps a later hold).</summary>
    /// <param name="until">Raid clock.</param>
    public void ExtendFrameHold(double until) => _framesHeldUntil = Math.Max(_framesHeldUntil, until);

    /// <summary>The frames show an interaction the server runs for the character (replaces an earlier one).</summary>
    /// <param name="interaction">The interaction, times in raid world seconds.</param>
    public void RunInteraction(PawnInteraction interaction) => _interaction = interaction;

    /// <summary>The server-run interaction, if any, ended successfully: the frames show it succeeded.</summary>
    public void SucceedInteraction() =>
        _interaction = _interaction is { } ended ? ended with { Event = LastInteractionEvent.Succeeded } : null;

    /// <summary>Numbers a new server-run interaction.</summary>
    /// <returns>Its sequence number.</returns>
    public uint NextInteractionSequence() => ++_interactionCount;

    /// <summary>Names the character's interactor to the client once; it counts as named once acknowledged.</summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="sender">The connection's sender.</param>
    public void ExportInteractor(IPlayerChannelsContext player, ReliableSender sender)
    {
        if (InteractorExported)
        {
            return;
        }

        if (_interactorExport is null)
        {
            _interactorExport = sender.Queue(Bunches.Exports(player.Raid.State.InteractorExports(player.Player), [player.Player.Interactor]));
        }
        else if (!sender.Pending(_interactorExport.Value))
        {
            InteractorExported = true;
        }
    }

    /// <summary>
    /// The character's opening frame: the client's character simulation turns the character to the facing of the state
    /// it starts from, so the character opens with the spawn state. The first frame built from the client's input then
    /// follows as an ordinary update.
    /// </summary>
    /// <param name="player">The player's channels.</param>
    /// <returns>The frame.</returns>
    public ActorContentBlock SpawnFrame(IPlayerChannelsContext player)
    {
        RaidWorld raid = player.Raid;
        RaidPlayer member = player.Member;
        WorldActor pawn = raid.State.Actor(player.Player.Pawn);
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(raid.GameData.Data, PawnBlackboardFrames.FirstTick,
            new PawnMotion(pawn.Location, Yaw: pawn.Rotation.Yaw * Math.PI / 180), 0, member.Inventory.InitialBlackboard,
            member.Weapons.Variables(PawnBlackboardFrames.FirstTick), TimeOffsetAt(raid, PawnBlackboardFrames.FirstTick, raid.Clock()));
        uint delivery = checked((uint)BlackboardFrames);
        ActorContentBlock frame = raid.State.BlackboardFrame(delivery, delivery + 1, state, _blackboardState);
        _blackboardState = state;
        BlackboardFrames++;
        return frame;
    }

    /// <summary>
    /// Sends the character's frames. The first frame starts the client's character simulation at the spawn after the
    /// client confirmed its initial levels loaded and possession completed, with the slot-1 gun already in hand (a raid
    /// never starts unarmed; its item link follows once the item exists). The client applies each later frame as its
    /// full character state, so later frames carry the movement the client itself predicted (accepted as is): input
    /// frame N reports the position before tick N, so a frame describes the tick before the latest input. They are
    /// sent when the usable focused button changes: the client's interaction (and its animation) only starts once the
    /// server says it may use the button.
    /// </summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="input">The client's input frames.</param>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="now">Raid clock.</param>
    public void Service(IPlayerChannelsContext player, InputHistory input, ReliableSender sender, double now)
    {
        if (player.InputFrame is not { } latest || latest is < PawnBlackboardFrames.FirstTick or > PawnBlackboardFrames.LastTick || _blackboardKeys.Any(sender.Pending))
        {
            return;
        }

        RaidWorld raid = player.Raid;
        RaidPlayer member = player.Member;
        WorldPlayer actors = player.Player;

        // Item initialization caches its inventory owner even when FindSlotOf fails. Registering
        // before populated containers arrive leaves the item permanently assigned to an invalid slot.
        if (_populatedInventorySent && actors.HeldItems.Count > 0 && !ItemsLinked)
        {
            if (!_weaponCreationSent)
            {
                _weaponCreationKeys = [.. raid.State.WeaponItemCreation(actors, member.Inventory)
                    .Select(item => sender.Queue(Bunches.Actor(ChannelLayout.Pawn, item.Write)))];
                _weaponCreationSent = true;
                return;
            }

            if (!_weaponItemsCreated)
            {
                if (_weaponCreationKeys.Any(sender.Pending))
                {
                    return;
                }

                _weaponItemsCreated = true;
            }

            if (!_weaponReferencesSent)
            {
                _weaponReferenceKeys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, raid.State.WeaponItemReferences(actors).Write))];
                _weaponReferencesSent = true;
                return;
            }

            if (_weaponReferenceKeys.Any(sender.Pending))
            {
                return;
            }

            ItemsLinked = true;
            _weaponVersion = uint.MaxValue;
        }

        IReadOnlyDictionary<Guid, ulong>? weaponObjects = ItemsLinked ? actors.HeldItems : null;
        IReadOnlyDictionary<int, PendulumSlot> state;
        uint frame = latest;
        ulong focus = 0;
        bool initializingContainers = Frame == 0;
        if (Frame == 0)
        {
            WorldActor pawn = raid.State.Actor(actors.Pawn);
            double yaw = pawn.Rotation.Yaw * Math.PI / 180;
            state = PawnBlackboardFrames.State(raid.GameData.Data, frame, new PawnMotion(pawn.Location, Yaw: yaw), focus,
                member.Inventory.InitialBlackboard, member.Weapons.Variables(frame, weaponObjects), TimeOffsetAt(raid, frame, now),
                emptySlots: member.Weapons.EmptyItemSlots(frame));
            Movement = new PlayerMovement(raid.GameData.Data, frame, pawn.Location, yaw, raid.Collision);
        }
        else
        {
            if (InteractionFramesHeld(player, now))
            {
                return;
            }

            if (QuickItemHeld(member, latest - 1))
            {
                return;
            }

            focus = player.FocusUsable(input.Focus) ? input.Focus : 0;
            frame = latest - 1;
            bool inventoryChanged = _inventoryVersion != member.Inventory.Version;
            bool weaponsChanged = _weaponVersion != member.Weapons.Version;
            if ((!inventoryChanged && !weaponsChanged && focus == _blackboardFocus) || frame <= Frame || frame < _inventoryFrame
                || input.Position(latest) is not { } position || input.Position(frame) is not { } before || input.Yaw(frame) is not { } yaw
                || Movement?.State(frame) is not { } character || character.Stance == CharacterStance.Slide || character.SlideExitTicks > 0
                || character.Roll is not null || character.Vaulting || character.Unsettled || character.SnapHook.Busy)
            {
                // A frame lacks the state of the client's own slide, roll, vault, landing and Snap Hook nodes: it would end a
                // slide, a roll, a vault, a landing recovery or a hook's flight and pull, and drop a jumping or falling character
                // to the ground. Wait until the character is back in ordinary ground movement.
                return;
            }

            // The character's interactor shows the server-run interaction (an open container is still its search).
            state = PawnBlackboardFrames.State(raid.GameData.Data, frame, Motion(character, position, before, yaw, input.ActionRotation(frame)), focus, member.Inventory.Blackboard,
                member.Weapons.Variables(frame, weaponObjects), TimeOffsetAt(raid, frame, now),
                _interaction is { } interaction ? OnCharacterClock(interaction) : null, member.Weapons.EmptyItemSlots(frame));
        }

        // Delivery numbers address the client's snapshot ring; the graph inside carries the input tick.
        uint delivery = checked((uint)BlackboardFrames);
        List<ReliableKey> keys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, raid.State.BlackboardFrame(delivery, delivery + 1, state, _blackboardState)))];
        if (_shield != member.Inventory.Shield)
        {
            keys.Add(sender.Queue(Bunches.Actor(ChannelLayout.Pawn, raid.State.ShieldContent(actors, member.Inventory).Write)));
            _shield = member.Inventory.Shield;
        }

        _blackboardKeys = [.. keys];
        (Frame, _blackboardState, _blackboardFocus) = (frame, state, focus);
        _inventoryVersion = initializingContainers ? 1 : member.Inventory.Version;
        _weaponVersion = member.Weapons.Version;
        _populatedInventorySent = !initializingContainers;
        BlackboardFrames++;
    }

    /// <summary>Sends a frame that puts the character at rest at a place (an operator teleport).</summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="sender">The connection's sender.</param>
    /// <param name="latest">The client's latest input frame: the frame's tick.</param>
    /// <param name="position">Destination at the character's feet.</param>
    /// <param name="yaw">Heading in radians.</param>
    /// <param name="now">Raid clock.</param>
    public void SendRest(IPlayerChannelsContext player, ReliableSender sender, uint latest, Vector3D position, double yaw, double now)
    {
        RaidWorld raid = player.Raid;
        RaidPlayer member = player.Member;
        CharacterState rest = new(latest, position, Movement?.Latest.Flashlight ?? false) { Yaw = yaw };
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(raid.GameData.Data, latest, new PawnMotion(position, default, yaw, rest), 0,
            member.Inventory.Blackboard, member.Weapons.Variables(latest, ItemsLinked ? player.Player.HeldItems : null), TimeOffsetAt(raid, latest, now),
            emptySlots: member.Weapons.EmptyItemSlots(latest));
        uint delivery = checked((uint)BlackboardFrames);
        List<ReliableKey> keys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, raid.State.BlackboardFrame(delivery, delivery + 1, state, _blackboardState)))];
        if (_shield != member.Inventory.Shield)
        {
            keys.Add(sender.Queue(Bunches.Actor(ChannelLayout.Pawn, raid.State.ShieldContent(player.Player, member.Inventory).Write)));
            _shield = member.Inventory.Shield;
        }

        _blackboardKeys = [.. keys];
        (Frame, _blackboardState, _blackboardFocus) = (latest, state, 0);
        _inventoryVersion = member.Inventory.Version;
        _weaponVersion = member.Weapons.Version;
        _populatedInventorySent = true;
        BlackboardFrames++;
    }

    /// <summary>
    /// Pawn frames carry no interaction state, so the client's own one (requested, holding, succeeded) is reset by each
    /// frame; none are sent while this player breaches and just after it succeeds.
    /// </summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="now">Raid clock.</param>
    /// <returns>True while frames are held.</returns>
    public bool InteractionFramesHeld(IPlayerChannelsContext player, double now) => player.InteractionRunning || now < _framesHeldUntil;

    /// <summary>
    /// Gives other players this character's movement: the input tick before the newest input, at the position the
    /// client reported, with the simulated velocity, stance and locomotion (as the character's own frames carry it),
    /// the held weapons and the aim pitch. Other clients have no item objects of this player, so the weapons go
    /// without item links: the client draws an equipped gun or gadget from its item type. Other clients only show the
    /// character, so its movement goes out through a Snap Hook's throw and pull too, while this player's own frames are
    /// held: the Snap Hook's node carries the hook (other clients draw it and its cable from it), and the move target
    /// and pull node the pulled pose.
    /// </summary>
    /// <param name="player">The player's channels.</param>
    /// <param name="input">The client's input frames.</param>
    /// <param name="now">Raid clock.</param>
    public void PublishMotion(IPlayerChannelsContext player, InputHistory input, double now)
    {
        if (player.Phase != DeliveryPhase.Possessed || Frame == 0 || player.InputFrame is not { } latest || latest <= PawnBlackboardFrames.FirstTick)
        {
            return;
        }

        RaidPlayer member = player.Member;
        uint tick = latest - 1;
        if ((member.Motion is { } published && published.Tick >= tick) || !member.Weapons.Describable(tick) || input.Position(latest) is not { } position
            || input.Position(tick) is not { } before || input.Yaw(tick) is not { } yaw || Movement?.State(tick) is not { } character
            || input.Pitch(tick) is not { } pitch)
        {
            return;
        }

        IReadOnlyList<PendulumVariable> weapons = member.Weapons.ObservedVariables(tick, character.SnapHook);
        member.Publish(new PlayerMotion(tick, Motion(character, position, before, yaw, input.ActionRotation(tick)),
            [.. weapons, .. character.SnapHook.ObservedNodes(tick, weapons)], pitch, TimeOffsetAt(player.Raid, tick, now)));
    }

    // While a thrown Snap Hook is out (its flight and pull, until the client may use it again) the client keeps its own
    // character state: a frame would end them. Raid keeps following its movement reports.
    // Inventory and focus changes go out with the first frame after the hold.
    private bool QuickItemHeld(RaidPlayer member, uint frame)
    {
        bool held = member.Weapons.HoldsFrames(frame);
        switch (held)
        {
            case true when LastQuickItemHold is not { To: null }:
                QuickItemHolds++;
                LastQuickItemHold = (frame, null);
                break;
            case false when LastQuickItemHold is { To: null } hold:
                LastQuickItemHold = (hold.From, frame - 1);
                break;
        }

        return held;
    }

    // A server-run interaction with its times moved from world time to the character's clock.
    private PawnInteraction OnCharacterClock(PawnInteraction interaction)
    {
        double offset = (double)(TimeOffset ?? 0) / PawnBlackboard.TimePerSecond;
        return interaction with
        {
            RequestedAt = interaction.RequestedAt - offset,
            AlignedAt = interaction.AlignedAt - offset,
            StartedAt = interaction.StartedAt - offset,
        };
    }

    // A tick's movement for a frame: the simulated body when the simulation moves it, otherwise the position the
    // client reported after the tick with the height change it shows.
    private static PawnMotion Motion(CharacterState character, Vector3D reported, Vector3D before, double yaw, PendulumQuaternion? actionRotation) =>
        character.Body is null
            ? new PawnMotion(reported, character.Velocity with { Z = (reported.Z - before.Z) / PawnBlackboard.TickSeconds }, yaw, character, actionRotation)
            : new PawnMotion(character.Position, character.Velocity, yaw, character, actionRotation);

    // The character's time offset: a tick's simulation time plus the offset is the raid's world time, which is
    // also the server time on this raid's packets. The client's ticks drift against the raid's clock; like the
    // game, the offset follows in whole steps, so it stays put while the drift is under one step.
    private long TimeOffsetAt(RaidWorld raid, uint tick, double now)
    {
        long ideal = (long)Math.Round(raid.WorldSeconds(now) * PawnBlackboard.TimePerSecond) - ((long)tick * PawnBlackboard.TimePerTick);
        long offset = TimeOffset is { } current
            ? current + (PawnBlackboard.TimeOffsetStep * ((ideal - current) / PawnBlackboard.TimeOffsetStep))
            : ideal - (ideal % PawnBlackboard.TimePerTick);
        TimeOffset = offset;
        return offset;
    }
}
