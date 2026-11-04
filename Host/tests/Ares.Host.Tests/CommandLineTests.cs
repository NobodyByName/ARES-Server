using Ares.Common.CommandLine;

namespace Ares.Host.Tests;

public sealed class CommandLineTests : IDisposable
{
    private readonly string _keysFile = Path.GetTempFileName();
    private readonly string _configFile = Path.GetTempFileName();

    public CommandLineTests()
    {
        File.WriteAllText(_keysFile, ArchiveKeysTests.DummyFile);
        File.WriteAllText(_configFile, "mongodb-uri = mongodb://127.0.0.1:27017/?directConnection=true");
    }

    public void Dispose()
    {
        File.Delete(_keysFile);
        File.Delete(_configFile);
    }

    [Fact]
    public void Defaults_listen_on_loopback()
    {
        HostConfiguration configuration = Parse();

        Assert.Equal("http://127.0.0.1:18080", configuration.Settings.PublicOrigin);
        Assert.Equal("http://127.0.0.1:18080/", configuration.Settings.Issuer);
        Assert.Null(configuration.SessionControlUrl);
        Assert.Equal(ArchiveKeysTests.DummySha256, configuration.Settings.ArchiveKeys.Fingerprint());
    }

    [Theory]
    [InlineData("--bind", "0.0.0.0")]
    [InlineData("--public-origin", "http://192.0.2.4:18080")]
    public void Network_exposure_needs_explicit_opt_in(string option, string value)
    {
        Assert.Throws<CommandLineException>(() => Parse(option, value));
        Parse(option, value, "--allow-network-clients");
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("interactive")]
    [InlineData("plain")]
    [InlineData("json")]
    public void Console_mode_is_explicit(string mode)
    {
        HostConfiguration configuration = Parse("--console", mode, "--log-file", "backend.log");
        Assert.Equal(mode, configuration.ConsoleMode);
        Assert.Equal("backend.log", configuration.LogFile);
    }

    [Fact]
    public void Host_does_not_start_without_the_archive_keys_file()
    {
        string missing = _keysFile + ".missing";

        Assert.Contains("not found", Assert.Throws<CommandLineException>(() => HostConfiguration.Parse(["--config", _configFile, "--archive-keys-file", missing])).Message,
            StringComparison.Ordinal);
        Assert.Contains("archive-keys.txt", Assert.Throws<CommandLineException>(() => HostConfiguration.Parse(["--config", _configFile])).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Archive_keys_must_be_well_formed_and_match_the_supported_build()
    {
        Assert.Contains("do not match", Assert.Throws<CommandLineException>(() => HostConfiguration.Parse(["--config", _configFile, "--archive-keys-file", _keysFile])).Message,
            StringComparison.Ordinal);
        File.WriteAllText(_keysFile, "pak-key = 00");
        Assert.Contains("hex digits", Assert.Throws<CommandLineException>(() => Parse()).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Version_option_needs_no_other_options()
    {
        Assert.Throws<VersionRequestedException>(() => HostConfiguration.Parse(["--version"]));
    }

    [Theory]
    [InlineData("--public-origin", "http://127.0.0.1:9999")]
    [InlineData("--public-origin", "https://127.0.0.1:18080")]
    [InlineData("--public-origin", "http://127.0.0.1:18080/path")]
    [InlineData("--session-control-url", "http://127.0.0.1:17778")]
    [InlineData("--game-build", "abc")]
    [InlineData("--http-only", "x")]
    [InlineData("--data-dir", "/tmp/x")]
    [InlineData("--console", "invalid")]
    public void Invalid_options_are_rejected(string option, string value)
    {
        Assert.Throws<CommandLineException>(() => Parse(option, value));
    }

    /// <summary>Parses options using the test configuration and archive keys.</summary>
    private HostConfiguration Parse(params string[] args)
    {
        return HostConfiguration.Parse(["--config", _configFile, .. args, "--archive-keys-file", _keysFile], ArchiveKeysTests.DummySha256);
    }
}
