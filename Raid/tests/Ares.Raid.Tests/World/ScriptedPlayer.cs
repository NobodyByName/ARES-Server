using System.Globalization;
using Ares.Common.Game.Inventory;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

// Plays one joined player against the world channels: acknowledges every packet at once and records the
// bunches the server sends.
internal sealed class ScriptedPlayer
{
    private const string MatchId = "0b7c6c7e-6a0e-4c43-9c1e-3f1b8a3f2d41";
    private const int Seed = 123456789;
    private const string Lighting = "Lighting.GreyMorning";

    private const int SenderSeed = 300;
    private const int ReceiverSeed = 500;

    private readonly double[] _now = [100.0];
    private int _packet = SenderSeed;
    private int _clientSequence = ReceiverSeed & (ChannelBunch.SequenceCount - 1);
    private int _pawnSequence = ReceiverSeed & (ChannelBunch.SequenceCount - 1);
    private ulong _focus;
    private Vector3D? _position;
    private readonly Dictionary<uint, sbyte> _movementInputs = [];
    private readonly Dictionary<uint, InventoryCommand> _inventoryInputs = [];
    private readonly Dictionary<uint, ulong> _weaponInputs = [];

    public ScriptedPlayer(IReadOnlyList<CompactPlayerProfile>? profiles = null, PlayerLoadout? loadout = null)
    {
        Worlds = new DamWorlds(() => _now[0], Lighting, () => Seed);
        Principal = new SessionPrincipal(42, MatchId);
        Worlds.Prepare(MatchId, [42], loadouts: loadout is null ? null : [loadout], profiles: profiles);
        Io = new ChannelIo(new ReliableSender(SenderSeed), new ReliableReceiver(ReceiverSeed));
        Channels = Start();
    }

    // Another party of one joining the raid of an earlier scripted player; both share its clock.
    public ScriptedPlayer(ScriptedPlayer host, long userId, PlayerLoadout? loadout = null)
    {
        _now = host._now;
        Worlds = host.Worlds;
        Principal = new SessionPrincipal(userId, MatchId);
        Worlds.Join(MatchId, [userId], loadouts: loadout is null ? null : [loadout]);
        Io = new ChannelIo(new ReliableSender(SenderSeed), new ReliableReceiver(ReceiverSeed));
        Channels = Start();
    }

    public SessionPrincipal Principal { get; }

    private UniqueNetId UniqueId => new(5, Principal.UserId.ToString(CultureInfo.InvariantCulture), "", false, false);

    public DamWorlds Worlds { get; }

    public RaidWorld Raid => Worlds.Current!;

    public RaidPlayer Member => Raid.Player(Principal.UserId)!;

    public WorldPlayer Player => Raid.State.Player(Principal);

    private PlayerChannels Start() => (PlayerChannels)Worlds.StartPlayer(Principal, new PlayerLogin(7, 0, UniqueId), Io);

    // The first door a press can open; Possess makes its cell visible.
    public Door Door => Raid.Doors.Doors.First(door => Raid.DoorStates.Usable(door.Index));

    // A door a press could open, in a cell that stays hidden.
    public Door HiddenDoor => Raid.Doors.Doors.Last(door => Raid.DoorStates.Usable(door.Index) && door.Cell != Door.Cell);

    public ChannelIo Io { get; }

    public PlayerChannels Channels { get; }

    public List<string> Trace { get; } = [];

    public List<ChannelBunch> Sent { get; } = [];

    private double Now
    {
        get => _now[0];
        set => _now[0] = value;
    }

    // Whether the client reports the next packet as not received.
    public bool LoseNextPacket { get; set; }

    public bool HoldAcknowledgements { get; set; }

    public void AcknowledgeSent() =>
        Channels.Acknowledge(Io, new NotificationHeader(1, (_packet - 1) & PacketSequence.Mask, [uint.MaxValue]));

    public void Flush(string label)
    {
        Trace.Add($"# {label}");
        while (Io.Sender.Select(Now, 8000) is { Count: > 0 } keys)
        {
            foreach (ChannelBunch bunch in Io.Sender.Bunches(keys))
            {
                Sent.Add(bunch);
                if (bunch is { Channel: 6, Opened: true })
                {
                    _spawnTimeOffset = Channels.TimeOffset ?? 0;
                }

                string close = bunch.CloseReason?.ToString(CultureInfo.InvariantCulture) ?? "None";
                Trace.Add($"{bunch.Channel}|{bunch.Sequence}|{(bunch.Opened ? 1 : 0)}|{close}|{(bunch.PackageExports ? 1 : 0)}|{(bunch.MustBeMapped ? 1 : 0)}|{bunch.PayloadBits}|{Convert.ToHexStringLower(bunch.Payload.AsSpan(0, (int)((bunch.PayloadBits + 7) / 8)))}");
            }

            Io.Sender.Sent(_packet, keys, Now);
            if (!HoldAcknowledgements)
            {
                Channels.Acknowledge(Io, new NotificationHeader(1, _packet, [LoseNextPacket ? uint.MaxValue - 1 : uint.MaxValue]));
            }

            LoseNextPacket = false;
            _packet = (_packet + 1) & PacketSequence.Mask;
        }
    }

    public void Service(double at)
    {
        Now = at;
        Channels.Service(Io, at);
        Flush($"service {at.ToString("0.0", CultureInfo.InvariantCulture)}");
    }

    // A reliable call from the client on its controller channel.
    public void Call(string function, Dictionary<string, object?> parameters)
    {
        Channels.Receive(Io, ControllerCall(function, parameters));
        Channels.Commit();
    }

    public ChannelBunch ControllerCall(string function, Dictionary<string, object?> parameters)
    {
        WorldPlayer player = Player;
        RpcCodec calls = Raid.State.Replication.Calls(Raid.State.Objects([player.Controller, player.PlayerState, player.Pawn, Raid.State.GameState.Guid]));
        ActorContentBlock block = calls.Call(PlayerClasses.PlayerController.Class, function, parameters, RpcDirection.Server);
        BitWriter writer = new();
        block.Write(writer);
        _clientSequence = (_clientSequence + 1) & (ChannelBunch.SequenceCount - 1);
        return new ChannelBunch(2, writer.ToArray(), writer.Length) { Name = ChannelName.Actor, Sequence = _clientSequence };
    }

    // A reliable call from the client on a component of its controller (channel 2) or character (any other
    // channel) that the client names by path (the server has not replicated it).
    public void ComponentCall(int channel, ulong outer, string component)
    {
        BitWriter writer = new();
        writer.WriteBit(false);
        writer.WriteBit(false);
        NetGuid.Write(writer, 1);
        writer.WriteByte(1);
        NetGuid.Write(writer, outer);
        writer.WriteString(component, 256);
        writer.WritePacked(8);
        writer.WriteByte(0x5a);
        ref int sequence = ref channel == 2 ? ref _clientSequence : ref _pawnSequence;
        sequence = (sequence + 1) & (ChannelBunch.SequenceCount - 1);
        Channels.Receive(Io, new ChannelBunch(channel, writer.ToArray(), writer.Length) { Name = ChannelName.Actor, Sequence = sequence });
        Channels.Commit();
    }

    public void ReportVisible(params (string Package, bool Visible, bool Client)[] levels) =>
        Call(FunctionNames.ServerUpdateMultipleLevelsVisibility, new Dictionary<string, object?>
        {
            [PropertyNames.LevelVisibilities] = levels.Select(object? (level, index) => new LevelVisibility(level.Package, level.Package, level.Visible, false, new VisibilityTransaction(index == 0 ? 1 : 0, level.Client))).ToArray(),
        });

    public void Input(uint baseline, uint latest, ulong? press = null, ulong focus = 0, Vector3D? position = null, InventoryCommand? inventory = null, ulong weaponFlags = 0, sbyte? forward = null, int request = PendulumInput.PressRequest)
    {
        if (forward is { } axis)
        {
            _movementInputs[latest] = axis;
        }

        if (inventory is not null)
        {
            _inventoryInputs[latest] = inventory;
        }

        _weaponInputs[latest] = weaponFlags;
        Channels.ObserveDirect(PendulumInput.DirectStreamKind, InputPayload(baseline, latest, press, (_focus, focus), (_position, position), _inventoryInputs, _weaponInputs, _movementInputs, request));
        (_focus, _position) = (focus, position);
        Channels.Commit();
    }

    public static byte[] InventoryPayload(uint baseline, uint latest, InventoryCommand command) =>
        InputPayload(baseline, latest, null, (0, 0), (null, null), new Dictionary<uint, InventoryCommand> { [latest] = command });

    // The client's input history: a header and the last frames, every slot at its descriptor default
    // (the new frames optionally report a position and focus a button, and the latest frame optionally presses one).
    private static byte[] InputPayload(uint baseline, uint latest, ulong? press, (ulong Baseline, ulong Latest) focus, (Vector3D? Baseline, Vector3D? Latest) reported, Dictionary<uint, InventoryCommand> inventory,
        Dictionary<uint, ulong>? weapons = null, Dictionary<uint, sbyte>? movement = null, int request = PendulumInput.PressRequest)
    {
        BitWriter writer = new();
        writer.WriteBit(true);
        writer.WriteBounded(baseline, PendulumFrames.BaselineRange);
        PendulumFrames.WriteRelative(writer, latest, baseline);
        Dictionary<int, PendulumSlot> previous = baseline == 0 ? [] : Slots(baseline);
        for (uint frame = Math.Max(baseline + 1, latest - 2); frame <= latest; frame++)
        {
            Dictionary<int, PendulumSlot> current = Slots(frame);
            PendulumCodec.Default.WriteFrame(writer, current.Values, previous);
            previous = current;
        }

        return writer.ToArray();

        Dictionary<int, PendulumSlot> Slots(uint frame)
        {
            Dictionary<int, PendulumSlot> slots = [];
            int position = 0;
            for (int slot = 0; slot < PendulumInput.SlotDescriptors.Count; slot++)
            {
                int descriptor = PendulumInput.SlotDescriptors[slot];
                byte[] value = PendulumCodec.Default.DefaultValue(descriptor);
                switch (slot)
                {
                    case InventoryInput.Slot when inventory.TryGetValue(frame, out InventoryCommand? command):
                        value = PendulumCodec.Default.Value(descriptor, [
                            new PendulumIntegerValue(InventoryInput.CommandField, (ulong)command.Type),
                            new PendulumIntegerValue(InventoryInput.FromContainerField, (uint)command.From.Parent.Id ^ 0x80000000U),
                            new PendulumIntegerValue(InventoryInput.ToContainerField, (uint)command.To.Parent.Id ^ 0x80000000U),
                            new PendulumIntegerValue(InventoryInput.FromRevisionField, command.From.Parent.Revision),
                            new PendulumIntegerValue(InventoryInput.ToRevisionField, command.To.Parent.Revision),
                            new PendulumIntegerValue(InventoryInput.FromSlotField, command.From.Index),
                            new PendulumIntegerValue(InventoryInput.ToSlotField, command.To.Index),
                        ]);
                        break;
                    case PendulumInput.InteractionSlot:
                        List<PendulumValue> claim = [new PendulumIntegerValue(PendulumInput.InteractionFocusField, frame == baseline ? focus.Baseline : focus.Latest)];
                        if (press is { } target && frame == latest)
                        {
                            claim.Add(new PendulumIntegerValue(PendulumInput.InteractionRequestField, (ulong)request));
                            claim.Add(new PendulumIntegerValue(PendulumInput.InteractionTargetField, target));
                        }

                        value = PendulumCodec.Default.Value(descriptor, claim);
                        break;
                    case PendulumInput.MovementSlot when movement is not null && movement.TryGetValue(frame, out sbyte forward):
                        value = PendulumCodec.Default.Value(descriptor, [
                            new PendulumIntegerValue(PendulumInput.ForwardField, (ulong)(forward + PendulumInput.MovementAxisBias)),
                            new PendulumIntegerValue(PendulumInput.RightField, PendulumInput.MovementAxisBias),
                        ]);
                        break;
                    case PendulumInput.PositionSlot when (frame == baseline ? reported.Baseline : reported.Latest) is { } at:
                        value = PendulumCodec.Default.Value(descriptor, [
                            Quantized(descriptor, PendulumInput.PositionXField, at.X),
                            Quantized(descriptor, PendulumInput.PositionYField, at.Y),
                            Quantized(descriptor, PendulumInput.PositionZField, at.Z),
                        ]);
                        break;
                }

                if (slot == WeaponProtocol.InputSlot && weapons is not null && weapons.TryGetValue(frame, out ulong flags))
                {
                    PendulumCodec.Apply(PendulumRegistry.Descriptors[descriptor].Fields, value,
                        [new PendulumIntegerValue(WeaponProtocol.InputFlagsField, flags)]);
                }

                int size = (value.Length + 255) / 256;
                slots[slot] = new PendulumSlot(slot, position, size, descriptor, value);
                position += size;
            }

            return slots;
        }
    }

    // The state the character opens with: the spawn frame inside the pawn's opening bunch.
    public Dictionary<int, PendulumSlot> SpawnState => new(PawnBlackboardFrames.State(PawnBlackboardFrames.FirstTick,
        new PawnMotion(Member.Spawn.Location, Yaw: Member.Spawn.Yaw * Math.PI / 180), 0, Member.Inventory.InitialBlackboard,
        Member.Weapons.Variables(PawnBlackboardFrames.FirstTick), _spawnTimeOffset));

    // The time offset of the spawn frame, kept when the character's opening bunch is sent.
    private long _spawnTimeOffset;

    public (uint Baseline, uint Latest) LastFrameNumbers { get; private set; }

    // The one pawn blackboard frame on a character's channel (the player's own by default) among the bunches sent
    // since the last clear, decoded against the state of the frame before it (empty for a full frame) with the codec
    // the receiving client uses (the owner's by default); its baseline and frame numbers are kept in LastFrameNumbers.
    public Dictionary<int, PendulumSlot> PawnFrame(Dictionary<int, PendulumSlot> previous, int channel = 6, PendulumCodec? codec = null)
    {
        ChannelBunch bunch = Assert.Single(Sent, item => item.Channel == channel && (item.Payload[0] & 3) == 2);
        ActorContentBlock block = ActorContentBlock.Read(new BitReader(bunch.Payload, bunch.PayloadBits));
        NetFieldTable fields = Raid.State.Replication.Calls().Fields(PlayerClasses.Pawn.Class);
        ActorFieldPayload payload = ActorFieldPayload.Read(new BitReader(block.Payload, block.PayloadBits), fields.Count);
        Assert.Equal(PropertyNames.BlackBoardReplicationPtr, fields.FieldAt(payload.Index).Name);
        BitReader reader = new(payload.Payload, payload.PayloadBits);
        Assert.True(reader.ReadBit());
        BlackboardUpdateHeader header = BlackboardUpdate.ReadHeader(reader);
        Assert.Equal(previous.Count == 0, header.Reset);
        LastFrameNumbers = (header.Baseline, header.Latest);
        // Like the client, read the body with the field set the header names.
        PendulumCodec decoder = header.Fields == PendulumFieldFlags.Others ? PendulumCodec.Others : PendulumCodec.Default;
        Assert.Same(codec ?? PendulumCodec.Default, decoder);
        Assert.Equal(decoder.Fields, header.Fields);
        Dictionary<int, PendulumSlot> result = decoder.ReadFrame(reader, previous);
        Assert.Equal(0, reader.Remaining);
        return result;
    }

    private static PendulumIntegerValue Quantized(int descriptor, int field, double world)
    {
        PendulumScalarField scalar = (PendulumScalarField)PendulumRegistry.Descriptors[descriptor].Fields[field];
        return new PendulumIntegerValue(field, (ulong)Math.Round((world + PawnBlackboard.PositionRange) * (scalar.Maximum - 1) / (2 * PawnBlackboard.PositionRange)));
    }

    // Join, possession, the map and the usable door's cell visible, and the first input frames.
    public void Possess()
    {
        Flush("start");
        Call(FunctionNames.ServerNotifyHasLoadedInitialLevels, []);
        Flush("initial levels loaded");
        Call(FunctionNames.ServerAcknowledgePossession, new Dictionary<string, object?> { [PropertyNames.P] = Player.Pawn });
        ReportVisible((DamMap.MapPackage, true, true), (Door.Cell, true, false));
        Flush("possession and visibility");
        Input(0, 30);
    }

    /// <summary>Frames sent until the held items are linked: spawn, first input, filled containers, item links.</summary>
    public const int SettledFrames = 4;

    // Standing inputs from the given frame until the first input frame, the filled containers and the linked held
    // items are delivered; returns the next input frame and service time.
    public (uint Frame, double At) Settle(uint frame, double at, Vector3D? position = null)
    {
        for (int step = 0; step < 10 && Channels.BlackboardFrames < SettledFrames; step++)
        {
            Input(frame, frame + 3, position: position ?? Member.Spawn.Location);
            Service(at);
            (frame, at) = (frame + 3, at + 0.1);
        }

        Assert.Equal(SettledFrames, Channels.BlackboardFrames);
        return (frame, at);
    }
}
