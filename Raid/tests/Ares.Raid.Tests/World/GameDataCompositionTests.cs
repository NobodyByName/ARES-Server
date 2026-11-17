using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Raid.World;

namespace Ares.Raid.Tests.World;

/// <summary>The game data the Raid loads at startup reaches its world host.</summary>
public sealed class GameDataCompositionTests
{
    [Fact]
    public void The_world_host_holds_the_loaded_game_data()
    {
        GameData data = SyntheticGameData.Create();

        Assert.Same(data, new DamWorlds(() => 1, gameData: data).GameData);
        Assert.Null(new DamWorlds(() => 1).GameData);
    }
}
