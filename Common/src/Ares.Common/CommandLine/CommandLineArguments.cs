using System.Globalization;

namespace Ares.Common.CommandLine;

/// <summary>
/// Minimal GNU-style argument reader: <c>--name value</c>, <c>--name=value</c> and <c>--flag</c>.
/// Unknown or repeated options are errors, so typos never silently change behaviour.
/// </summary>
public sealed class CommandLineArguments
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);
    private readonly HashSet<string> _consumed = new(StringComparer.Ordinal);

    private CommandLineArguments()
    {
    }

    /// <summary>Parses <paramref name="args"/>. Options listed in <paramref name="flags"/> take no value.</summary>
    /// <exception cref="CommandLineException">Malformed or repeated option.</exception>
    public static CommandLineArguments Parse(IReadOnlyList<string> args, IReadOnlySet<string> flags)
    {
        CommandLineArguments result = new();
        for (int i = 0; i < args.Count; i++)
        {
            string argument = args[i];
            if (!argument.StartsWith("--", StringComparison.Ordinal) || argument.Length == 2)
            {
                throw new CommandLineException($"Unexpected argument '{argument}'");
            }

            string name = argument[2..];
            string? value = null;
            int separator = name.IndexOf('=', StringComparison.Ordinal);
            if (separator >= 0)
            {
                value = name[(separator + 1)..];
                name = name[..separator];
                if (flags.Contains(name))
                {
                    throw new CommandLineException($"--{name} does not take a value");
                }
            }
            else if (!flags.Contains(name))
            {
                if (i + 1 >= args.Count)
                {
                    throw new CommandLineException($"--{name} requires a value");
                }

                value = args[++i];
            }

            if (!result._values.TryAdd(name, value))
            {
                throw new CommandLineException($"--{name} was given more than once");
            }
        }

        return result;
    }

    /// <summary>True when the help option was given.</summary>
    public bool HelpRequested => _values.ContainsKey("help");

    /// <summary>True when the version option was given.</summary>
    public bool VersionRequested => _values.ContainsKey("version");

    /// <summary>True when <c>--name</c> was given (flag or valued option).</summary>
    public bool Has(string name)
    {
        _consumed.Add(name);
        return _values.ContainsKey(name);
    }

    /// <summary>The value of <c>--name</c>, or <paramref name="fallback"/> when absent.</summary>
    public string? GetString(string name, string? fallback = null)
    {
        _consumed.Add(name);
        return _values.GetValueOrDefault(name, fallback);
    }

    /// <summary>The integer value of <c>--name</c>, or <paramref name="fallback"/> when absent.</summary>
    public int GetInt32(string name, int fallback)
    {
        string? text = GetString(name);
        if (text is null)
        {
            return fallback;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new CommandLineException($"--{name} expects an integer");
    }

    /// <summary>Throws when an option was given that the program never asked for.</summary>
    public void RejectUnknown()
    {
        _consumed.Add("help");
        _consumed.Add("version");
        string? unknown = _values.Keys.FirstOrDefault(name => !_consumed.Contains(name));
        if (unknown is not null)
        {
            throw new CommandLineException($"Unknown option --{unknown}");
        }
    }
}

/// <summary>Invalid command-line usage.</summary>
public sealed class CommandLineException(string message) : Exception(message);

/// <summary>--help was given.</summary>
public sealed class HelpRequestedException() : Exception("Help requested");

/// <summary>--version was given.</summary>
public sealed class VersionRequestedException() : Exception("Version requested");
