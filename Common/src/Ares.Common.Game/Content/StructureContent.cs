using System.Collections.ObjectModel;
using Ares.Structs.Backend.Progression;

namespace Ares.Common.Game.Content;

/// <summary>The stash structure of <see cref="AresContent.StashFile"/>: the stash slot and the chain of container tiers.</summary>
public sealed class StashContent
{
    internal StashContent(StashDocument file, Func<string, AresContentException> invalid)
    {
        SlotAssetId = file.SlotAssetId;
        FirstContainerAssetId = file.FirstContainerAssetId;
        Definitions = file.Definitions;
        Dictionary<long, ItemDescription> byAsset = Index(file.Definitions, invalid);
        if (!byAsset.TryGetValue(SlotAssetId, out ItemDescription? slot) || slot.SlotConfig is not SingleItemSlot { IsRequired: true } stashSlot)
        {
            throw invalid($"stashSlot {SlotAssetId} must be defined as a required single slot");
        }

        List<ItemDescription> tiers = [];
        for (long tier = FirstContainerAssetId; tier != 0;)
        {
            if (!byAsset.TryGetValue(tier, out ItemDescription? container) || container.SlotConfig is not ItemSlotArray
                || !stashSlot.AllowedSlotAssetIds.Contains(tier) || tiers.Any(earlier => earlier.GameAssetId == tier))
            {
                throw invalid($"stash tier {tier} must be defined once in the chain as a slot array that the stash slot allows");
            }

            tiers.Add(container);
            if (container.Upgrade is not { NextItemGameAssetId: not 0 } upgrade)
            {
                break;
            }

            if (upgrade.Cost is not ItemAmountsOfferCost { ItemAmounts: [_] })
            {
                throw invalid($"the upgrade of stash tier {tier} must cost one item amount (the coin price)");
            }

            tier = upgrade.NextItemGameAssetId;
        }

        Tiers = tiers.AsReadOnly();
    }

    /// <summary>The stash slot, the root holding the owned container tier.</summary>
    public long SlotAssetId { get; }

    /// <summary>The container tier a new stash starts with.</summary>
    public long FirstContainerAssetId { get; }

    /// <summary>The stash slot, every container tier and their content slot, in serving order.</summary>
    public IReadOnlyList<ItemDescription> Definitions { get; }

    /// <summary>The container tiers from the first, each upgraded into the next.</summary>
    public IReadOnlyList<ItemDescription> Tiers { get; }

    internal static Dictionary<long, ItemDescription> Index(IReadOnlyList<ItemDescription> definitions, Func<string, AresContentException> invalid)
    {
        Dictionary<long, ItemDescription> byAsset = [];
        foreach (ItemDescription definition in definitions)
        {
            if (!byAsset.TryAdd(definition.GameAssetId, definition))
            {
                throw invalid($"defines {definition.GameAssetId} twice");
            }
        }

        return byAsset;
    }
}

/// <summary>The loadout structure of <see cref="AresContent.LoadoutFile"/>: the frame slot, the standard frame and its equipment places.</summary>
public sealed class LoadoutContent
{
    internal LoadoutContent(LoadoutDocument file, Func<string, AresContentException> invalid)
    {
        FrameSlotAssetId = file.FrameSlotAssetId;
        StandardFrameAssetId = file.StandardFrameAssetId;
        Branches = file.Branches;
        Definitions = file.Definitions;
        Dictionary<long, ItemDescription> byAsset = StashContent.Index(file.Definitions, invalid);
        if (!byAsset.ContainsKey(FrameSlotAssetId))
        {
            throw invalid($"frameSlot {FrameSlotAssetId} must be defined");
        }

        if (!byAsset.TryGetValue(StandardFrameAssetId, out ItemDescription? frame) || frame.SlotConfig is not StructuralItemSlots structure)
        {
            throw invalid($"standardFrame {StandardFrameAssetId} must be defined as a structure of branches");
        }

        long[] named = [.. Branches.All];
        if (!named.All(structure.SlotAssetIds.Contains) || named.Distinct().Count() != named.Length)
        {
            throw invalid($"each of the branches must be a different branch of standardFrame {StandardFrameAssetId}");
        }

        FrameBranches = new ReadOnlyCollection<long>([.. structure.SlotAssetIds]);
    }

    /// <summary>The loadout root holding the selected frame.</summary>
    public long FrameSlotAssetId { get; }

    /// <summary>The frame selected without an augment.</summary>
    public long StandardFrameAssetId { get; }

    /// <summary>The standard frame's branch of each equipment place.</summary>
    public LoadoutBranches Branches { get; }

    /// <summary>Every branch of the standard frame, in frame order.</summary>
    public IReadOnlyList<long> FrameBranches { get; }

    /// <summary>The frame slot, the standard frame and its branches, containers and slots, in serving order.</summary>
    public IReadOnlyList<ItemDescription> Definitions { get; }
}
