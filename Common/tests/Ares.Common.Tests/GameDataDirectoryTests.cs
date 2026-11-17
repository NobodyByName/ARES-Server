using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Ares.Common.GameData;
using Ares.Structs.Core;

namespace Ares.Common.Tests;

public sealed class GameDataDirectoryTests : IDisposable
{
    private const string Table = "fruits.json";
    private const string TableJson = """
        [
          { "name": "apple", "weight": 120 },
          { "name": "pear", "weight": 95 }
        ]

        """;

    private readonly string _directory = Directory.CreateTempSubdirectory("ares-gamedata-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void A_valid_directory_reads_its_tables_and_files()
    {
        WriteFile(Table, TableJson);
        WriteFile("maps/Map/Map.collision", "binary");
        WriteManifest(Entry(Table, TableJson, 2), Entry("maps/Map/Map.collision", "binary"));

        GameDataDirectory data = GameDataDirectory.Open(_directory);

        Assert.Equal(GameBuild.SteamBuildId, data.Manifest.GameBuildId);
        Assert.Equal("2026110201", data.Manifest.ExtractorVersion);
        Assert.True(data.Contains(Table));
        Assert.False(data.Contains("other.json"));
        Assert.Equal([new Fruit("apple", 120), new Fruit("pear", 95)], data.ReadTable(Table, TestTableContext.Default.Fruit));
        using FileStream stream = data.OpenFile("maps/Map/Map.collision");
        using StreamReader reader = new(stream);
        Assert.Equal("binary", reader.ReadToEnd());
    }

    [Fact]
    public void A_missing_directory_is_reported()
    {
        string missing = Path.Combine(_directory, "missing");

        Assert.Contains($"Game data directory not found: {missing}", Open(missing), StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_manifest_is_reported()
    {
        Assert.Contains($"Game data manifest not found: {Path.Combine(_directory, GameDataFiles.ManifestFileName)}", Open(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{ "extractorVersion": "1", "files": [] }""")]
    [InlineData("""{ "gameBuildId": 1, "extractorVersion": "1", "files": [], "extra": 1 }""")]
    [InlineData("""{ "gameBuildId": "1", "extractorVersion": "1", "files": [] }""")]
    [InlineData("""{ "gameBuildId": 1, "extractorVersion": null, "files": [] }""")]
    public void A_malformed_manifest_is_reported(string manifest)
    {
        WriteFile(GameDataFiles.ManifestFileName, manifest);

        string error = Open();

        Assert.Contains("Game data manifest", error, StringComparison.Ordinal);
        Assert.Contains("ARES-GameDataExtract extract again", error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_manifest_of_another_game_build_is_refused()
    {
        WriteManifest(GameBuild.SteamBuildId + 1);

        Assert.Contains($"extracted from game build {GameBuild.SteamBuildId + 1}, but this ARES supports game build {GameBuild.SteamBuildId}", Open(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../outside.json")]
    [InlineData("/absolute.json")]
    [InlineData("maps/./x.json")]
    [InlineData("maps\\x.json")]
    [InlineData(GameDataFiles.ManifestFileName)]
    public void Unsafe_file_names_are_refused(string name)
    {
        WriteManifest(new GameDataFileEntry(name, new string('0', 64), 0));

        Assert.Contains($"'{name}' is not a valid file name", Open(), StringComparison.Ordinal);
    }

    [Fact]
    public void Duplicate_entries_bad_hashes_and_negative_row_counts_are_refused()
    {
        WriteFile(Table, TableJson);
        WriteManifest(Entry(Table, TableJson, 2), Entry("FRUITS.json", TableJson, 2));
        Assert.Contains("'FRUITS.json' is listed twice", Open(), StringComparison.Ordinal);

        WriteManifest(new GameDataFileEntry(Table, new string('A', 64), 2));
        Assert.Contains("SHA-256 that is not 64 lowercase hex digits", Open(), StringComparison.Ordinal);

        WriteManifest(Entry(Table, TableJson, -1));
        Assert.Contains("negative row count", Open(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_listed_file_that_is_missing_is_reported()
    {
        WriteManifest(Entry(Table, TableJson, 2));

        Assert.Contains($"Game data file {Path.Combine(_directory, Table)} is listed in", Open(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_file_changed_after_extraction_is_refused()
    {
        WriteFile(Table, TableJson);
        WriteFile("other.bin", "binary");
        WriteManifest(Entry(Table, TableJson, 2), Entry("other.bin", "binary"));
        WriteFile(Table, TableJson.Replace("120", "121", StringComparison.Ordinal));
        WriteFile("other.bin", "changed");
        GameDataDirectory data = GameDataDirectory.Open(_directory);

        GameDataException table = Assert.Throws<GameDataException>(() => data.ReadTable(Table, TestTableContext.Default.Fruit));
        GameDataException file = Assert.Throws<GameDataException>(() => data.OpenFile("other.bin"));

        Assert.Contains("does not match its SHA-256 in the manifest", table.Message, StringComparison.Ordinal);
        Assert.Contains("does not match its SHA-256 in the manifest", file.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_table_is_reported_with_its_name()
    {
        WriteManifest();

        GameDataException error = Assert.Throws<GameDataException>(() => GameDataDirectory.Open(_directory).ReadTable(Table, TestTableContext.Default.Fruit));

        Assert.Contains($"has no file '{Table}'", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TableJson, 3, "holds 2 rows, but the manifest lists 3")]
    [InlineData(TableJson, null, "is not a table")]
    [InlineData("""{ "name": "apple", "weight": 1 }""", 1, "a table is a JSON array of rows")]
    [InlineData("""[{ "name": "apple" }]""", 1, "is malformed")]
    [InlineData("""[{ "name": "apple", "weight": 1, "colour": "red" }]""", 1, "is malformed")]
    [InlineData("[null]", 1, "row 0 is null")]
    [InlineData("[] []", 0, "is malformed")]
    public void A_malformed_table_is_reported(string json, int? rows, string problem)
    {
        WriteFile(Table, json);
        WriteManifest(Entry(Table, json, rows));

        GameDataException error = Assert.Throws<GameDataException>(() => GameDataDirectory.Open(_directory).ReadTable(Table, TestTableContext.Default.Fruit));

        Assert.Contains(problem, error.Message, StringComparison.Ordinal);
    }

    private string Open(string? directory = null) => Assert.Throws<GameDataException>(() => GameDataDirectory.Open(directory ?? _directory)).Message;

    private void WriteManifest(params GameDataFileEntry[] files) => WriteManifest(GameBuild.SteamBuildId, files);

    private void WriteManifest(int gameBuildId, params GameDataFileEntry[] files)
    {
        JsonArray entries = [];
        foreach (GameDataFileEntry file in files)
        {
            JsonObject entry = new() { ["name"] = file.Name, ["sha256"] = file.Sha256 };
            if (file.RowCount is { } rows)
            {
                entry["rowCount"] = rows;
            }

            entries.Add(entry);
        }

        JsonObject manifest = new() { ["gameBuildId"] = gameBuildId, ["extractorVersion"] = "2026110201", ["files"] = entries };
        WriteFile(GameDataFiles.ManifestFileName, manifest.ToJsonString());
    }

    private static GameDataFileEntry Entry(string name, string content, int? rows = null) =>
        new(name, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content))), rows);

    private void WriteFile(string name, string content)
    {
        string path = Path.Combine(_directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}

/// <summary>A made-up table row.</summary>
public sealed record Fruit(string Name, int Weight);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(Fruit))]
internal sealed partial class TestTableContext : JsonSerializerContext;
