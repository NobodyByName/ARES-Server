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
        string? requested = arguments.GetString("config");
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

    internal string? GetString(string name, string? fallback = null)
    {
        _consumed.Add(name);
        return _arguments.GetString(name, _values.TryGetValue(name, out string? value) ? value : fallback);
    }

    internal int GetInt32(string name, int fallback)
    {
        string? value = GetString(name);
        return value is null ? fallback
            : int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number
            : throw new CommandLineException($"{Path}: '{name}' expects an integer");
    }

    internal bool GetBoolean(string name)
    {
        _consumed.Add(name);
        if (_arguments.Has(name))
        {
            return true;
        }

        string value = _values.GetValueOrDefault(name, "false");
        return bool.TryParse(value, out bool flag) ? flag
            : throw new CommandLineException($"{Path}: '{name}' expects true or false");
    }

    internal string? GetPath(string name, string? fallback = null)
    {
        string? value = GetString(name, fallback);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        // Preserve the existing working-directory convention for command-line overrides.
        if (_arguments.Has(name))
        {
            return value;
        }

        try
        {
            return System.IO.Path.GetFullPath(value, _directory);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException)
        {
            throw new CommandLineException($"{Path}: invalid path for '{name}': {error.Message}");
        }
    }

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
