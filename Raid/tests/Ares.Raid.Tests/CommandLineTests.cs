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
    public void Required_options_are_still_required()
    {
        Assert.Throws<CommandLineException>(() => RaidConfiguration.Parse([]));
    }
}
