using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>A button press the client asked for in an input frame.</summary>
/// <param name="Frame">Input frame.</param>
/// <param name="Target">The pressed object (a button id).</param>
/// <param name="Request">The request.</param>
public readonly record struct InteractionRequest(uint Frame, ulong Target, int Request);

/// <summary>An inventory request associated with the input frame that performed it.</summary>
/// <param name="Frame">Input frame.</param>
/// <param name="Command">The requested operation.</param>
public readonly record struct InventoryRequest(uint Frame, InventoryCommand Command);

/// <summary>
/// A connection's client input history. Each payload carries the newest frames coded against a frame the
/// server acknowledged; a malformed payload, or one coded against an unknown frame, changes nothing.
/// </summary>
public sealed class InputHistory
{
    private const int FramesPerPayload = 3;
    private const uint KeptFrames = 64;

    private static readonly IReadOnlyList<PendulumField> s_claimFields =
        PendulumRegistry.Descriptors[PendulumInput.SlotDescriptors[PendulumInput.InteractionSlot]].Fields;

    private static readonly IReadOnlyList<PendulumField> s_positionFields =
        PendulumRegistry.Descriptors[PendulumInput.SlotDescriptors[PendulumInput.PositionSlot]].Fields;

    private static readonly PendulumQuaternionField s_rotationField = (PendulumQuaternionField)
        PendulumRegistry.Descriptors[PendulumInput.SlotDescriptors[PendulumInput.ControlRotationSlot]].Fields[PendulumInput.ControlRotationField];

    private Dictionary<uint, Dictionary<int, PendulumSlot>> _frames = new() { [0] = [] };

    /// <summary>The object the client focuses in its latest frame (its prompt shows), or 0.</summary>
    public ulong Focus { get; private set; }

    /// <summary>New weapon input frames from the last successfully decoded payload.</summary>
    public IReadOnlyList<WeaponInput> Weapons { get; private set; } = [];

    /// <summary>Latest frame received.</summary>
    private uint Latest { get; set; }

    /// <summary>A copy for speculative packet processing (frames are not changed after they are stored).</summary>
    /// <returns>The copy.</returns>
    public InputHistory Copy() => new() { _frames = new Dictionary<uint, Dictionary<int, PendulumSlot>>(_frames), Latest = Latest, Focus = Focus, Weapons = Weapons };

    /// <summary>The character position the client predicted for a frame it sent.</summary>
    /// <param name="frame">Input frame.</param>
    /// <returns>The world position, or null when the frame is unknown or carries no position yet.</returns>
    public Vector3D? Position(uint frame)
    {
        if (!_frames.TryGetValue(frame, out Dictionary<int, PendulumSlot>? slots)
            || !slots.TryGetValue(PendulumInput.PositionSlot, out PendulumSlot? slot)
            || slot.DescriptorId != PendulumInput.SlotDescriptors[PendulumInput.PositionSlot])
        {
            return null;
        }

        int[] fields = [PendulumInput.PositionXField, PendulumInput.PositionYField, PendulumInput.PositionZField];
        PendulumScalarField[] scalars = [.. fields.Select(field => (PendulumScalarField)s_positionFields[field])];
        ulong[] values = [.. scalars.Select(field => PendulumCodec.Integer(field, slot.Value))];
        if (values.Zip(scalars).All(pair => pair.First == pair.Second.Default))
        {
            return null;
        }

        // The lowest and highest representable integers map to the two world-range endpoints.
        double[] world = [.. values.Zip(scalars, (value, field) => value * (2 * PawnBlackboard.PositionRange / (field.Maximum - 1.0)) - PawnBlackboard.PositionRange)];
        return new Vector3D(world[0], world[1], world[2]);
    }

    /// <summary>The heading of the client's control rotation in a frame it sent (the character faces it).</summary>
    /// <param name="frame">Input frame.</param>
    /// <returns>The heading in radians, or null when the frame is unknown.</returns>
    public double? Yaw(uint frame) =>
        _frames.TryGetValue(frame, out Dictionary<int, PendulumSlot>? slots)
        && slots.TryGetValue(PendulumInput.ControlRotationSlot, out PendulumSlot? slot)
        && slot.DescriptorId == PendulumInput.SlotDescriptors[PendulumInput.ControlRotationSlot]
            ? PendulumRotations.Yaw(PendulumCodec.Quaternion(s_rotationField, slot.Value), s_rotationField)
            : null;

    /// <summary>The pitch of the client's control rotation in a frame it sent (where it aims up or down).</summary>
    /// <param name="frame">Input frame.</param>
    /// <returns>The pitch in radians, or null when the frame is unknown.</returns>
    public double? Pitch(uint frame) =>
        _frames.TryGetValue(frame, out Dictionary<int, PendulumSlot>? slots)
        && slots.TryGetValue(PendulumInput.ControlRotationSlot, out PendulumSlot? slot)
        && slot.DescriptorId == PendulumInput.SlotDescriptors[PendulumInput.ControlRotationSlot]
            ? PendulumRotations.Pitch(PendulumCodec.Quaternion(s_rotationField, slot.Value), s_rotationField)
            : null;

    /// <summary>The movement axes and buttons of a frame the client sent.</summary>
    /// <param name="frame">Input frame.</param>
    /// <returns>The movement input, or null when the frame is unknown.</returns>
    public MovementInput? Movement(uint frame) =>
        _frames.TryGetValue(frame, out Dictionary<int, PendulumSlot>? slots)
        && slots.TryGetValue(PendulumInput.MovementSlot, out PendulumSlot? slot)
        && slot.DescriptorId == PendulumInput.SlotDescriptors[PendulumInput.MovementSlot]
            ? MovementInput.Read(frame, slot)
            : null;

    /// <summary>The character button claims of a frame the client sent.</summary>
    /// <param name="frame">Input frame.</param>
    /// <returns>The claims, or null when the frame is unknown.</returns>
    public CharacterInput? Claims(uint frame) =>
        _frames.TryGetValue(frame, out Dictionary<int, PendulumSlot>? slots)
        && slots.TryGetValue(PendulumInput.CharacterClaimsSlot, out PendulumSlot? slot)
        && slot.DescriptorId == PendulumInput.SlotDescriptors[PendulumInput.CharacterClaimsSlot]
            ? CharacterInput.Read(frame, slot)
            : null;

    /// <summary>Reads a payload.</summary>
    /// <param name="payload">The payload.</param>
    /// <returns>Its latest frame (null when it carries none), new interaction requests and new inventory requests.</returns>
    public (uint? Latest, IReadOnlyList<InteractionRequest> Requests, IReadOnlyList<InventoryRequest> Inventory) Read(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        BitReader reader = new(payload);
        if (PendulumInputHeader.Read(reader) is not { } header)
        {
            Weapons = [];
            return (null, [], []);
        }

        if (header.Baseline >= header.Latest)
        {
            throw new InvalidPacketException("Invalid input frame range");
        }

        IReadOnlyDictionary<int, PendulumSlot> previous = _frames.TryGetValue(header.Baseline, out Dictionary<int, PendulumSlot>? baseline)
            ? baseline
            : throw new InvalidPacketException("Input coded against an unknown frame");
        Dictionary<uint, Dictionary<int, PendulumSlot>> updates = [];
        List<InteractionRequest> requests = [];
        List<InventoryRequest> inventory = [];
        List<WeaponInput> weapons = [];
        uint first = Math.Max(header.Baseline + 1, header.Latest >= FramesPerPayload - 1 ? header.Latest - (FramesPerPayload - 1) : 0);
        for (uint number = first; number <= header.Latest; number++)
        {
            Dictionary<int, PendulumSlot> frame = PendulumCodec.Default.ReadFrame(reader, previous);
            updates[number] = frame;
            previous = frame;
            if (number > Latest && frame.TryGetValue(WeaponProtocol.InputSlot, out PendulumSlot? weapon))
            {
                weapons.Add(WeaponInput.Read(number, weapon));
            }
            if (number > Latest && Request(number, frame) is { } request)
            {
                requests.Add(request);
            }

            if (number > Latest && frame.TryGetValue(InventoryInput.Slot, out PendulumSlot? input)
                && InventoryCommand.Read(input) is { } command)
            {
                inventory.Add(new InventoryRequest(number, command));
            }
        }

        // The payload is byte-aligned; only zero padding may follow.
        if (reader.Remaining > 7 || (reader.Remaining > 0 && reader.ReadBits((int)reader.Remaining) != 0))
        {
            throw new InvalidPacketException("Unexpected input trailing data");
        }

        if (header.Latest > Latest)
        {
            Focus = Claim(updates[header.Latest], PendulumInput.InteractionFocusField);
        }

        Latest = Math.Max(Latest, header.Latest);
        Weapons = weapons.AsReadOnly();
        foreach ((uint number, Dictionary<int, PendulumSlot> frame) in updates)
        {
            _frames[number] = frame;
        }

        _frames = _frames.Where(entry => entry.Key == 0 || entry.Key + KeptFrames >= Latest).ToDictionary();
        return (header.Latest, requests, inventory);
    }

    private static InteractionRequest? Request(uint number, Dictionary<int, PendulumSlot> frame)
    {
        int request = (int)Claim(frame, PendulumInput.InteractionRequestField);
        ulong target = Claim(frame, PendulumInput.InteractionTargetField);
        return request != 0 && target != 0 ? new InteractionRequest(number, target, request) : null;
    }

    // A field of the frame's interaction claim (0 when the frame has none).
    private static ulong Claim(Dictionary<int, PendulumSlot> frame, int field) =>
        frame.TryGetValue(PendulumInput.InteractionSlot, out PendulumSlot? claim)
        && claim.DescriptorId == PendulumInput.SlotDescriptors[PendulumInput.InteractionSlot]
            ? PendulumCodec.Integer(s_claimFields[field], claim.Value)
            : 0;
}
