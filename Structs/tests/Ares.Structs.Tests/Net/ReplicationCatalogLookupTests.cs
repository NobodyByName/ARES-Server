using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Net;

public sealed class ReplicationCatalogLookupTests
{
    private const string Root = "/Script/Test.Root";
    private const string Base = "/Script/Test.Base";
    private const string Derived = "/Script/Test.Derived";
    private const string Widget = "/Script/Test.Widget";

    private static readonly PropertyDefinition s_count = new(Base + ".Count", PropertyKind.IntProperty, "int32", PropertyFlags.Net);
    private static readonly PropertyDefinition s_slots = new(Derived + ".Slots", PropertyKind.ByteProperty, "uint8", PropertyFlags.Net) { ArrayDim = 3 };
    private static readonly PropertyDefinition s_x = new("/Script/Test.Point.X", PropertyKind.FloatProperty, "float", PropertyFlags.None);
    private static readonly PropertyDefinition s_y = new("/Script/Test.Point.Y", PropertyKind.FloatProperty, "float", PropertyFlags.None) { LayoutIndex = 1 };

    private static readonly PropertyDefinition s_point = new(Derived + ".Point", PropertyKind.StructProperty, "FPoint", PropertyFlags.Net)
    {
        TypePath = "/Script/Test.Point",
        Children = [s_x.Path, s_y.Path],
    };

    private static ReplicationCatalog Catalog() =>
        new(
            [s_count, s_slots, s_x, s_y, s_point],
            [
                new ClassSchema(Root, ClassFlags.Native, null, [], [], 0x1U),
                new ClassSchema(Base, ClassFlags.Native, Root, [s_count.Path], [], 0x2U),
                new ClassSchema(Derived, ClassFlags.None, Base, [s_slots.Path, s_point.Path], [], 0x3U),
            ],
            [new ReferenceClass(Widget, Derived, 7)]);

    [Fact]
    public void Records_expand_static_arrays_after_inherited_records()
    {
        ReplicationRecord[] expected =
        [
            new(s_count.Path, 0),
            new(s_slots.Path, 0),
            new(s_slots.Path, 1),
            new(s_slots.Path, 2),
            new(s_point.Path, 0),
        ];

        Assert.Equal(expected, Catalog().Records(Derived));
    }

    [Fact]
    public void Ancestry_includes_reference_classes()
    {
        ReplicationCatalog catalog = Catalog();

        Assert.True(catalog.IsA(Widget, Root));
        Assert.False(catalog.IsA(Base, Derived));
        Assert.Equal(Derived, catalog.SuperOf(Widget));
        Assert.Equal("Point", catalog.Property(s_point.Path).Name);
        Assert.Equal([s_x, s_y], catalog.Property(s_point.Path).Children.Select(catalog.Property));
    }

    [Fact]
    public void A_class_cannot_be_both_a_schema_and_a_reference()
    {
        Assert.Throws<ArgumentException>(() => new ReplicationCatalog([], [new ClassSchema(Root, ClassFlags.Native, null, [], [], 0x1U)], [new ReferenceClass(Root, Root)]));
    }
}
