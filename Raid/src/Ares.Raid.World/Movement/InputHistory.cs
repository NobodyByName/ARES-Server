using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>A button press the client asked for in an input frame.</summary>
/// <param name="Frame">Input frame.</param>
/// <param name="Target">The pressed object (a button id).</param>
/// <param name="Request">The request.</param>
public readonly record struct InteractionRequest(uint Frame, ulong Target, int Request);

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

    private Dictionary<uint, Dictionary<int, PendulumSlot>> _frames = new() { [0] = [] };

    /// <summary>Latest frame received.</summary>
    private uint Latest { get; set; }

    /// <summary>A copy for speculative packet processing (frames are not changed after they are stored).</summary>
    /// <returns>The copy.</returns>
    public InputHistory Copy() => new() { _frames = new Dictionary<uint, Dictionary<int, PendulumSlot>>(_frames), Latest = Latest };

    /// <summary>Reads a payload.</summary>
    /// <param name="payload">The payload.</param>
    /// <returns>Its latest frame (null when it carries none) and the new interaction requests.</returns>
    public (uint? Latest, IReadOnlyList<InteractionRequest> Requests) Read(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        BitReader reader = new(payload);
        if (PendulumInputHeader.Read(reader) is not { } header)
        {
            return (null, []);
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
        uint first = Math.Max(header.Baseline + 1, header.Latest >= FramesPerPayload - 1 ? header.Latest - (FramesPerPayload - 1) : 0);
        for (uint number = first; number <= header.Latest; number++)
        {
            Dictionary<int, PendulumSlot> frame = PendulumCodec.Default.ReadFrame(reader, previous);
            updates[number] = frame;
            previous = frame;
            if (number > Latest && Request(number, frame) is { } request)
            {
                requests.Add(request);
            }
        }

        // The payload is byte-aligned; only zero padding may follow.
        if (reader.Remaining > 7 || (reader.Remaining > 0 && reader.ReadBits((int)reader.Remaining) != 0))
        {
            throw new InvalidPacketException("Unexpected input trailing data");
        }

        Latest = Math.Max(Latest, header.Latest);
        foreach ((uint number, Dictionary<int, PendulumSlot> frame) in updates)
        {
            _frames[number] = frame;
        }

        _frames = _frames.Where(entry => entry.Key == 0 || entry.Key + KeptFrames >= Latest).ToDictionary();
        return (header.Latest, requests);
    }

    private static InteractionRequest? Request(uint number, Dictionary<int, PendulumSlot> frame)
    {
        if (!frame.TryGetValue(PendulumInput.InteractionSlot, out PendulumSlot? claim)
            || claim.DescriptorId != PendulumInput.SlotDescriptors[PendulumInput.InteractionSlot])
        {
            return null;
        }

        int request = (int)PendulumCodec.Integer(s_claimFields[PendulumInput.InteractionRequestField], claim.Value);
        ulong target = PendulumCodec.Integer(s_claimFields[PendulumInput.InteractionTargetField], claim.Value);
        return request != 0 && target != 0 ? new InteractionRequest(number, target, request) : null;
    }
}
