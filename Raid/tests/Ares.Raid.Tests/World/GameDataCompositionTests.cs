using Ares.Common.Testing;
using Ares.Raid.World;

namespace Ares.Raid.Tests.World;

/// <summary>The game data the Raid loads at startup reaches its world host and every raid.</summary>
public sealed class GameDataCompositionTests
{
    [Fact]
    public void The_world_host_and_the_raid_rules_require_game_data_and_content()
    {
        Assert.Throws<ArgumentNullException>(() => new RaidGameData(null!, ShippedContent.Value));
        Assert.Throws<ArgumentNullException>(() => new RaidGameData(SyntheticGameData.Create(), null!));
        Assert.Throws<ArgumentNullException>(() => new DamWorlds(null!, () => 1));
    }

    [Fact]
    public void The_world_host_and_its_raids_hold_the_loaded_game_data()
    {
        RaidGameData data = TestGameData.Raid;
        DamWorlds worlds = new(data, () => 1, random: () => 7);

        worlds.Prepare("a", [1]);

        Assert.Same(data, worlds.GameData);
        Assert.Same(data, worlds.Current!.GameData);
        Assert.Same(data.Data.Names.Dam, data.Map);
    }
}
