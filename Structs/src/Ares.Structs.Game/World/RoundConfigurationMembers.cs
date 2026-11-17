using Ares.Structs.Core.Assets;

namespace Ares.Structs.Game.World;

/// <summary>The round configuration assets the map tables read for each map.</summary>
public static class RoundConfigurationAssets
{
    /// <summary>The Spearmint round configuration.</summary>
    public const string Spearmint = "/Game/Pioneer/GameMode/RoundConfiguration/DA_RoundConfiguration_Spearmint";

    /// <summary>The round configurations of the Dam map, in table order.</summary>
    public static IReadOnlyList<string> Dam { get; } = [Spearmint];
}

/// <summary>
/// Members of a round configuration asset (<c>URoundConfigurationDataAsset</c>, script, over the native
/// <c>UOptionalPersistenceDataAsset</c>), by script member name, and the native structs and classes they need.
/// </summary>
public static class RoundConfigurationMembers
{
    /// <summary><c>ConfigurationName</c>: short name, e.g. <c>Spearmint</c>.</summary>
    public const string ConfigurationName = "ConfigurationName";

    /// <summary><c>ContentFilterQuery</c>: tag query for regular rounds.</summary>
    public const string ContentFilterQuery = "ContentFilterQuery";

    /// <summary><c>ContentFilterQueryPvE</c>: tag query for PvE rounds.</summary>
    public const string ContentFilterQueryPvE = "ContentFilterQueryPvE";

    /// <summary><c>bFilterPlayerStarts</c>: whether player starts are filtered by the content query.</summary>
    public const string FilterPlayerStarts = "bFilterPlayerStarts";

    /// <summary><c>DataLayers</c>: data layer assets the round enables.</summary>
    public const string DataLayers = "DataLayers";

    /// <summary><c>Tags</c>: additional round tags.</summary>
    public const string Tags = "Tags";

    /// <summary>Script name of the round configuration's native super class.</summary>
    public const string NativeBase = "UOptionalPersistenceDataAsset";

    /// <summary>The native persistence identifier: a gameplay tag (the online identifier).</summary>
    public const string Identifier = "Identifier";

    /// <summary>The native persistence asset id: int64.</summary>
    public const string AssetId = "AssetId";

    /// <summary>
    /// The native chain of <see cref="NativeBase"/> as far as round configurations store it: <see cref="Identifier"/>
    /// then <see cref="AssetId"/>, fitted to the Spearmint asset (the identifier names its online tag, the id ends the
    /// block before the export's closing word).
    /// </summary>
    public static StructSchema PersistenceDataAsset { get; } = new(NativeBase,
    [
        new PropertyField(Identifier, PropertyType.StructOf(EngineStructs.GameplayTag)),
        new PropertyField(AssetId, PropertyType.Int64),
    ]);

    /// <summary><c>FGameplayTagQuery.TagDictionary</c> (see <see cref="EngineStructs.GameplayTagQuery"/>): the tags the token stream indexes.</summary>
    public const string TagDictionary = "TagDictionary";

    /// <summary><c>FGameplayTagQuery.QueryTokenStream</c>: the query tokens.</summary>
    public const string QueryTokenStream = "QueryTokenStream";

    /// <summary><c>FGameplayTag.TagName</c>: the tag's name.</summary>
    public const string TagName = "TagName";
}
