using System.Globalization;
using Ares.Common.Game.Inventory;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
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

    public ScriptedPlayer(IReadOnlyList<CompactPlayerProfile>? profiles = null, PlayerLoadout? loadout = null)
    {
        Worlds = new DamWorlds(() => _now[0], Lighting, () => Seed);
        Worlds.Prepare(MatchId, [42], loadouts: loadout is null ? null : [loadout], profiles: profiles);
        Io = new ChannelIo(new ReliableSender(SenderSeed), new ReliableReceiver(ReceiverSeed));
        Channels = (PlayerChannels)Worlds.StartPlayer(Principal, new PlayerLogin(7, 0, UniqueId), Io);
    }

    private static SessionPrincipal Principal { get; } = new(42, MatchId);

    private static UniqueNetId UniqueId { get; } = new(5, "42", "", false, false);

    private DamWorlds Worlds { get; }

    public RaidWorld Raid => Worlds.Current!;

    public WorldPlayer Player => Raid.State.Player(Principal);

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

    public void Flush(string label)
    {
        Trace.Add($"# {label}");
        while (Io.Sender.Select(Now, 8000) is { Count: > 0 } keys)
        {
            foreach (ChannelBunch bunch in Io.Sender.Bunches(keys))
            {
                Sent.Add(bunch);
                string close = bunch.CloseReason?.ToString(CultureInfo.InvariantCulture) ?? "None";
                Trace.Add($"{bunch.Channel}|{bunch.Sequence}|{(bunch.Opened ? 1 : 0)}|{close}|{(bunch.PackageExports ? 1 : 0)}|{(bunch.MustBeMapped ? 1 : 0)}|{bunch.PayloadBits}|{Convert.ToHexStringLower(bunch.Payload.AsSpan(0, (int)((bunch.PayloadBits + 7) / 8)))}");
            }

            Io.Sender.Sent(_packet, keys, Now);
            Channels.Acknowledge(Io, new NotificationHeader(1, _packet, [uint.MaxValue]));
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
        WorldPlayer player = Player;
        RpcCodec calls = Raid.State.Replication.Calls(Raid.State.Objects([player.Controller, player.PlayerState, player.Pawn, Raid.State.GameState.Guid]));
        ActorContentBlock block = calls.Call(PlayerClasses.PlayerController.Class, function, parameters, RpcDirection.Server);
        BitWriter writer = new();
        block.Write(writer);
        _clientSequence = (_clientSequence + 1) & (ChannelBunch.SequenceCount - 1);
        Channels.Receive(Io, new ChannelBunch(2, writer.ToArray(), writer.Length) { Name = ChannelName.Actor, Sequence = _clientSequence });
        Channels.Commit();
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

    public void Input(uint baseline, uint latest, ulong? press = null, ulong focus = 0, Vector3D? position = null)
    {
        Channels.ObserveDirect(PendulumInput.DirectStreamKind, InputPayload(baseline, latest, press, (_focus, focus), (_position, position)));
        (_focus, _position) = (focus, position);
        Channels.Commit();
    }

    // The client's input history: a header and the last frames, every slot at its descriptor default
    // (the new frames optionally report a position and focus a button, and the latest frame optionally presses one).
    private static byte[] InputPayload(uint baseline, uint latest, ulong? press, (ulong Baseline, ulong Latest) focus, (Vector3D? Baseline, Vector3D? Latest) reported)
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
                    case PendulumInput.InteractionSlot:
                        List<PendulumValue> claim = [new PendulumIntegerValue(PendulumInput.InteractionFocusField, frame == baseline ? focus.Baseline : focus.Latest)];
                        if (press is { } target && frame == latest)
                        {
                            claim.Add(new PendulumIntegerValue(PendulumInput.InteractionRequestField, PendulumInput.PressRequest));
                            claim.Add(new PendulumIntegerValue(PendulumInput.InteractionTargetField, target));
                        }

                        value = PendulumCodec.Default.Value(descriptor, claim);
                        break;
                    case PendulumInput.PositionSlot when (frame == baseline ? reported.Baseline : reported.Latest) is { } at:
                        value = PendulumCodec.Default.Value(descriptor, [
                            Quantized(descriptor, PendulumInput.PositionXField, at.X),
                            Quantized(descriptor, PendulumInput.PositionYField, at.Y),
                            Quantized(descriptor, PendulumInput.PositionZField, at.Z),
                        ]);
                        break;
                }

                int size = (value.Length + 255) / 256;
                slots[slot] = new PendulumSlot(slot, position, size, descriptor, value);
                position += size;
            }

            return slots;
        }
    }

    private static PendulumIntegerValue Quantized(int descriptor, int field, double world)
    {
        uint half = ((PendulumScalarField)PendulumRegistry.Descriptors[descriptor].Fields[field]).Maximum / 2;
        return new PendulumIntegerValue(field, (ulong)(half + Math.Round(world * half / PawnBlackboard.PositionRange)));
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
}
