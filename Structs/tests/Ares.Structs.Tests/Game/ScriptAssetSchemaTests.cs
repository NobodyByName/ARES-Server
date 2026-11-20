using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Scripts;

namespace Ares.Structs.Tests.Game;

/// <summary>
/// The hand-written property chains of script classes and structs (movement data assets, the item menu metadata
/// prefix) against the current build's script types: their members are the reflected members, by name and in
/// declaration order (a member left unknown matches any name), and so are those of the script structs they hold.
/// </summary>
public sealed class ScriptAssetSchemaTests
{
    private static readonly Dictionary<string, StructSchema> s_movementSchemas = new(StringComparer.Ordinal)
    {
        ["UDodgeRollMetrics"] = MovementAssetSchemas.DodgeRollMetrics,
        ["UHeavyLandingAnimations"] = MovementAssetSchemas.HeavyLandingAnimations,
        ["UVaultDefinitionsDataAsset"] = MovementAssetSchemas.VaultDefinitionsDataAsset,
    };

    public static TheoryData<string> MovementTypes => [.. s_movementSchemas.Keys];

    [Theory]
    [MemberData(nameof(MovementTypes))]
    public void MovementAssetSchemasDeclareTheScriptMembers(string scriptType) => OwnMembers(scriptType, s_movementSchemas[scriptType]);

    [Fact]
    public void SnapHookChainNamesItsKnownMembersAtTheirScriptChainIndices()
    {
        string[] chain = ReflectedChain("USnaphookItem");
        StructSchema schema = MovementAssetSchemas.SnaphookItemChain;

        Assert.All(Enumerable.Range(0, schema.ChainLength).Where(index => schema.At(index).Field is not null),
            index => Assert.Equal(schema.At(index).Field!.Name, index < chain.Length ? chain[index] : null));
    }

    [Fact]
    public void UiMetaDataItemSchemaIsAPrefixOfTheScriptMembers()
    {
        StructSchema schema = ItemAssetSchemas.UiGameplayItemMetaDataItem;
        string[] members = ReflectedChain("U" + ItemAssetSchemas.UiGameplayItemMetaDataItemClass[ScriptTypeSchemas.ScriptPackage.Length..]);

        Assert.Equal(members.Take(schema.Members.Count), schema.Members.Select(member => member!.Name));
    }

    private static string[] ReflectedChain(string name)
    {
        List<string> members = [];
        for (string? type = name; type is not null && ScriptTypes.ByName.TryGetValue(type, out ScriptTypeDefinition? definition); type = definition.Super)
        {
            members.AddRange(definition.Members.Select(member => member.Name));
        }

        return [.. members];
    }

    private static void OwnMembers(string scriptType, StructSchema schema)
    {
        ScriptMemberDefinition[] members = [.. Assert.Contains(scriptType, ScriptTypes.ByName).Members];
        Assert.Equal(members.Length, schema.Members.Count);
        for (int i = 0; i < members.Length; i++)
        {
            PropertyField? known = schema.Members[i];
            if (known is null)
            {
                continue;
            }

            Assert.Equal(members[i].Name, known.Name);
            (PropertyType type, ScriptMemberType member) = (known.Type, members[i].Type);
            if (type is { Kind: PropertyKind.Array, Inner: { } inner } && member is { Kind: PropertyKind.Array, Inner: { } element })
            {
                (type, member) = (inner, element);
            }

            if (type is { Kind: PropertyKind.Struct, Struct: { Native: NativeLayout.None } nested }
                && member is { Kind: PropertyKind.Struct, TypeName: { } name } && ScriptTypes.ByName.ContainsKey(name))
            {
                OwnMembers(name, nested);
            }
        }
    }
}
