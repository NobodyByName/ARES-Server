using System.Reflection;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Tests.Game;

public sealed class ReplicationCatalogTests
{
    private static ReplicationCatalog Catalog => GameReplication.Catalog;

    [Fact]
    public void Catalog_holds_every_class_and_property()
    {
        // 154 classes with replication data plus the package, world and level classes.
        Assert.Equal(157, Catalog.Classes.Count);
        Assert.Equal(1647, Catalog.Properties.Count);
        Assert.Equal(14, Catalog.ReferenceClasses.Count);
    }

    [Fact]
    public void Property_references_resolve()
    {
        foreach (PropertyDefinition property in Catalog.Properties.Values)
        {
            string?[] references = [property.Inner, property.MapKey, property.MapValue, property.Underlying, .. property.Children];
            foreach (string reference in references.OfType<string>())
            {
                Assert.True(Catalog.Properties.ContainsKey(reference), $"{property.Path} refers to unknown {reference}");
            }

            Assert.Equal(property.Kind == PropertyKind.ArrayProperty, property.Inner is not null);
        }
    }

    [Fact]
    public void Struct_members_of_one_owner_are_in_layout_order()
    {
        foreach (PropertyDefinition property in Catalog.Properties.Values.Where(p => p.Children.Count > 1))
        {
            foreach (IGrouping<string, string> owner in property.Children.GroupBy(child => child[..child.LastIndexOf('.')]))
            {
                int[] order = [.. owner.Select(child => Catalog.Property(child).LayoutIndex)];
                Assert.True(order.Order().SequenceEqual(order), $"{property.Path} members of {owner.Key} are not in layout order");
            }
        }
    }

    [Fact]
    public void Every_class_derives_from_object()
    {
        foreach (string path in Catalog.Classes.Keys.Concat(Catalog.ReferenceClasses.Keys))
        {
            Assert.True(Catalog.IsA(path, ClassPaths.Object), $"{path} does not reach the root object class");
        }
    }

    [Fact]
    public void Own_properties_are_replicated_and_function_parameters_are_parameters()
    {
        foreach (ClassSchema schema in Catalog.Classes.Values)
        {
            Assert.All(schema.OwnProperties, path => Assert.True(Catalog.Property(path).Flags.HasFlag(PropertyFlags.Net), path));
            foreach (FunctionSchema function in schema.Functions)
            {
                Assert.True(function.Flags.HasFlag(FunctionFlags.Net), $"{schema.Path}.{function.Name}");
                Assert.All(function.Parameters, path => Assert.True(Catalog.Property(path).Flags.HasFlag(PropertyFlags.Parameter), path));
            }
        }
    }

    [Fact]
    public void Records_include_inherited_properties_and_array_elements()
    {
        IReadOnlyList<ReplicationRecord> records = Catalog.Records(PlayerClasses.Pawn.Class);

        Assert.Equal(38, records.Count);
        Assert.Equal(Catalog.Records(ClassPaths.Actor), records.Take(Catalog.Records(ClassPaths.Actor).Count));
        Assert.Empty(Catalog.Records(ClassPaths.World));
    }

    [Theory]
    [InlineData(typeof(ClassPaths))]
    [InlineData(typeof(StructPaths))]
    [InlineData(typeof(EnumPaths))]
    public void Path_constants_name_types_in_the_catalog(Type constants)
    {
        HashSet<string> types = [.. Catalog.Properties.Values.Select(p => p.TypePath).OfType<string>()];
        foreach (FieldInfo field in constants.GetFields())
        {
            string path = (string)field.GetValue(null)!;
            string name = path[(path.LastIndexOf('.') + 1)..].Replace("_", "", StringComparison.Ordinal);
            Assert.Equal(name, field.Name, ignoreCase: true);
            bool known = constants == typeof(ClassPaths)
                ? Catalog.Classes.ContainsKey(path) || Catalog.ReferenceClasses.ContainsKey(path)
                : types.Contains(path);
            Assert.True(known, $"{constants.Name}.{field.Name} is not in the catalog");
        }
    }

    [Fact]
    public void Known_enums_are_used_by_replicated_properties()
    {
        Assert.Equal(KnownEnums.NetRole.Path, Catalog.Property("/Script/Engine.Actor.Role").TypePath);
        Assert.Equal(KnownEnums.GamePhase.Path, Catalog.Property("/Script/Angelscript.PioneerGameState.GamePhase").TypePath);
        Assert.Equal(Enum.GetValues<NetRole>().Length, KnownEnums.NetRole.Names.Count);
        Assert.Equal((int)GamePhase.NotInitialized, KnownEnums.GamePhase.MaxValue);
    }

    [Fact]
    public void Unknown_lookups_throw()
    {
        Assert.Throws<KeyNotFoundException>(() => Catalog.Class("/Script/Missing.Class"));
        Assert.Throws<KeyNotFoundException>(() => Catalog.Property("/Script/Missing.Class.Value"));
        Assert.Throws<KeyNotFoundException>(() => Catalog.IsA("/Script/Missing.Class", ClassPaths.Object));
    }
}
