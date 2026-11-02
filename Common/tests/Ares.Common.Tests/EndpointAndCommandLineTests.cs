using Ares.Common.CommandLine;
using Ares.Common.Endpoints;

namespace Ares.Common.Tests;

public sealed class EndpointAndCommandLineTests
{
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("play.example.com", true)]
    [InlineData("host:7777", false)]
    [InlineData("-bad.example", false)]
    [InlineData("a/b", false)]
    [InlineData("", false)]
    public void Connection_hosts_are_ipv4_or_dns_names(string host, bool valid)
    {
        Assert.Equal(valid, ConnectionHost.IsValid(host));
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("[::1]", true)]
    [InlineData("localhost", true)]
    [InlineData("192.0.2.10", false)]
    [InlineData("example.com", false)]
    public void Loopback_hosts_are_recognised(string host, bool loopback)
    {
        Assert.Equal(loopback, Loopback.IsLoopbackHost(host));
    }

    [Fact]
    public void Arguments_accept_values_flags_and_equals_syntax()
    {
        CommandLineArguments arguments = CommandLineArguments.Parse(["--port", "18080", "--bind=0.0.0.0", "--allow"], new HashSet<string> { "allow" });

        Assert.Equal(18080, arguments.GetInt32("port", 1));
        Assert.Equal("0.0.0.0", arguments.GetString("bind"));
        Assert.True(arguments.Has("allow"));
        arguments.RejectUnknown();
    }

    [Fact]
    public void Help_and_version_are_known_options()
    {
        CommandLineArguments arguments = CommandLineArguments.Parse(["--version"], new HashSet<string> { "help", "version" });

        Assert.True(arguments.VersionRequested);
        Assert.False(arguments.HelpRequested);
        arguments.RejectUnknown();
    }

    [Fact]
    public void Version_is_a_date_with_a_counter()
    {
        Assert.Matches("^20[0-9]{2}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])(0[1-9]|[1-9][0-9])$", AresVersion.Current);
    }

    [Fact]
    public void Unknown_repeated_or_incomplete_arguments_fail()
    {
        CommandLineArguments typo = CommandLineArguments.Parse(["--prot", "1"], new HashSet<string>());

        Assert.Throws<CommandLineException>(typo.RejectUnknown);
        Assert.Throws<CommandLineException>(() => CommandLineArguments.Parse(["--a", "1", "--a", "2"], new HashSet<string>()));
        Assert.Throws<CommandLineException>(() => CommandLineArguments.Parse(["--a"], new HashSet<string>()));
        Assert.Throws<CommandLineException>(() => CommandLineArguments.Parse(["stray"], new HashSet<string>()));
    }
}
