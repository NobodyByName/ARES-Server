using Ares.Structs.Net.GameplayTags;

namespace Ares.Structs.Game.World;

/// <summary>Round configurations available on the Dam map.</summary>
public static class DamRounds
{
    /// <summary>All configurations.</summary>
    public static IReadOnlyList<RoundConfiguration> All { get; } =
    [
        new("Spearmint", "/Game/Pioneer/GameMode/RoundConfiguration/DA_RoundConfiguration_Spearmint", "Online.RoundConfiguration.Spearmint", 1360114865UL, true,
            new GameplayTagQuery(["RoundConfiguration.Extraction", "RoundConfiguration.Loot.Standard", "RoundConfiguration.Spearmint"], [0, 1, 1, 3, 0, 1, 2]),
            new GameplayTagQuery(["RoundConfiguration.Extraction", "RoundConfiguration.Loot.Standard", "RoundConfiguration.Spearmint", "RoundConfiguration.PvE"], [0, 1, 1, 4, 0, 1, 2, 3]),
            [new RoundDataLayer("/Game/Pioneer/Core/DataLayers/RoundConfiguration_Bunker_Standard", 0xA1D89D7F9AF17D72UL), new RoundDataLayer("/Game/Pioneer/Core/DataLayers/RoundConfiguration_ToxicSwamp_Standard", 0xB646F2CA49F8D82DUL), new RoundDataLayer("/Game/Pioneer/Core/DataLayers/RoundConfiguration_LockedGate_Standard", 0xADEE8A03856BDC75UL), new RoundDataLayer("/Game/Pioneer/Core/DataLayers/RoundConfiguration_LockedDoors_Standard", 0xA9F4FCEACED56713UL)],
            ["DialogueBucketParameter.RoundConfig.Matriarch"]),
    ];
}
