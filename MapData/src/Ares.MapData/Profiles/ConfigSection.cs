namespace Ares.MapData.Profiles;

/// <summary>How a config line changes the array it names.</summary>
internal enum ConfigOperation
{
    /// <summary><c>Key=Value</c>, <c>+Key=Value</c> or <c>.Key=Value</c>: adds or replaces an element.</summary>
    Add,

    /// <summary><c>-Key=Value</c>: removes an element.</summary>
    Remove,

    /// <summary><c>!Key=…</c>: empties the array.</summary>
    Clear,
}

/// <summary>One line of a config section.</summary>
internal sealed record ConfigLine(ConfigOperation Operation, string Key, string Value);

/// <summary>Reads config (ini) files: the lines of one section and the struct literals of their values.</summary>
internal static class ConfigSection
{
    /// <summary>The <c>Key=Value</c> lines of every <c>[<paramref name="section"/>]</c> in <paramref name="text"/>, in order.</summary>
    public static IReadOnlyList<ConfigLine> Read(string text, string section)
    {
        List<ConfigLine> lines = [];
        string header = "[" + section + "]";
        bool inSection = false;
        foreach (string rawLine in text.TrimStart('﻿').Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.StartsWith('['))
            {
                inSection = line == header;
                continue;
            }

            int equals = line.IndexOf('=');
            if (!inSection || equals <= 0 || line.StartsWith(';'))
            {
                continue;
            }

            ConfigOperation operation = line[0] switch
            {
                '-' => ConfigOperation.Remove,
                '!' => ConfigOperation.Clear,
                _ => ConfigOperation.Add,
            };
            int keyStart = line[0] is '+' or '-' or '!' or '.' ? 1 : 0;
            lines.Add(new ConfigLine(operation, line[keyStart..equals].Trim(), line[(equals + 1)..].Trim()));
        }

        return lines;
    }

    /// <summary>
    /// The top-level members of a struct literal <c>(Key=Value,Key="Value",…)</c>, with surrounding quotes removed.
    /// Nested literals stay as text. Later members with the same key replace earlier ones.
    /// </summary>
    /// <exception cref="InvalidDataException">The text is not a parenthesized literal.</exception>
    public static IReadOnlyDictionary<string, string> Members(string literal)
    {
        Dictionary<string, string> members = new(StringComparer.OrdinalIgnoreCase);
        foreach (string part in Elements(literal))
        {
            int equals = part.IndexOf('=');
            if (equals > 0)
            {
                members[part[..equals].Trim()] = Unquote(part[(equals + 1)..].Trim());
            }
        }

        return members;
    }

    /// <summary>The top-level comma-separated elements of a parenthesized list; an empty text is an empty list.</summary>
    /// <exception cref="InvalidDataException">The text is neither empty nor a parenthesized list.</exception>
    public static IReadOnlyList<string> Elements(string literal)
    {
        string text = literal.Trim();
        if (text.Length == 0)
        {
            return [];
        }

        if (text.Length < 2 || text[0] != '(' || text[^1] != ')')
        {
            throw new InvalidDataException($"Config value is not a parenthesized list: {Shorten(text)}");
        }

        List<string> parts = [];
        int depth = 0;
        bool quoted = false;
        int start = 1;
        for (int i = 1; i < text.Length - 1; i++)
        {
            switch (text[i])
            {
                case '"':
                    quoted = !quoted;
                    break;
                case '(' when !quoted:
                    depth++;
                    break;
                case ')' when !quoted:
                    depth--;
                    break;
                case ',' when !quoted && depth == 0:
                    parts.Add(text[start..i].Trim());
                    start = i + 1;
                    break;
            }
        }

        if (depth != 0 || quoted)
        {
            throw new InvalidDataException($"Config value has unbalanced parentheses or quotes: {Shorten(text)}");
        }

        string last = text[start..^1].Trim();
        if (last.Length > 0 || parts.Count > 0)
        {
            parts.Add(last);
        }

        return parts;
    }

    private static string Unquote(string value)
    {
        return value is ['"', .., '"'] ? value[1..^1] : value;
    }

    private static string Shorten(string text)
    {
        const int MaxShown = 80;
        return text.Length <= MaxShown ? text : text[..MaxShown] + "…";
    }
}
