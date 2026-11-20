using M = Ares.Structs.Game.Scripts.ScriptMemberDefinition;
using T = Ares.Structs.Game.Scripts.ScriptMemberType;

namespace Ares.Structs.Game.Scripts;

/// <summary>Types 8629 to 8636 of <see cref="ScriptTypes.ByName"/>.</summary>
internal static class ScriptTypesPart41
{
    public static IReadOnlyList<ScriptTypeDefinition> Entries { get; } =
    [
        new("UWorldPartitionStreamingHandler", "UStreamingHandler", []),
        new("UWorldQuestComponent", "UQuestComponentBase", []),
        new("UWorldQuestDataAsset", "UQuestDataAssetBase", []),
        new("UWorldSpaceWidgetBase", "UEmbarkUserWidget", []),
        new("UWorldSpaceWidgetMappings", "UDataAsset",
            [
                new M("PlayerWidgetClass", T.Object),
                new M("TaggedWidgetClass", T.Object),
                new M("DefaultPingIntentTemplate", T.Object),
                new M("PingIntentTemplate", T.MapOf(T.Enum("EPioneerPingIntent"), T.Object)),
                new M("DefaultIntentCategoryTemplate", T.MapOf(T.Enum("EHoverDescriptionCategory"), T.Object)),
            ]),
        new("UWorldSpaceWidgetProjectorWidget", "UBaseHUDWidget",
            [
                new M("MainCanvas", T.Object),
            ]),
        new("UXPEventCategoryDataAsset", "UPersistenceDataAsset", []),
        new("UZiplineItem", "UItemBase",
            [
                new M("ZiplineAnchorClass", T.Object),
                new M("MaxDistanceFromPlayerToPlace", T.Double),
                new M("ZiplinesDistanceLimit", T.Struct("FFloatInterval")),
                new M("FirstPreviewClass", T.Object),
                new M("SecondPreviewClass", T.Object),
                new M("BlockedPlacementClasses", T.ArrayOf(T.Object)),
            ]),
    ];
}
