using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.Tests.World.Collision;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class PositionFollowingTests
{
    private const uint Start = 30;
    private const uint Last = Start + 20;

    [Fact]
    public void A_body_the_reports_agree_with_is_simulated_without_following()
    {
        PlayerMovement movement = Run(_ => new Vector3D(100, 200, 0), PlayerMovement.PositionTolerance);

        Assert.Equal(0, movement.Reseeds);
        Assert.Null(movement.LastReseed);
        BodyState body = movement.Latest.Body!.Value;
        Assert.False(body.Airborne);
        Assert.Equal(100, body.Position.X, 0);
        Assert.Equal(0, body.Position.Z, 0);
        Assert.Equal(body.Position, movement.Latest.Position);
    }

    [Fact]
    public void A_body_far_from_the_reported_position_continues_from_it()
    {
        // The client reports a walk the input does not ask for: ten units per tick.
        PlayerMovement movement = Run(frame => new Vector3D(100 + (10.0 * (frame - Start)), 200, 0), PlayerMovement.PositionTolerance);

        // Every simulated tick after the first, which places the body where the client reports it.
        Assert.Equal((int)(Last - Start - 2), movement.Reseeds);
        Assert.Equal(movement.Frame, movement.LastReseed!.Value.Frame);
        // Reported positions are coarse, so the distances are ten units give or take a position step.
        Assert.InRange(movement.LastReseed.Value.Distance, 9, 11);
        Assert.InRange(movement.WorstReseed, 9, 11);
        Assert.Equal(100 + (10.0 * (movement.Frame + 1 - Start)), movement.Latest.Position.X, 0);
    }

    [Fact]
    public void A_reported_position_above_the_ground_puts_the_body_in_the_air()
    {
        PlayerMovement movement = Run(frame => new Vector3D(100, 200, frame < Last - 3 ? 0 : 150), PlayerMovement.PositionTolerance);

        Assert.True(movement.Latest.Body!.Value.Airborne);
        Assert.Equal(CharacterStance.InAir, movement.State(movement.Frame)!.Value.Stance);
        Assert.True(movement.Latest.Unsettled);
    }

    [Fact]
    public void An_infinite_tolerance_never_follows_the_reports()
    {
        PlayerMovement movement = Run(frame => new Vector3D(100 + (10.0 * (frame - Start)), 200, 0), double.PositiveInfinity);

        // The body stays where the first simulated tick placed it: the position reported after that tick.
        Assert.Equal(0, movement.Reseeds);
        Assert.Equal(120, movement.Latest.Position.X, 0);
    }

    [Fact]
    public void Without_collision_the_position_stays_the_reported_one()
    {
        InputHistory history = new();
        history.Read(Payload(Start + 3, _ => new Vector3D(100, 200, 0)));
        PlayerMovement movement = new(Start, new Vector3D(100, 200, 0), 0);

        CharacterState state = movement.Advance(history, armed: false);

        Assert.Null(state.Body);
        Assert.Equal(new Vector3D(100, 200, 0), state.Position, (left, right) => Math.Abs(left.X - right.X) < 1 && Math.Abs(left.Y - right.Y) < 1 && Math.Abs(left.Z - right.Z) < 1);
        Assert.Equal(0, movement.Reseeds);
    }

    // Simulates up to the frame before the last one, as a raid does, on level ground at height 0.
    private static PlayerMovement Run(Func<uint, Vector3D> reported, double tolerance)
    {
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("floor", boxes: [SyntheticMaps.Box(new Vector3(4000, 4000, 10))])),
            builder.AddCell("/Game/Cell", []), SyntheticMaps.At(0, 0, -10), PlacementFlags.BlocksPawn);
        CollisionWorld world = new(builder.Build());
        InputHistory history = new();
        PlayerMovement movement = new(Start, reported(Start), 0, world, tolerance);

        for (uint latest = Start + 3; latest <= Last; latest++)
        {
            history.Read(Payload(latest, reported));
            while (movement.Frame + 1 < latest)
            {
                movement.Advance(history, armed: false);
            }
        }

        return movement;
    }

    // A client payload coded against the defaults, carrying the latest three frames: no buttons, no movement input.
    private static byte[] Payload(uint latest, Func<uint, Vector3D> reported)
    {
        BitWriter writer = new();
        writer.WriteBit(true);
        writer.WriteBounded(0, PendulumFrames.BaselineRange);
        PendulumFrames.WriteRelative(writer, latest, 0);
        Dictionary<int, PendulumSlot> previous = [];
        for (uint frame = latest - 2; frame <= latest; frame++)
        {
            Dictionary<int, PendulumSlot> current = Slots(reported(frame));
            PendulumCodec.Default.WriteFrame(writer, current.Values, previous);
            previous = current;
        }

        return writer.ToArray();
    }

    private static Dictionary<int, PendulumSlot> Slots(Vector3D at)
    {
        int positionDescriptor = PendulumInput.SlotDescriptors[PendulumInput.PositionSlot];
        int movementDescriptor = PendulumInput.SlotDescriptors[PendulumInput.MovementSlot];
        int claimsDescriptor = PendulumInput.SlotDescriptors[PendulumInput.CharacterClaimsSlot];
        (int Slot, int Descriptor, byte[] Value)[] values =
        [
            (PendulumInput.PositionSlot, positionDescriptor, PendulumCodec.Default.Value(positionDescriptor,
            [
                Quantized(positionDescriptor, PendulumInput.PositionXField, at.X),
                Quantized(positionDescriptor, PendulumInput.PositionYField, at.Y),
                Quantized(positionDescriptor, PendulumInput.PositionZField, at.Z),
            ])),
            (PendulumInput.MovementSlot, movementDescriptor, PendulumCodec.Default.Value(movementDescriptor,
            [
                new PendulumIntegerValue(PendulumInput.ForwardField, PendulumInput.MovementAxisBias),
                new PendulumIntegerValue(PendulumInput.RightField, PendulumInput.MovementAxisBias),
                new PendulumIntegerValue(PendulumInput.MovementFlagsField, 0),
            ])),
            (PendulumInput.CharacterClaimsSlot, claimsDescriptor, PendulumCodec.Default.Value(claimsDescriptor,
                [new PendulumIntegerValue(PendulumInput.CharacterClaimsFlagsField, 0)])),
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
