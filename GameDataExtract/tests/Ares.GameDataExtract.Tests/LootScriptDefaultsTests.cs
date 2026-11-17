using Ares.GameDataExtract.Loot;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.Loot;

namespace Ares.GameDataExtract.Tests;

public sealed class LootScriptDefaultsTests
{
    private readonly ScriptCache _cache = SyntheticLootScripts.Cache().Parse();

    [Fact]
    public void Tweakables_come_from_the_constructor_stores_and_the_map_entries()
    {
        LootSpawnTweakables tweakables = LootScriptDefaults.Tweakables(_cache);

        Assert.Equal(SyntheticLootScripts.Tweakables with { ContainerContentsWeights = tweakables.ContainerContentsWeights }, tweakables);
        Assert.Equal(SyntheticLootScripts.Tweakables.ContainerContentsWeights, tweakables.ContainerContentsWeights);
    }

    [Fact]
    public void Target_stacks_and_the_net_cull_distance_come_from_the_default_statements()
    {
        Assert.Equal(SyntheticLootScripts.TargetStacks, LootScriptDefaults.TargetStacks(_cache));
        Assert.Equal(SyntheticLootScripts.CullDistance, LootScriptDefaults.NetCullDistance(_cache));
    }

    [Fact]
    public void Data_asset_and_interaction_defaults_are_found_by_member_name()
    {
        Assert.Equal(new LootContainerDataAssetDefaults(true, SyntheticLootScripts.MaxPerGroup, 0), LootScriptDefaults.DataAsset(_cache));

        LootInteractionDefaults interaction = LootScriptDefaults.Interaction(_cache);

        Assert.Equal(
            new LootInteractionDefaults(0, 0.0, false, SyntheticLootScripts.AlignmentSeconds, false, 0, SyntheticLootScripts.SlotCount,
                SyntheticLootScripts.StandardDistance, SyntheticLootScripts.StationaryDistance),
            interaction);
    }

    [Fact]
    public void A_float_reads_as_the_double_with_its_seven_significant_digits()
    {
        Assert.Equal(0.2, LootNumbers.Single(0.2f));
        Assert.Equal(3.5, LootNumbers.Single(3.5f));
    }
}
