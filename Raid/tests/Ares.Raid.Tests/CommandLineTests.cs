using Ares.Common.CommandLine;
using Ares.Common.Game;

namespace Ares.Raid.Tests;

public sealed class CommandLineTests
{
    [Fact]
    public void Version_option_needs_no_other_options()
    {
        Assert.Throws<VersionRequestedException>(() => RaidConfiguration.Parse(["--version"]));
    }

    [Fact]
    public void Unknown_console_modes_are_rejected_before_reading_secrets()
    {
        Assert.Contains("--console", Assert.Throws<CommandLineException>(() => RaidConfiguration.Parse(["--console", "invalid"])).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Log_file_option_requires_a_path()
    {
        Assert.Throws<CommandLineException>(() => RaidConfiguration.Parse(["--log-file"]));
    }

    [Fact]
    public void Game_data_and_content_directories_are_optional_and_made_absolute_and_the_raid_settings_default()
    {
        string directory = Directory.CreateTempSubdirectory("ares-raid-config-").FullName;
        try
        {
            string tokenFile = Path.Combine(directory, "operator-token");
            File.WriteAllText(tokenFile, new string('a', 64));
            string[] required = ["--advertise-host", "127.0.0.1", "--control-port", "17778", "--control-token-file", tokenFile];
            Assert.Null(RaidConfiguration.Parse(required).GameDataDirectory);
            Assert.Equal(Path.GetFullPath("gamedata"), RaidConfiguration.Parse([.. required, "--game-data-dir", "gamedata"]).GameDataDirectory);
            Assert.Null(RaidConfiguration.Parse(required).ContentDirectory);
            Assert.Equal(Path.GetFullPath("content"), RaidConfiguration.Parse([.. required, "--content-dir", "content"]).ContentDirectory);
            Assert.Equal(RaidSettings.Default, RaidConfiguration.Parse(required).Settings);
            Assert.Equal(new RaidSettings(5000, 60, 10),
                RaidConfiguration.Parse([.. required, "--loot-cull-distance", "5000", "--extraction-seconds", "60", "--extraction-ready-seconds", "10"]).Settings);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Required_options_are_still_required()
    {
        Assert.Throws<CommandLineException>(() => RaidConfiguration.Parse([]));
    }

    [Fact]
    public void Max_players_must_be_at_least_one()
    {
        Assert.Contains("--max-players", Assert.Throws<CommandLineException>(() => RaidConfiguration.Parse(["--max-players", "0"])).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("601")]
    public void Fill_seconds_must_be_from_zero_to_ten_minutes(string value)
    {
        Assert.Contains("--fill-seconds", Assert.Throws<CommandLineException>(() => RaidConfiguration.Parse(["--fill-seconds", value])).Message,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--loot-cull-distance", "0")]
    [InlineData("--extraction-seconds", "-1")]
    [InlineData("--extraction-ready-seconds", "-1")]
    public void Raid_settings_out_of_range_are_rejected(string option, string value)
    {
        Assert.Contains(option, Assert.Throws<CommandLineException>(() => RaidConfiguration.Parse([option, value])).Message, StringComparison.Ordinal);
    }
}
