using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.Tests.World.Collision;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

/// <summary>One input frame a scripted client sends.</summary>
/// <param name="Position">Reported position.</param>
/// <param name="Forward">Forward movement axis.</param>
/// <param name="Right">Right movement axis.</param>
/// <param name="Buttons">Movement buttons held; their previous-frame copies are added from the frame before.</param>
/// <param name="Claims">Character claims flags.</param>
/// <param name="FireBlocked">Whether the client blocks firing.</param>
internal readonly record struct ScriptedFrame(Vector3D Position, sbyte Forward = 0, sbyte Right = 0, MovementButtons Buttons = MovementButtons.None,
    ulong Claims = 0, bool FireBlocked = false);

/// <summary>Builds client payloads from a per-frame script and delivers them to a character simulation as a raid does.</summary>
internal static class ScriptedClient
{
    private static readonly (MovementButtons Button, MovementButtons Previous)[] s_pressPairs =
    [
        (MovementButtons.Jump, MovementButtons.PreviousJump),
        (MovementButtons.Crouch, MovementButtons.PreviousCrouch),
        (MovementButtons.DodgeRoll, MovementButtons.PreviousDodgeRoll),
    ];

    /// <summary>Packets carrying the latest frames from <paramref name="first"/> to <paramref name="last"/>, in order.</summary>
    public static IEnumerable<uint> InOrder(uint first, uint last)
    {
        for (uint latest = first; latest <= last; latest++)
        {
            yield return latest;
        }
    }

    /// <summary>
    /// Delivers the packets in this order, simulating like a raid after each one, and returns each frame's state as it was
    /// when it left the revisable window (or at the end), in frame order.
    /// </summary>
    public static List<CharacterState> Deliver(PlayerMovement movement, Func<uint, ScriptedFrame> script, IEnumerable<uint> packets,
        Action<uint>? delivered = null)
    {
        InputHistory history = new();
        Dictionary<uint, CharacterState> settled = [];
        uint latest = 0;
        uint first = movement.Frame + 1;
        foreach (uint packet in packets)
        {
            latest = Math.Max(latest, history.Read(Payload(script, packet)).Latest ?? 0);
            delivered?.Invoke(packet);
            movement.Revise(history);
            while (movement.Frame + 1 < latest)
            {
                movement.Advance(history, armed: false);
            }

            for (uint frame = first; frame <= movement.Frame; frame++)
            {
                if (movement.State(frame) is { } state)
                {
                    settled[frame] = state;
                }
            }
        }

        return [.. settled.OrderBy(entry => entry.Key).Select(entry => entry.Value)];
    }

    /// <summary>A level of axis-aligned boxes that block the character and climbing.</summary>
    public static CollisionWorld World(params (Vector3D Center, Vector3 HalfExtents)[] boxes)
    {
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        int index = 0;
        foreach ((Vector3D center, Vector3 halfExtents) in boxes)
        {
            builder.AddPlacement(builder.AddBody(SyntheticMaps.Body($"box{index++}", boxes: [SyntheticMaps.Box(halfExtents)])), cell,
                SyntheticMaps.At(center.X, center.Y, center.Z), PlacementFlags.BlocksPawn | PlacementFlags.BlocksClimbable);
        }

        return new CollisionWorld(builder.Build());
    }

    // A client payload coded against the defaults, carrying the latest three frames.
    private static byte[] Payload(Func<uint, ScriptedFrame> script, uint latest)
    {
        BitWriter writer = new();
        writer.WriteBit(true);
        writer.WriteBounded(0, PendulumFrames.BaselineRange);
        PendulumFrames.WriteRelative(writer, latest, 0);
        Dictionary<int, PendulumSlot> previous = [];
        for (uint frame = latest - 2; frame <= latest; frame++)
        {
            Dictionary<int, PendulumSlot> current = Slots(script(frame), script(frame - 1));
            PendulumCodec.Default.WriteFrame(writer, current.Values, previous);
            previous = current;
        }

        return writer.ToArray();
    }

    private static Dictionary<int, PendulumSlot> Slots(ScriptedFrame frame, ScriptedFrame before)
    {
        int positionDescriptor = PendulumInput.SlotDescriptors[PendulumInput.PositionSlot];
        List<PendulumValue> reported =
        [
            Quantized(positionDescriptor, PendulumInput.PositionXField, frame.Position.X),
            Quantized(positionDescriptor, PendulumInput.PositionYField, frame.Position.Y),
            Quantized(positionDescriptor, PendulumInput.PositionZField, frame.Position.Z),
        ];
        if (frame.FireBlocked)
        {
            reported.Add(new PendulumIntegerValue(WeaponProtocol.InputFlagsField, 1UL << WeaponProtocol.FireBlockedBit));
        }

        MovementButtons buttons = frame.Buttons;
        foreach ((MovementButtons button, MovementButtons copy) in s_pressPairs)
        {
            buttons |= before.Buttons.HasFlag(button) ? copy : MovementButtons.None;
        }

        int movementDescriptor = PendulumInput.SlotDescriptors[PendulumInput.MovementSlot];
        int claimsDescriptor = PendulumInput.SlotDescriptors[PendulumInput.CharacterClaimsSlot];
        (int Slot, int Descriptor, byte[] Value)[] values =
        [
            (PendulumInput.PositionSlot, positionDescriptor, PendulumCodec.Default.Value(positionDescriptor, reported)),
            (PendulumInput.MovementSlot, movementDescriptor, PendulumCodec.Default.Value(movementDescriptor,
            [
                new PendulumIntegerValue(PendulumInput.ForwardField, (ulong)(frame.Forward + PendulumInput.MovementAxisBias)),
                new PendulumIntegerValue(PendulumInput.RightField, (ulong)(frame.Right + PendulumInput.MovementAxisBias)),
                new PendulumIntegerValue(PendulumInput.MovementFlagsField, (ulong)buttons),
            ])),
            (PendulumInput.CharacterClaimsSlot, claimsDescriptor, PendulumCodec.Default.Value(claimsDescriptor,
                [new PendulumIntegerValue(PendulumInput.CharacterClaimsFlagsField, frame.Claims)])),
        ];
        Dictionary<int, PendulumSlot> slots = [];
        int offset = 0;
        foreach ((int slot, int descriptor, byte[] value) in values)
        {
            int size = (value.Length + 255) / 256;
            slots[slot] = new PendulumSlot(slot, offset, size, descriptor, value);
            offset += size;
        }

        return slots;
    }

    private static PendulumIntegerValue Quantized(int descriptor, int field, double world)
    {
        PendulumScalarField scalar = (PendulumScalarField)PendulumRegistry.Descriptors[descriptor].Fields[field];
        return new PendulumIntegerValue(field, (ulong)Math.Round((world + PawnBlackboard.PositionRange) * (scalar.Maximum - 1) / (2 * PawnBlackboard.PositionRange)));
    }
}
