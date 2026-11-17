using Ares.GameDataExtract.Animations;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Game.Movement;

namespace Ares.GameDataExtract.Movement;

/// <summary>
/// Reads the gameplay tags the character graph tracks from the local simulation logic's root export
/// (<see cref="MovementAssets.SimulationLogicPackage"/>). The cooked object keeps them as two arrays, found by shape (an
/// int32 count followed by that many names that all are dotted tag names): the output tags, each followed by its tracking
/// settings (an int32 count, then one unversioned struct of three bytes per tag), and right after them the input tags.
/// The output tags are the tracked ones, in bit order; <see cref="MovementAssets.SprintActiveTag"/> and
/// <see cref="MovementAssets.FlashlightActiveTag"/> must sit at their known indices.
/// </summary>
internal static class GraphTagReader
{
    private const int MinTags = 8;
    private const int MaxTags = 1024;
    private const int NameSize = 8;
    private const int SettingsMembers = 3;
    private const int SkipMask = 0x7F;
    private const int ZeroesFlag = 0x80;
    private const int LastFlag = 0x100;
    private const int CountShift = 9;

    /// <summary>The tracked tags in bit order.</summary>
    /// <exception cref="InvalidDataException">The arrays are not found as expected, or a known tag is elsewhere.</exception>
    public static IReadOnlyList<string> Read(PackageCache packages)
    {
        CookedPackage package = MovementPackages.Load(packages, MovementAssets.SimulationLogicPackage);
        ReadOnlySpan<byte> raw = MovementPackages.Bytes(package, MovementPackages.Named(package, MovementPackages.ShortName(MovementAssets.SimulationLogicPackage)));
        List<(int At, int End, string[] Names)> arrays = TagArrays(package, raw);
        if (arrays.Count != 2)
        {
            throw new InvalidDataException($"{package.Name}: {arrays.Count} tag arrays, expected two");
        }

        string[] outputs = arrays[0].Names;
        if (SettingsEnd(raw, arrays[0].End, outputs.Length) != arrays[1].At)
        {
            throw new InvalidDataException($"{package.Name}: unexpected data between the output tag settings and the input tags");
        }

        if (outputs.Distinct(StringComparer.Ordinal).Count() != outputs.Length || outputs.Length > MovementAssets.GraphTagWords * MovementAssets.GraphTagWordBits)
        {
            throw new InvalidDataException($"{package.Name}: the output tags repeat or exceed the tag words");
        }

        foreach ((string tag, int index) in new[] { (MovementAssets.SprintActiveTag, MovementAssets.SprintActiveTagIndex), (MovementAssets.FlashlightActiveTag, MovementAssets.FlashlightActiveTagIndex) })
        {
            if (Array.IndexOf(outputs, tag) != index)
            {
                throw new InvalidDataException($"{package.Name}: {tag} is tag {Array.IndexOf(outputs, tag)}, expected {index}");
            }
        }

        return outputs;
    }

    /// <summary>Every (offset, end, names) where an int32 count is followed by that many dotted names of number 0.</summary>
    private static List<(int At, int End, string[] Names)> TagArrays(CookedPackage package, ReadOnlySpan<byte> raw)
    {
        IReadOnlyList<string> names = package.Header.Names;
        List<(int, int, string[])> found = [];
        int at = 0;
        while (at + 4 <= raw.Length)
        {
            int count = ExportBytes.Int32(raw, at);
            int end = at + 4 + (NameSize * count);
            if (count is >= MinTags and <= MaxTags && end <= raw.Length && TagNames(raw, at + 4, count, names) is { } tags)
            {
                found.Add((at, end, tags));
                at = end;
                continue;
            }

            at++;
        }

        return found;
    }

    private static string[]? TagNames(ReadOnlySpan<byte> raw, int at, int count, IReadOnlyList<string> names)
    {
        string[] tags = new string[count];
        for (int item = 0; item < count; item++)
        {
            uint index = ExportBytes.UInt32(raw, at + (NameSize * item));
            uint number = ExportBytes.UInt32(raw, at + (NameSize * item) + 4);
            if (index >= names.Count || number != 0 || !names[(int)index].Contains('.', StringComparison.Ordinal))
            {
                return null;
            }

            tags[item] = names[(int)index];
        }

        return tags;
    }

    /// <summary>End offset of <paramref name="count"/> tag settings structs (three byte members each) starting at <paramref name="at"/>.</summary>
    private static int SettingsEnd(ReadOnlySpan<byte> raw, int at, int count)
    {
        if (ExportBytes.Int32(raw, at) != count)
        {
            throw new InvalidDataException("The tag settings count differs from the tag count");
        }

        at += 4;
        for (int item = 0; item < count; item++)
        {
            int header = ExportBytes.UInt16(raw, at);
            at += 2;
            if ((header & SkipMask) != 0 || (header & LastFlag) == 0 || header >> CountShift != SettingsMembers)
            {
                throw new InvalidDataException($"Unsupported tag settings header {header:x}");
            }

            int mask = 0;
            if ((header & ZeroesFlag) != 0)
            {
                mask = ExportBytes.Byte(raw, at);
                at++;
            }

            for (int member = 0; member < SettingsMembers; member++)
            {
                if (((mask >> member) & 1) == 0)
                {
                    at++;
                }
            }
        }

        return at;
    }
}
