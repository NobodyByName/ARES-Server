using Ares.Structs.Backend.Progression;

namespace Ares.Common.Game.Inventory;

/// <summary>
/// The backend item content the shared item rules (<see cref="LoadoutDefinitions"/>) take besides the game data: what
/// the game's menu reads from its backend and the installed game does not hold. A server owns and loads it; Host keeps
/// it as its default content, which the operator can override.
/// </summary>
/// <param name="Storage">The stash structure definitions: the stash slot, every stash container tier and their content slot.</param>
/// <param name="Loadout">The loadout structure definitions: the frame slot, the standard frame and its branches, containers and slots.</param>
/// <param name="FrameSlotAssetId">Asset id of the loadout root that holds the selected frame.</param>
/// <param name="StandardFrameAssetId">Asset id of the frame selected without an augment.</param>
/// <param name="RecycleOutputs">Items one recycled item gives, in package order, by item asset id; items absent here cannot be recycled.</param>
/// <param name="QualityLevels">
/// Quality level of a catalogue item's menu description, by item asset id; items absent here have quality 0.
/// </param>
public sealed record BackendItemContent(
    IReadOnlyList<ItemDescription> Storage,
    IReadOnlyList<ItemDescription> Loadout,
    long FrameSlotAssetId,
    long StandardFrameAssetId,
    IReadOnlyDictionary<long, IReadOnlyList<ItemAssetAmount>> RecycleOutputs,
    IReadOnlyDictionary<long, long> QualityLevels);
