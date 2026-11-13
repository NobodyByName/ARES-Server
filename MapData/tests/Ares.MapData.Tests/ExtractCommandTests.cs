using Ares.Common.CommandLine;
using Ares.MapData.Archives;

namespace Ares.MapData.Tests;

public sealed class ExtractCommandTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("ares-mapdata-").FullName;

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Every_option_is_required()
    {
        Assert.Throws<CommandLineException>(() => ExtractCommand.Parse(["--game", _directory, "--keys", "k", "--toc-key", "t"]));
        Assert.Throws<CommandLineException>(() => ExtractCommand.Parse(["--game", _directory, "--keys", "k", "--out", "o"]));
        Assert.Throws<CommandLineException>(() => ExtractCommand.Parse(["--game", _directory, "--keys", "k", "--toc-key", "t", "--out", "o", "--extra", "x"]));
        Assert.Throws<HelpRequestedException>(() => ExtractCommand.Parse(["--help"]));
    }

    [Fact]
    public void A_missing_game_folder_is_a_usage_error()
    {
        (int code, string error) = Run(Path.Combine(_directory, "missing"), Path.Combine(_directory, "keys.txt"));

        Assert.Equal(ExtractCommand.UsageError, code);
        Assert.Contains("game folder not found", error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_keys_file_is_a_usage_error()
    {
        (int code, string error) = Run(_directory, Path.Combine(_directory, "keys.txt"));

        Assert.Equal(ExtractCommand.UsageError, code);
        Assert.Contains("archive keys file not found", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Keys_of_another_build_are_a_usage_error()
    {
        string keys = Path.Combine(_directory, "keys.txt");
        File.WriteAllText(keys, """
            pak-key = 000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F
            container-key = 1, 2, 3, 4, 5, 6, 7, 8
            """);

        (int code, string error) = Run(_directory, keys);

        Assert.Equal(ExtractCommand.UsageError, code);
        Assert.Contains("do not match game build", error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_toc_key_file_holds_one_key_with_optional_prefix_comments_and_whitespace()
    {
        TocKey key = TocKey.Parse("""
            # TOC key
            0x000102030405060708090A0B0C0D0E0F
              101112131415161718191a1b1c1d1e1f  # second half
            """);

        Assert.Equal([.. Enumerable.Range(0, 32).Select(i => (byte)i)], key.Bytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("# only a comment")]
    [InlineData("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E")]
    [InlineData("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F00")]
    [InlineData("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1G")]
    [InlineData("toc-key = 000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")]
    public void Malformed_toc_key_files_are_rejected_without_echoing_them(string text)
    {
        FormatException error = Assert.Throws<FormatException>(() => TocKey.Parse(text));

        Assert.DoesNotContain("0102", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_or_malformed_toc_key_file_is_reported()
    {
        string path = Path.Combine(_directory, "toc-key.txt");
        ExtractCommand command = ExtractCommand.Parse(["--game", _directory, "--keys", "k", "--toc-key", path, "--out", "o"]);
        StringWriter missing = new();
        Assert.Null(command.ReadTocKey(missing));
        Assert.Contains("TOC key file not found", missing.ToString(), StringComparison.Ordinal);

        File.WriteAllText(path, "00");
        StringWriter malformed = new();
        Assert.Null(command.ReadTocKey(malformed));
        Assert.Contains("expected one key of 64 hex digits", malformed.ToString(), StringComparison.Ordinal);
    }

    private (int Code, string Error) Run(string game, string keys)
    {
        ExtractCommand command = ExtractCommand.Parse(
            ["--game", game, "--keys", keys, "--toc-key", Path.Combine(_directory, "toc-key.txt"), "--out", Path.Combine(_directory, "out")]);
        StringWriter output = new();
        StringWriter error = new();
        int code = command.Run(output, error);
        return (code, error.ToString());
    }
}
