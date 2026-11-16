using System.Globalization;
using Ares.Common.CommandLine;

namespace Ares.Host;

/// <summary>Host's commented key/value file, with command-line overrides.</summary>
internal sealed class HostConfigurationFile
{
    internal const string DefaultName = "ares-host.conf";
    private readonly CommandLineArguments _arguments;
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
    private readonly HashSet<string> _consumed = new(StringComparer.Ordinal);
    private readonly string _directory;

    private HostConfigurationFile(CommandLineArguments arguments, string path)
    {
        _arguments = arguments;
        Path = path;
        _directory = System.IO.Path.GetDirectoryName(path)!;
    }

    internal string Path { get; }

    internal static HostConfigurationFile Load(CommandLineArguments arguments, string workingDirectory)
    {
        string? requested = arguments.GetString(HostOptions.Config.Key);
        try
        {
            string path = System.IO.Path.GetFullPath(requested ?? DefaultName, workingDirectory);
            HostConfigurationFile result = new(arguments, path);
            if (requested is null && !File.Exists(path))
            {
                using Stream template = typeof(HostConfigurationFile).Assembly.GetManifestResourceStream("Ares.Host.ares-host.conf.dist")!;
                using FileStream output = new(path, FileMode.CreateNew, FileAccess.Write);
                template.CopyTo(output);
            }

            int lineNumber = 0;
            foreach (string raw in File.ReadLines(path))
            {
                lineNumber++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                int separator = line.IndexOf('=', StringComparison.Ordinal);
                if (separator <= 0)
                {
                    throw new CommandLineException($"{path}:{lineNumber}: expected key = value");
                }

                string key = line[..separator].Trim();
                string value = line[(separator + 1)..].Trim();
                if (value.StartsWith('"'))
                {
                    if (value.Length < 2 || !value.EndsWith('"'))
                    {
                        throw new CommandLineException($"{path}:{lineNumber}: unclosed quoted value for '{key}'");
                    }

                    value = value[1..^1];
                }

                if (!result._values.TryAdd(key, value))
                {
                    throw new CommandLineException($"{path}:{lineNumber}: repeated setting '{key}'");
                }
            }

            return result;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new CommandLineException($"Cannot load Host configuration '{requested ?? DefaultName}': {error.Message}");
        }
    }

    /// <summary>The option's value, or its default.</summary>
    internal string? GetString(HostOption option)
    {
        _consumed.Add(option.Key);
        return _arguments.GetString(option.Key, _values.TryGetValue(option.Key, out string? value) ? value : option.Default);
    }

    internal int GetInt32(HostOption option)
    {
        string? value = GetString(option);
        return value is not null && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number
            : throw new CommandLineException($"{Path}: '{option.Key}' expects an integer");
    }

    internal bool GetBoolean(HostOption option)
    {
        _consumed.Add(option.Key);
        if (_arguments.Has(option.Key))
        {
            return true;
        }

        string value = _values.GetValueOrDefault(option.Key, "false");
        return bool.TryParse(value, out bool flag) ? flag
            : throw new CommandLineException($"{Path}: '{option.Key}' expects true or false");
    }

    internal string? GetPath(HostOption option)
    {
        string? value = GetString(option);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        // Resolve command-line paths from the working directory.
        if (_arguments.Has(option.Key))
        {
            return value;
        }

        try
        {
            return System.IO.Path.GetFullPath(value, _directory);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException)
        {
            throw new CommandLineException($"{Path}: invalid path for '{option.Key}': {error.Message}");
        }
    }

    /// <summary>Settings read so far that neither the file nor the command line sets; they take their defaults.</summary>
    internal IReadOnlyList<string> Missing() => [.. _consumed.Where(key => !_values.ContainsKey(key) && !_arguments.Has(key)).Order(StringComparer.Ordinal)];

    internal void RejectUnknown()
    {
        _arguments.RejectUnknown();
        string? unknown = _values.Keys.FirstOrDefault(key => !_consumed.Contains(key));
        if (unknown is not null)
        {
            throw new CommandLineException($"{Path}: unknown setting '{unknown}'");
        }
    }
}
