using Ares.Structs.Net.GameplayTags;

namespace Ares.Structs.Game.World;

/// <summary>
/// An authored round configuration. Only the values set on the asset itself are included;
/// inherited defaults are not resolved yet.
/// </summary>
/// <param name="Name">Short name, e.g. <c>Spearmint</c>.</param>
/// <param name="Package">Asset package.</param>
/// <param name="Identifier">Online identifier tag.</param>
/// <param name="AssetId">Numeric asset id.</param>
/// <param name="FilterPlayerStarts">Whether player starts are filtered by <paramref name="ContentQuery"/>.</param>
/// <param name="ContentQuery">Tag query for regular rounds.</param>
/// <param name="ContentQueryPvE">Tag query for PvE rounds.</param>
/// <param name="DataLayers">Data layers the round enables.</param>
/// <param name="Tags">Additional round tags.</param>
public sealed record RoundConfiguration(
    string Name,
    string Package,
    string Identifier,
    ulong AssetId,
    bool FilterPlayerStarts,
    GameplayTagQuery ContentQuery,
    GameplayTagQuery ContentQueryPvE,
    IReadOnlyList<RoundDataLayer> DataLayers,
    IReadOnlyList<string> Tags);

/// <summary>A data layer enabled by a round configuration.</summary>
/// <param name="Package">Data layer asset package.</param>
/// <param name="ExportHash">Public export hash of the asset.</param>
public readonly record struct RoundDataLayer(string Package, ulong ExportHash);
