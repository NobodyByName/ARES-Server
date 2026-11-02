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
    public void Required_options_are_still_required()
    {
        Assert.Throws<CommandLineException>(() => RaidConfiguration.Parse([]));
    }
}
