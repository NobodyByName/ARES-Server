using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.World.Collision;
using Ares.Structs.Core;
using static Ares.Raid.Tests.World.Collision.SyntheticMaps;

namespace Ares.Raid.Tests.World.Collision;

public sealed class MapCollisionLoaderTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("ares-map-data-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Without_a_directory_no_collision_is_loaded()
    {
        Assert.Null(MapCollisionLoader.Load(null, MapName, out string? problem));
        Assert.Contains("map-data-dir", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_directory_is_reported()
    {
        string missing = Path.Combine(_directory, "missing");
        Assert.Null(MapCollisionLoader.Load(missing, MapName, out string? problem));
        Assert.Contains(missing, problem, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_file_is_reported_with_its_path()
    {
        Assert.Null(MapCollisionLoader.Load(_directory, MapName, out string? problem));
        Assert.Contains(Path.Combine(_directory, MapDataFiles.CollisionFileName(MapName)), problem, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_for_another_game_build_is_refused_with_the_reason()
    {
        Write(GameBuild.SteamBuildId + 1);
        Assert.Null(MapCollisionLoader.Load(_directory, MapName, out string? problem));
        Assert.Contains($"game build {GameBuild.SteamBuildId + 1}, expected {GameBuild.SteamBuildId}", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_for_this_build_is_loaded()
    {
        Write(GameBuild.SteamBuildId);
        MapCollision? map = MapCollisionLoader.Load(_directory, MapName, out string? problem);
        Assert.Null(problem);
        Assert.NotNull(map);
        Assert.Equal(10, new CollisionWorld(map).GroundHeight(0, 0, 100, 200, PlacementFlags.BlocksPawn));
    }

    private void Write(int gameBuildId)
    {
        MapCollisionBuilder builder = Builder(gameBuildId);
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(Body("box", boxes: [Box(new Vector3(10, 10, 10))])), cell, At(0, 0, 0), PlacementFlags.BlocksPawn);
        using FileStream stream = File.Create(Path.Combine(_directory, MapDataFiles.CollisionFileName(MapName)));
        MapCollisionFile.Write(stream, builder.Build());
    }
}
