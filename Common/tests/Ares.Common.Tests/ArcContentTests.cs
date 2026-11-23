using Ares.Common.Game.Content;
using Ares.Common.Game.Enemies;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Game.Enemies;

namespace Ares.Common.Tests;

/// <summary>
/// <c>arc-spawners.json</c> and <c>arc-movement.json</c> in a content folder; the spawners are resolved against the
/// extracted game data, which the real-data tests need (<see cref="RealGameData.DirectoryVariable"/>).
/// </summary>
public sealed class ArcContentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ares-arc-content-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [RealDataFact]
    public void The_shipped_dam_spawners_keep_to_routes_the_map_has_and_every_wasp_spawner_has_one()
    {
        MapData dam = RealGameData.Data.Names.Dam;
        IReadOnlyList<ArcSpawner> spawners = ShippedContent.Value.ArcSpawners(RealGameData.Data)[dam.WorldName];
        Assert.Equal(spawners.Count, spawners.Select(spawner => spawner.Id).Distinct(StringComparer.Ordinal).Count());
        ArcSpawner[] wasps = [.. spawners.Where(spawner => spawner.EnemyTypes.Contains(Arcs.Wasp.EnemyType))];
        Assert.NotEmpty(wasps);
        Assert.All(wasps, spawner => Assert.Single(spawner.Routes));
        Assert.All(spawners.SelectMany(spawner => spawner.Routes), route => Assert.Contains(dam.SplinePaths, path => path.Name == route));
    }

    [Fact]
    public void Every_arc_the_servers_spawn_has_a_movement_profile()
    {
        Assert.Contains(AresContent.ArcMovementFile, AresContent.FileNames);
        Assert.Equal(Arcs.All, ShippedContent.Value.ArcMovement.Keys);
    }

    [RealDataFact]
    public void A_spawner_on_a_route_the_map_lacks_is_refused()
    {
        ShippedContent.CopyTo(_directory);
        string path = Path.Combine(_directory, AresContent.ArcSpawnersFile);
        string text = File.ReadAllText(path);
        string route = ShippedContent.Value.ArcSpawners(RealGameData.Data)[RealGameData.Data.Names.Dam.WorldName].First(spawner => spawner.Routes.Count > 0).Routes[0];
        File.WriteAllText(path, text.Replace($"\"{route}\"", "\"BP_SplinePath_Missing\"", StringComparison.Ordinal));
        AresContentException error = Assert.Throws<AresContentException>(() => AresContent.Load(_directory).ArcSpawners(RealGameData.Data));
        Assert.Contains("BP_SplinePath_Missing", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_movement_profile_out_of_range_or_for_an_unknown_arc_is_refused()
    {
        ShippedContent.CopyTo(_directory);
        string path = Path.Combine(_directory, AresContent.ArcMovementFile);
        string text = File.ReadAllText(path);
        File.WriteAllText(path, text.Replace("\"turnBackChance\": 0.15", "\"turnBackChance\": 1.5", StringComparison.Ordinal));
        Assert.Contains("turnBackChance", Assert.Throws<AresContentException>(() => AresContent.Load(_directory)).Message, StringComparison.Ordinal);
        File.WriteAllText(path, text.Replace("\"wasp\"", "\"hornet\"", StringComparison.Ordinal));
        Assert.Contains("hornet", Assert.Throws<AresContentException>(() => AresContent.Load(_directory)).Message, StringComparison.Ordinal);
    }
}
