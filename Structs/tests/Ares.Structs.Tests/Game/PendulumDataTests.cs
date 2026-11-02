using Ares.Structs.Game.Movement;

namespace Ares.Structs.Tests.Game;

public sealed class PendulumDataTests
{
    [Fact]
    public void Descriptors_are_registered_and_fit_their_values()
    {
        foreach ((int id, PendulumDescriptor descriptor) in PendulumRegistry.Descriptors)
        {
            Assert.Equal(id, descriptor.Id);
            Assert.InRange(id, 0, PendulumRegistry.Count - 1);
            AssertFieldsFit(descriptor.Fields, descriptor.Size, $"descriptor {id}");
        }
    }

    [Fact]
    public void Input_slots_use_registered_descriptors()
    {
        Assert.Equal(PendulumInput.SlotDescriptors.Count, PendulumInput.SlotDescriptors.Distinct().Count());
        Assert.All(PendulumInput.SlotDescriptors, id => Assert.True(PendulumRegistry.Descriptors.ContainsKey(id)));
    }

    [Fact]
    public void Pawn_blackboard_values_match_their_fields()
    {
        IReadOnlyList<PendulumVariable> variables = PawnBlackboard.InitialState;
        Assert.Equal(variables.Count, variables.Select(variable => variable.Slot).Distinct().Count());
        foreach (PendulumVariable variable in variables)
        {
            PendulumDescriptor descriptor = PendulumRegistry.Descriptors[variable.DescriptorId];
            Assert.InRange(variable.Position, 0, 63);
            Assert.InRange(variable.Size, 1, 63);
            Assert.True(variable.Position + variable.Size <= 64);
            Assert.True(descriptor.Size <= variable.Size * 256, $"slot {variable.Slot} is too small");
            AssertValuesMatch(variable.Values, descriptor.Fields, $"slot {variable.Slot}");
        }

        // Positions do not overlap.
        int[] used = [.. variables.SelectMany(variable => Enumerable.Range(variable.Position, variable.Size))];
        Assert.Equal(used.Length, used.Distinct().Count());
    }

    [Fact]
    public void Per_frame_fields_are_scalars_left_at_their_defaults()
    {
        AssertPerFrameField(PawnBlackboard.GraphStateSlot, PawnBlackboard.TickField);
        AssertPerFrameField(PawnBlackboard.GraphStateSlot, PawnBlackboard.TimeField);
        AssertPerFrameField(PawnBlackboard.MovementStateSlot, PawnBlackboard.PositionXField);
        AssertPerFrameField(PawnBlackboard.MovementStateSlot, PawnBlackboard.PositionYField);
        AssertPerFrameField(PawnBlackboard.MovementStateSlot, PawnBlackboard.PositionZField);
    }

    private static void AssertPerFrameField(int slot, int field)
    {
        PendulumVariable variable = PawnBlackboard.InitialState.Single(candidate => candidate.Slot == slot);
        Assert.IsType<PendulumScalarField>(PendulumRegistry.Descriptors[variable.DescriptorId].Fields[field]);
        Assert.DoesNotContain(variable.Values, value => value.Field == field);
    }

    private static void AssertFieldsFit(IReadOnlyList<PendulumField> fields, int size, string where)
    {
        foreach (PendulumField field in fields)
        {
            int end = field switch
            {
                PendulumScalarField scalar => scalar.Offset + scalar.Width,
                PendulumBoundedField bounded => bounded.Offset + bounded.Width,
                PendulumBitSetField bits => bits.Offset + (bits.BitCount / 8),
                PendulumBitfieldField bits => bits.Offset + ((bits.BitCount + 7) / 8),
                PendulumObjectField or PendulumNameField => field.Offset + 8,
                PendulumQuaternionField => field.Offset + 21, // four 4-byte components, the unknown bits, then the component order
                PendulumArrayField array => array.Offset + (array.Capacity * array.Stride),
                _ => field.Offset + 1,
            };
            Assert.True(end <= size, $"{where}: field at {field.Offset} ends at {end}, beyond {size}");
            if (field is not PendulumArrayField nested)
            {
                continue;
            }

            AssertFieldsFit(nested.ElementFields, nested.Stride, $"{where} array at {nested.Offset}");
            if (nested.CountField is { } count)
            {
                Assert.IsType<PendulumScalarField>(fields[count]);
            }
        }
    }

    private static void AssertValuesMatch(IReadOnlyList<PendulumValue> values, IReadOnlyList<PendulumField> fields, string where)
    {
        Assert.Equal(values.Count, values.Select(value => value.Field).Distinct().Count());
        foreach (PendulumValue value in values)
        {
            Assert.True(value.Field < fields.Count, $"{where}: field {value.Field} does not exist");
            PendulumField field = fields[value.Field];
            switch (value)
            {
                case PendulumQuaternionValue:
                    Assert.IsType<PendulumQuaternionField>(field);
                    break;
                case PendulumArrayValue array:
                    PendulumArrayField arrayField = Assert.IsType<PendulumArrayField>(field);
                    foreach (PendulumArrayElement element in array.Elements)
                    {
                        Assert.InRange(element.Index, 0, arrayField.Capacity - 1);
                        AssertValuesMatch(element.Values, arrayField.ElementFields, $"{where} element {element.Index}");
                    }

                    break;
                default:
                    Assert.IsNotType<PendulumQuaternionField>(field);
                    Assert.IsNotType<PendulumArrayField>(field);
                    break;
            }
        }
    }
}
