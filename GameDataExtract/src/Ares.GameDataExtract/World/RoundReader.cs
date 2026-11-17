using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.Structs.Game.World;
using Ares.Structs.Net.GameplayTags;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads round configuration assets (<see cref="RoundConfiguration"/>): the values set on each asset itself, with its
/// native persistence identifier and asset id.
/// </summary>
internal static class RoundReader
{
    private static readonly string[] s_members =
    [
        RoundConfigurationMembers.ConfigurationName,
        RoundConfigurationMembers.ContentFilterQuery,
        RoundConfigurationMembers.ContentFilterQueryPvE,
        RoundConfigurationMembers.FilterPlayerStarts,
        RoundConfigurationMembers.DataLayers,
        RoundConfigurationMembers.Tags,
        RoundConfigurationMembers.Identifier,
        RoundConfigurationMembers.AssetId,
    ];

    /// <summary>The round configuration in package <paramref name="package"/>.</summary>
    /// <exception cref="InvalidDataException">The asset is missing or a value does not decode.</exception>
    public static RoundConfiguration Read(MapRuleSources sources, string package)
    {
        ExportRef asset = sources.Export(package, "." + package[(package.LastIndexOf('/') + 1)..]);
        PropertyBag bag = sources.Members(asset, s_members);
        long assetId = bag.Get<long>(RoundConfigurationMembers.AssetId);
        if (assetId <= 0)
        {
            throw new InvalidDataException($"{asset.Path}: invalid asset id {assetId}");
        }

        RoundDataLayer[] layers =
        [
            .. bag.Get(RoundConfigurationMembers.DataLayers, Array.Empty<object>()).Cast<ObjectReference>().Select(reference =>
            {
                (string layer, ulong hash) = sources.PublicExport(asset.Package, reference);
                return new RoundDataLayer(layer, hash);
            }),
        ];
        return new RoundConfiguration(
            bag.Get<string>(RoundConfigurationMembers.ConfigurationName),
            package,
            MapRuleSources.Tag(bag.Get<PropertyBag>(RoundConfigurationMembers.Identifier)),
            (ulong)assetId,
            bag.Get(RoundConfigurationMembers.FilterPlayerStarts, false),
            Query(bag.Get<PropertyBag>(RoundConfigurationMembers.ContentFilterQuery)),
            Query(bag.Get<PropertyBag>(RoundConfigurationMembers.ContentFilterQueryPvE)),
            layers,
            bag.Get(RoundConfigurationMembers.Tags, Array.Empty<string>()));
    }

    private static GameplayTagQuery Query(PropertyBag query)
    {
        string[] dictionary = [.. query.Get(RoundConfigurationMembers.TagDictionary, Array.Empty<object>()).Cast<PropertyBag>().Select(MapRuleSources.Tag)];
        byte[] tokens = [.. query.Get(RoundConfigurationMembers.QueryTokenStream, Array.Empty<object>()).Cast<byte>()];
        return new GameplayTagQuery(dictionary, tokens);
    }
}
