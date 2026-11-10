using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Movement;

namespace Ares.Structs.Tests.Game;

public sealed class PendulumCodecTests
{
    private static PendulumCodec Codec => PendulumCodec.Default;

    [Fact]
    public void Frame_sequences_match_the_reference()
    {
        foreach (IReadOnlyList<(PendulumSlot[] Slots, string Encoded, long Bits)> sequence in PendulumCodecVectors.Sequences)
        {
            IReadOnlyDictionary<int, PendulumSlot> previous = new Dictionary<int, PendulumSlot>();
            foreach ((PendulumSlot[] slots, string encoded, long bits) in sequence)
            {
                IReadOnlyDictionary<int, PendulumSlot> baseline = previous;
                Assert.Equal((encoded, bits), Encode(writer => Codec.WriteFrame(writer, slots, baseline)));

                Dictionary<int, PendulumSlot> decoded = Codec.ReadFrame(new BitReader(Convert.FromHexString(encoded), bits), baseline);
                Assert.Equal(slots.Length, decoded.Count);
                Assert.Equal((encoded, bits), Encode(writer => Codec.WriteFrame(writer, decoded.Values, baseline)));
                previous = slots.ToDictionary(slot => slot.Slot);
            }
        }
    }

    [Fact]
    public void Pawn_frames_are_rebuilt_from_the_published_initial_state()
    {
        PendulumSlot[] reset = PawnSlots(5000, (1000.5, -2000.25, 300.0));
        Assert.Equal((PendulumCodecVectors.PawnReset, PendulumCodecVectors.PawnResetBits), Encode(writer => Codec.WriteFrame(writer, reset, new Dictionary<int, PendulumSlot>())));

        Dictionary<int, PendulumSlot> before = PawnSlots(5000, null).ToDictionary(slot => slot.Slot);
        Assert.Equal((PendulumCodecVectors.PawnSteady, PendulumCodecVectors.PawnSteadyBits), Encode(writer => Codec.WriteFrame(writer, PawnSlots(5003, null), before)));
    }

    [Fact]
    public void Integer_fields_read_back_what_was_applied()
    {
        int descriptor = PendulumInput.SlotDescriptors[PendulumInput.InteractionSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[descriptor].Fields;
        byte[] value = Codec.Value(descriptor, [
            new PendulumIntegerValue(PendulumInput.InteractionRequestField, PendulumInput.PressRequest),
            new PendulumIntegerValue(PendulumInput.InteractionTargetField, 0x200005UL),
        ]);

        Assert.Equal((ulong)PendulumInput.PressRequest, PendulumCodec.Integer(fields[PendulumInput.InteractionRequestField], value));
        Assert.Equal(0x200005UL, PendulumCodec.Integer(fields[PendulumInput.InteractionTargetField], value));
        Assert.Throws<InvalidPacketException>(() => PendulumCodec.Integer(fields.OfType<PendulumQuaternionField>().FirstOrDefault() ?? PendulumRegistry.Descriptors[101].Fields[0], new byte[256]));
    }

    [Theory]
    [MemberData(nameof(PendulumCodecVectors.InputHeaders), MemberType = typeof(PendulumCodecVectors))]
    public void Input_headers_match_the_reference(uint baseline, uint latest, string encoded)
    {
        PendulumInputHeader? header = PendulumInputHeader.Read(new BitReader(Convert.FromHexString(encoded)));
        Assert.Equal(new PendulumInputHeader(baseline, latest), header);
        Assert.Null(PendulumInputHeader.Read(new BitReader([0])));
    }

    [Fact]
    public void Blackboard_updates_match_the_reference()
    {
        foreach ((uint baseline, uint latest, bool reset, int flags, string body, long bodyBits, string encoded, long bits) in PendulumCodecVectors.Updates)
        {
            PendulumFieldFlags fields = (PendulumFieldFlags)flags;
            byte[]? frame = body.Length == 0 ? null : Convert.FromHexString(body);
            Assert.Equal((encoded, bits), Encode(writer => BlackboardUpdate.Write(writer, baseline, latest, reset, fields, frame, bodyBits)));
            Assert.Equal(new BlackboardUpdateHeader(baseline, latest, reset, fields), BlackboardUpdate.ReadHeader(new BitReader(Convert.FromHexString(encoded), bits)));
        }

        Assert.Throws<InvalidPacketException>(() => BlackboardUpdate.Write(new BitWriter(), 5, 5, false, PendulumFieldFlags.Owner, null, 0));
        Assert.Throws<InvalidPacketException>(() => BlackboardUpdate.Write(new BitWriter(), 5, 6, false, PendulumFieldFlags.None, null, 0));
    }

    [Fact]
    public void Observed_blackboard_open_uses_the_short_header()
    {
        Assert.Equal(("0000f009000004", 52L),
            Encode(writer => BlackboardUpdate.Write(writer, 0, 318, true, PendulumFieldFlags.Others, [], 0, preferShortHeader: true)));
    }

    // The receiver reads a body with the field set its header names, so every header form must name the set the
    // body was written with: a short baseline, the last one the short header holds, and wider ones.
    [Theory]
    [InlineData(0U, false)]
    [InlineData(318U, false)]
    [InlineData(0x7FFFU, false)]
    [InlineData(BlackboardUpdate.ShortBaselineRange - 1, false)]
    [InlineData(BlackboardUpdate.ShortBaselineRange, true)]
    [InlineData(200_000U, true)]
    public void Blackboard_update_headers_name_the_field_set_of_the_body(uint baseline, bool wide)
    {
        foreach (PendulumFieldFlags fields in new[] { PendulumFieldFlags.Owner, PendulumFieldFlags.Others })
        {
            BitWriter writer = new();
            BlackboardUpdate.Write(writer, baseline, baseline + 40, baseline == 0, fields, [], 0, preferShortHeader: true);
            BitReader reader = new(writer.ToArray(), writer.Length);
            Assert.Equal(wide, new BitReader(writer.ToArray(), writer.Length).ReadBit());
            Assert.Equal(new BlackboardUpdateHeader(baseline, baseline + 40, baseline == 0, fields), BlackboardUpdate.ReadHeader(reader));
            Assert.Equal(0, reader.Remaining);
        }
    }

    private static PendulumSlot[] PawnSlots(int tick, (double X, double Y, double Z)? position)
    {
        List<PendulumSlot> slots = [];
        foreach (PendulumVariable variable in PawnBlackboard.InitialState)
        {
            List<PendulumValue> values = [.. variable.Values];
            switch (variable.Slot)
            {
                case PawnBlackboard.GraphStateSlot:
                    values.Add(new PendulumIntegerValue(PawnBlackboard.TickField, (ulong)tick));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.TimeField, (ulong)(tick * PawnBlackboard.TimePerTick)));
                    break;
                case PawnBlackboard.MovementStateSlot when position is { } location:
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionXField, Quantize(location.X)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionYField, Quantize(location.Y)));
                    values.Add(new PendulumIntegerValue(PawnBlackboard.PositionZField, Quantize(location.Z)));
                    break;
            }

            slots.Add(new PendulumSlot(variable.Slot, variable.Position, variable.Size, variable.DescriptorId, Codec.Value(variable.DescriptorId, values)));
        }

        return [.. slots];
    }

    private static ulong Quantize(double world) => (ulong)Math.Round((world + PawnBlackboard.PositionRange) * ((1 << 24) - 1) / (2 * PawnBlackboard.PositionRange), MidpointRounding.ToEven);

    private static (string Encoded, long Bits) Encode(Action<BitWriter> write)
    {
        BitWriter writer = new();
        write(writer);
        return (Convert.ToHexStringLower(writer.ToArray()), writer.Length);
    }
}
