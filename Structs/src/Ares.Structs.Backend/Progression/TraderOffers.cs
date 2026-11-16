namespace Ares.Structs.Backend.Progression;

/// <summary>Trader offers that are always on sale: no stock limit, no required items and no date window.</summary>
public static class TraderOffers
{
    /// <summary>Item asset id of the trader Celeste.</summary>
    public const long CelesteAssetId = -1431231910L;

    /// <summary>Celeste's permanent offers, in shop order.</summary>
    public static IReadOnlyList<OfferDescription> Celeste { get; } =
    [
        new(580523442L, "NPC", "Metal Parts", "", CelesteAssetId, 0L, true, [],
            new OfferCostPackage([new OfferCostItem(1051518749L, 1L, 1.0)]),
            new ItemAmountsOfferCost([new ItemAssetAmount(1051518749L, 1L)]),
            new OfferRewardPackage([new OfferRewardedItem(-1437131902L, 1L, 1.0, [], "99f8cb21-a50f-4202-93a4-391603cef81a", 0L, false)]),
            81L, OfferDescription.NoDate, OfferDescription.NoDate, new OfferLimit(0L, 0L)),
        new(421055476L, "NPC", "Plastic Parts", "", CelesteAssetId, 0L, true, [],
            new OfferCostPackage([new OfferCostItem(1051518749L, 1L, 1.0)]),
            new ItemAmountsOfferCost([new ItemAssetAmount(1051518749L, 1L)]),
            new OfferRewardPackage([new OfferRewardedItem(828947161L, 1L, 1.0, [], "99f8cb21-a50f-4202-93a4-391603cef81a", 0L, false)]),
            82L, OfferDescription.NoDate, OfferDescription.NoDate, new OfferLimit(0L, 0L)),
        new(108424178L, "NPC", "Rubber", "", CelesteAssetId, 0L, true, [],
            new OfferCostPackage([new OfferCostItem(1051518749L, 1L, 1.0)]),
            new ItemAmountsOfferCost([new ItemAssetAmount(1051518749L, 1L)]),
            new OfferRewardPackage([new OfferRewardedItem(-2048050889L, 1L, 1.0, [], "99f8cb21-a50f-4202-93a4-391603cef81a", 0L, false)]),
            83L, OfferDescription.NoDate, OfferDescription.NoDate, new OfferLimit(0L, 0L)),
        new(953965280L, "NPC", "Chemicals", "", CelesteAssetId, 0L, true, [],
            new OfferCostPackage([new OfferCostItem(1051518749L, 1L, 1.0)]),
            new ItemAmountsOfferCost([new ItemAssetAmount(1051518749L, 1L)]),
            new OfferRewardPackage([new OfferRewardedItem(-1565478080L, 1L, 1.0, [], "99f8cb21-a50f-4202-93a4-391603cef81a", 0L, false)]),
            84L, OfferDescription.NoDate, OfferDescription.NoDate, new OfferLimit(0L, 0L)),
        new(432182497L, "NPC", "Fabric", "", CelesteAssetId, 0L, true, [],
            new OfferCostPackage([new OfferCostItem(1051518749L, 1L, 1.0)]),
            new ItemAmountsOfferCost([new ItemAssetAmount(1051518749L, 1L)]),
            new OfferRewardPackage([new OfferRewardedItem(-1580114534L, 1L, 1.0, [], "99f8cb21-a50f-4202-93a4-391603cef81a", 0L, false)]),
            85L, OfferDescription.NoDate, OfferDescription.NoDate, new OfferLimit(0L, 0L)),
        new(847600719L, "NPC", "Sheet Metal", "", CelesteAssetId, 0L, true, [],
            new OfferCostPackage([new OfferCostItem(1051518749L, 3L, 1.0)]),
            new ItemAmountsOfferCost([new ItemAssetAmount(1051518749L, 3L)]),
            new OfferRewardPackage([new OfferRewardedItem(-730174445L, 1L, 1.0, [], "99f8cb21-a50f-4202-93a4-391603cef81a", 0L, false)]),
            86L, OfferDescription.NoDate, OfferDescription.NoDate, new OfferLimit(0L, 0L)),
        new(452825094L, "NPC", "Planks", "", CelesteAssetId, 0L, true, [],
            new OfferCostPackage([new OfferCostItem(1051518749L, 3L, 1.0)]),
            new ItemAmountsOfferCost([new ItemAssetAmount(1051518749L, 3L)]),
            new OfferRewardPackage([new OfferRewardedItem(1086410133L, 1L, 1.0, [], "99f8cb21-a50f-4202-93a4-391603cef81a", 0L, false)]),
            87L, OfferDescription.NoDate, OfferDescription.NoDate, new OfferLimit(0L, 0L)),
    ];
}
