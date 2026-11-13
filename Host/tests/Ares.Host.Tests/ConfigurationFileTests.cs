using System.Net;
using Ares.Common.CommandLine;
using Ares.Host.Services;
using Ares.Structs.Core;

namespace Ares.Host.Tests;

public sealed class ConfigurationFileTests : IDisposable
{
    private const string MongoConfiguration = "\nmongodb-uri = mongodb://127.0.0.1:27017/?directConnection=true\n";
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ares-host-config-" + Guid.NewGuid().ToString("N"));

    public ConfigurationFileTests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "archive-keys.txt"), ArchiveKeysTests.DummyFile);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void MongoDB_connection_files_are_relative_and_cli_database_overrides_the_file()
    {
        File.WriteAllText(Path.Combine(_directory, "mongo-uri"), "mongodb://localhost:27017/?directConnection=true\n");
        File.WriteAllText(Path.Combine(_directory, HostConfigurationFile.DefaultName), "mongodb-uri = mongodb://198.51.100.2:27017\nmongodb-uri-file = mongo-uri\nmongodb-database = private_server\n");
        HostConfiguration configuration = Parse("--mongodb-database", "other_server");
        Assert.Equal("mongodb://localhost:27017/?directConnection=true", configuration.Settings.MongoDbUri);
        Assert.Equal("other_server", configuration.Settings.MongoDbDatabase);
    }

    [Fact]
    public void MongoDB_connection_comes_from_configuration_or_an_explicit_override()
    {
        File.WriteAllText(Path.Combine(_directory, HostConfigurationFile.DefaultName), "mongodb-uri = mongodb://database.internal:27018");
        Assert.Equal("mongodb://database.internal:27018", Parse().Settings.MongoDbUri);
        Assert.Equal("mongodb://another.internal:27019", Parse("--mongodb-uri", "mongodb://another.internal:27019").Settings.MongoDbUri);
        File.WriteAllText(Path.Combine(_directory, HostConfigurationFile.DefaultName), "");
        Assert.Contains("requires mongodb-uri or mongodb-uri-file", Assert.Throws<CommandLineException>(() => Parse()).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_MongoDB_uris_do_not_echo_credentials_in_errors()
    {
        File.WriteAllText(Path.Combine(_directory, "mongo-uri"), "not-mongodb://secret-user:secret-password@localhost");
        File.WriteAllText(Path.Combine(_directory, HostConfigurationFile.DefaultName), "mongodb-uri-file = mongo-uri\n");
        string message = Assert.Throws<CommandLineException>(() => Parse()).Message;
        Assert.DoesNotContain("secret", message, StringComparison.Ordinal);
        Assert.Contains("MongoDB", message, StringComparison.Ordinal);
    }

    [Fact]
    public void First_launch_creates_a_documented_file_with_the_existing_defaults()
    {
        HostConfiguration configuration = Parse();
        string path = Path.Combine(_directory, HostConfigurationFile.DefaultName);
        Assert.Equal(path, configuration.ConfigurationFile);
        Assert.Contains("# NETWORK", File.ReadAllText(path), StringComparison.Ordinal);
        Assert.Equal("mongodb://127.0.0.1:27017/?directConnection=true", configuration.Settings.MongoDbUri);
        Assert.Equal(IPAddress.Loopback, configuration.Bind);
        Assert.Equal(18080, configuration.Port);
        Assert.False(configuration.AllowNetworkClients);
        Assert.Equal("http://127.0.0.1:18080", configuration.Settings.PublicOrigin);
        Assert.Equal(HostSettings.DefaultServerName, configuration.Settings.ServerName);
        Assert.Empty(configuration.Settings.Motd);
        Assert.Equal(GameBuild.SteamBuildId.ToString(System.Globalization.CultureInfo.InvariantCulture), configuration.Settings.GameBuild);
        Assert.Equal(HostSettings.DefaultRaidCapacity, configuration.Settings.RaidCapacity);
        Assert.Null(configuration.Raids);
        Assert.Null(configuration.Settings.IdentityKey);
        Assert.Null(configuration.LogFile);
        Assert.Equal("auto", configuration.ConsoleMode);
        Assert.Equal(ArchiveKeysTests.DummySha256, configuration.Settings.ArchiveKeys.Fingerprint());
    }

    [Fact]
    public void Generated_file_is_left_for_editing_even_when_archive_keys_are_missing()
    {
        File.Delete(Path.Combine(_directory, "archive-keys.txt"));
        Assert.Throws<CommandLineException>(() => Parse());
        Assert.True(File.Exists(Path.Combine(_directory, HostConfigurationFile.DefaultName)));
    }

    [Fact]
    public void File_configures_all_settings_and_resolves_paths_from_its_own_directory()
    {
        File.WriteAllText(Path.Combine(_directory, "operator-token"), new string('a', 64));
        File.WriteAllText(Path.Combine(_directory, "ARES-Raid"), "");
        File.WriteAllBytes(Path.Combine(_directory, "identity-key"), new byte[32]);
        Write("""
            # Full-line comment
            port = 19090
            bind = 0.0.0.0
            allow-network-clients = true
            public-origin = http://192.0.2.4:19090
            server-name = " My server "
            motd = "Dam #1 = ready"
            game-build = 12345
            raid-capacity = 2
            archive-keys-file = archive-keys.txt
            auth-key-file = identity-key
            raid-executable = ARES-Raid
            session-control-token-file = operator-token
            raid-players = 4
            raid-fill-seconds = 45
            raid-port = 7000
            raid-control-port = 17000
            raid-lighting = Lighting.GreyMorning
            map-data-dir = maps
            console = json
            log-file = logs/host.log
            """);

        HostConfiguration configuration = Parse("--config", Path.Combine(_directory, HostConfigurationFile.DefaultName));
        Assert.Equal(19090, configuration.Port);
        Assert.Equal(IPAddress.Any, configuration.Bind);
        Assert.True(configuration.AllowNetworkClients);
        Assert.Equal("http://192.0.2.4:19090", configuration.Settings.PublicOrigin);
        Assert.Equal(" My server ", configuration.Settings.ServerName);
        Assert.Equal("Dam #1 = ready", configuration.Settings.Motd);
        Assert.Equal("12345", configuration.Settings.GameBuild);
        Assert.Equal(2, configuration.Settings.RaidCapacity);
        Assert.Equal(new byte[32], configuration.Settings.IdentityKey);
        Assert.Equal(new RaidLaunch(Path.Combine(_directory, "ARES-Raid"), Path.Combine(_directory, "operator-token"), new string('a', 64), IPAddress.Any,
            "192.0.2.4", 7000, 17000, 4, 45, "Lighting.GreyMorning", Path.Combine(_directory, "maps")), configuration.Raids);
        Assert.Equal("json", configuration.ConsoleMode);
        Assert.Equal(Path.Combine(_directory, "logs/host.log"), configuration.LogFile);
    }

    [Fact]
    public void Existing_file_is_not_rewritten_and_cli_overrides_remain_relative_to_working_directory()
    {
        const string Content = "# Keep my comments\nport = 19090\nserver-name = Original\nlog-file = logs/original.log\n";
        Write(Content);
        HostConfiguration configuration = Parse("--port", "19091", "--server-name", "Override", "--log-file", "override.log");
        Assert.Equal(19091, configuration.Port);
        Assert.Equal("http://127.0.0.1:19091", configuration.Settings.PublicOrigin);
        Assert.Equal("Override", configuration.Settings.ServerName);
        Assert.Equal("override.log", configuration.LogFile);
        Assert.Equal(Content + MongoConfiguration, File.ReadAllText(configuration.ConfigurationFile!));
    }

    [Fact]
    public void Cli_flag_can_opt_into_network_clients()
    {
        Write("allow-network-clients = false\nbind = 0.0.0.0");
        Assert.Throws<CommandLineException>(() => Parse());
        Assert.True(Parse("--allow-network-clients").AllowNetworkClients);
    }

    [Fact]
    public void Explicit_missing_file_is_an_error_and_is_not_created()
    {
        string missing = Path.Combine(_directory, "missing.conf");
        Assert.Contains("Cannot load", Assert.Throws<CommandLineException>(() => Parse("--config", missing)).Message, StringComparison.Ordinal);
        Assert.False(File.Exists(missing));
        Assert.False(File.Exists(Path.Combine(_directory, HostConfigurationFile.DefaultName)));
    }

    [Theory]
    [InlineData("--help", typeof(HelpRequestedException))]
    [InlineData("--version", typeof(VersionRequestedException))]
    public void Informational_options_do_not_read_or_create_files(string option, Type exceptionType)
    {
        Assert.Throws(exceptionType, () => Parse("--config", "missing.conf", option));
        Assert.False(File.Exists(Path.Combine(_directory, HostConfigurationFile.DefaultName)));
    }

    [Theory]
    [InlineData("porrt = 18080", "unknown setting")]
    [InlineData("port = 18080\nport = 18081", ":2: repeated setting")]
    [InlineData("port 18080", ":1: expected key = value")]
    [InlineData("motd = \"oops", ":1: unclosed quoted value")]
    [InlineData("allow-network-clients = yes", "expects true or false")]
    [InlineData("port = abc", "expects an integer")]
    [InlineData("port = 65536", "1-65535")]
    [InlineData("bind = localhost", "must be an IP address")]
    [InlineData("bind = 0.0.0.0", "requires --allow-network-clients")]
    [InlineData("public-origin = http://192.0.2.4:18080", "requires --allow-network-clients")]
    [InlineData("public-origin = http://127.0.0.1:9999", "must be http://host")]
    [InlineData("raid-capacity = -1", "Invalid --raid-capacity")]
    [InlineData("game-build = abc", "Invalid --server-name/--motd/--game-build")]
    [InlineData("console = invalid", "must be auto")]
    [InlineData("session-control-token-file = operator-token", "requires both")]
    [InlineData("raid-players = 0", "--raid-players must be at least 1")]
    [InlineData("raid-fill-seconds = 601", "--raid-fill-seconds must be from 0 to 600")]
    [InlineData("raid-control-port = 18079\nraid-capacity = 2", "must not include the Host --port")]
    [InlineData("raid-port = 65535\nraid-capacity = 2", "one port from 1 to 65535")]
    [InlineData("archive-keys-file =", "must not be empty")]
    [InlineData("config = another.conf", "unknown setting")]
    public void Bad_files_fail_explicitly(string content, string expected)
    {
        Write(content);
        Assert.Contains(expected, Assert.Throws<CommandLineException>(() => Parse()).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Backslashes_and_environment_placeholders_are_literal()
    {
        Write("""
            motd = "C:\servers\new ${ARES_HOST} %USERNAME% #1"
            """);
        Assert.Equal(@"C:\servers\new ${ARES_HOST} %USERNAME% #1", Parse().Settings.Motd);
    }

    [Fact]
    public void Relative_config_selection_and_file_paths_use_the_selected_directory()
    {
        string folder = Path.Combine(_directory, "other");
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(_directory, "archive-keys.txt"), Path.Combine(folder, "archive-keys.txt"));
        File.WriteAllText(Path.Combine(folder, "custom.conf"), "log-file = host.log" + MongoConfiguration);
        HostConfiguration configuration = Parse("--config", "other/custom.conf");
        Assert.Equal(Path.Combine(folder, "host.log"), configuration.LogFile);
        Assert.False(File.Exists(Path.Combine(_directory, HostConfigurationFile.DefaultName)));
    }

    [Fact]
    public void Validation_uses_effective_values_after_overrides_but_still_rejects_unknown_keys()
    {
        Write("port = broken\nallow-network-clients = broken");
        Assert.Equal(18081, Parse("--port", "18081", "--allow-network-clients").Port);
        Write("port = broken\nporrt = 18080");
        Assert.Contains("unknown setting", Assert.Throws<CommandLineException>(() => Parse("--port", "18081")).Message, StringComparison.Ordinal);
    }

    private HostConfiguration Parse(params string[] args) => HostConfiguration.Parse(args, ArchiveKeysTests.DummySha256, _directory);

    private void Write(string text) => File.WriteAllText(Path.Combine(_directory, HostConfigurationFile.DefaultName), text + MongoConfiguration);
}
