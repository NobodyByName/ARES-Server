using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json.Serialization;
using Ares.Common.GameData;
using Ares.GameDataExtract.Output;
using Ares.Structs.Core;

namespace Ares.GameDataExtract.Tests;

public sealed class GameDataWriterTests : IDisposable
{
    private const string Table = "crates.json";
    private const string MapTable = "maps/Map/crates.json";
    private const string Blob = "maps/Map/Map.collision";

    private static readonly Crate[] s_crates =
    [
        new("/Game/Crates/Wooden", 1.5, ["Loot.Common", "Loot.Tools"]),
        new("/Game/Crates/Ärztekoffer <'&'>", 0.1, [], Note: "non-ASCII and HTML characters stay readable"),
        new("/Game/Crates/Huge", 12345678.9, ["Loot.Rare"]),
    ];

    private readonly string _directory = Directory.CreateTempSubdirectory("ares-gamedata-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Tables_and_files_read_back_through_the_reader()
    {
        string output = Path.Combine(_directory, "out");
        Write(output);

        GameDataDirectory data = GameDataDirectory.Open(output);

        Assert.Equal(GameBuild.SteamBuildId, data.Manifest.GameBuildId);
        Assert.Equal("2026110201", data.Manifest.ExtractorVersion);
        Assert.Equal([Table, Blob, MapTable], data.Manifest.Files.Select(file => file.Name));
        Assert.Equal([3, null, 1], data.Manifest.Files.Select(file => file.RowCount));
        ImmutableArray<Crate> crates = data.ReadTable(Table, WriterTestContext.Default.Crate);
        Assert.Equal(s_crates.Length, crates.Length);
        for (int i = 0; i < s_crates.Length; i++)
        {
            Assert.Equal(s_crates[i].Path, crates[i].Path);
            Assert.Equal(s_crates[i].Weight, crates[i].Weight);
            Assert.Equal(s_crates[i].Tags, crates[i].Tags);
            Assert.Equal(s_crates[i].Note, crates[i].Note);
        }

        Assert.Single(data.ReadTable(MapTable, WriterTestContext.Default.Crate));
        using FileStream stream = data.OpenFile(Blob);
        using MemoryStream content = new();
        stream.CopyTo(content);
        Assert.Equal([1, 2, 3], content.ToArray());
    }

    [Fact]
    public void Writing_twice_gives_identical_bytes_whatever_the_culture()
    {
        string first = Path.Combine(_directory, "first");
        string second = Path.Combine(_directory, "second");
        CultureInfo culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Write(first);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            Write(second);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        string[] files = [GameDataFiles.ManifestFileName, Table, MapTable, Blob];
        foreach (string file in files)
        {
            Assert.Equal(File.ReadAllBytes(Path.Combine(first, file)), File.ReadAllBytes(Path.Combine(second, file)));
        }
    }

    [Fact]
    public void Json_is_indented_with_unix_newlines_and_readable_text()
    {
        string output = Path.Combine(_directory, "out");
        Write(output);

        string text = File.ReadAllText(Path.Combine(output, Table));

        Assert.StartsWith("[\n  {\n    \"path\": \"/Game/Crates/Wooden\",\n    \"weight\": 1.5,\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', text);
        Assert.EndsWith("]\n", text, StringComparison.Ordinal);
        Assert.Contains("\"path\": \"/Game/Crates/Ärztekoffer <'&'>\"", text, StringComparison.Ordinal);
        Assert.Contains("\"weight\": 12345678.9,", text, StringComparison.Ordinal);
        Assert.Contains("\"note\": null", text, StringComparison.Ordinal);
        Assert.Contains("\n  \"files\": [\n    {\n      \"name\": \"crates.json\",\n      \"sha256\": \"", File.ReadAllText(Path.Combine(output, GameDataFiles.ManifestFileName)), StringComparison.Ordinal);
    }

    [Fact]
    public void A_new_extraction_removes_the_earlier_manifest_before_replacing_files()
    {
        string output = Path.Combine(_directory, "out");
        Write(output);
        GameDataWriter writer = new(output);

        writer.WriteTable(Table, s_crates[..1], WriterTestContext.Default.Crate);

        Assert.False(File.Exists(Path.Combine(output, GameDataFiles.ManifestFileName)));
        Assert.Empty(Directory.GetFiles(output, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void Null_members_are_written_as_null_and_read_back()
    {
        string output = Path.Combine(_directory, "out");
        GameDataWriter writer = new(output);
        Shelf[] shelves = [new("A", null, 2), new("B", "Tools", 3)];

        writer.WriteTable(Table, shelves, WriterTestContext.Default.Shelf);
        writer.WriteManifest(GameBuild.SteamBuildId, "2026110201");

        Assert.Contains("\"label\": null", File.ReadAllText(Path.Combine(output, Table)), StringComparison.Ordinal);
        Assert.Contains("\"note\": null", File.ReadAllText(Path.Combine(output, Table)), StringComparison.Ordinal);
        Assert.Equal(shelves, GameDataDirectory.Open(output).ReadTable(Table, WriterTestContext.Default.Shelf));
    }

    [Theory]
    [InlineData("../outside.json")]
    [InlineData("/absolute.json")]
    [InlineData(GameDataFiles.ManifestFileName)]
    public void Invalid_names_are_rejected(string name)
    {
        GameDataWriter writer = new(Path.Combine(_directory, "out"));

        Assert.Throws<ArgumentException>(() => writer.WriteFile(name, _ => { }));
    }

    private static void Write(string output)
    {
        GameDataWriter writer = new(output);
        writer.WriteTable(MapTable, s_crates[2..], WriterTestContext.Default.Crate);
        writer.WriteFile(Blob, stream => stream.Write([1, 2, 3]));
        writer.WriteTable(Table, s_crates, WriterTestContext.Default.Crate);
        writer.WriteManifest(GameBuild.SteamBuildId, "2026110201");
    }
}

/// <summary>A made-up table row.</summary>
public sealed record Crate(string Path, double Weight, ImmutableArray<string> Tags, string? Note = null);

/// <summary>A made-up table row with a nullable member the reader requires (no default) and an optional one.</summary>
public sealed record Shelf(string Name, string? Label, int Count, string? Note = null);

/// <summary>The made-up rows' metadata, with the null handling of <c>GameDataJsonContext</c> (nulls written).</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(Crate))]
[JsonSerializable(typeof(Shelf))]
internal sealed partial class WriterTestContext : JsonSerializerContext;
