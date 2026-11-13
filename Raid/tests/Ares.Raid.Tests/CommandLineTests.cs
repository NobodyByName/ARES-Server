using Ares.Common.CommandLine;

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
    public void Map_data_directory_is_optional_and_made_absolute()
    {
        string directory = Directory.CreateTempSubdirectory("ares-raid-config-").FullName;
        try
        {
            string tokenFile = Path.Combine(directory, "operator-token");
            File.WriteAllText(tokenFile, new string('a', 64));
            string[] required = ["--advertise-host", "127.0.0.1", "--control-port", "17778", "--control-token-file", tokenFile];
            Assert.Null(RaidConfiguration.Parse(required).MapDataDirectory);
            Assert.Equal(Path.GetFullPath("maps"), RaidConfiguration.Parse([.. required, "--map-data-dir", "maps"]).MapDataDirectory);
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
}
