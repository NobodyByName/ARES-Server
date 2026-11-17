using System.Buffers.Binary;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Animations;

/// <summary>A span of an animation during which a branch tag raises a transient state.</summary>
/// <param name="State">The transient state id the tag raises.</param>
/// <param name="Start">Start, in animation seconds.</param>
/// <param name="End">End, in animation seconds (the notify's end link).</param>
internal readonly record struct BranchWindow(byte State, float Start, float End);

/// <summary>
/// Reads the branch-tag notify windows of an animation sequence: the notify events (<see cref="AnimationFormats.AnimNotifyEvent"/>
/// blocks stored with header <see cref="AnimationFormats.NotifyEventHeader"/>) whose notify-state class is one of the
/// package's branch tag exports (named with a given prefix; each stores the state it raises as a byte at a given chain
/// index, or nothing for a tag that raises none). Every branch tag must have exactly one notify event, and each window's
/// end link must lie at its start plus its duration.
/// </summary>
internal static class BranchWindowReader
{
    private const int Trailer = 4;
    private const double EndTolerance = 1e-4;

    /// <summary>The windows of the tags that raise a state, ordered by start, then state.</summary>
    /// <param name="package">The animation's package.</param>
    /// <param name="sequence">The animation sequence export.</param>
    /// <param name="tagPrefix">Name prefix of the branch tag exports.</param>
    /// <param name="stateIndex">Chain index of a branch tag's state byte.</param>
    /// <exception cref="InvalidDataException">A branch tag or a window does not have the expected layout, or a tag has no notify event.</exception>
    public static IReadOnlyList<BranchWindow> Read(CookedPackage package, ZenExport sequence, string tagPrefix, int stateIndex)
    {
        Dictionary<int, byte?> tags = BranchTags(package, tagPrefix, stateIndex);
        int start = CookedPackage.StartOf(sequence);
        ReadOnlySpan<byte> export = package.Data.AsSpan(start, (int)sequence.SerialSize);
        List<BranchWindow> windows = [];
        int found = 0;
        for (int at = 4; at < export.Length - 2; at++)
        {
            if (BinaryPrimitives.ReadUInt16LittleEndian(export[at..]) != AnimationFormats.NotifyEventHeader
                || TryReadEvent(package, start + at) is not { } notify
                || !tags.TryGetValue(notify.Get("NotifyStateClass", new ObjectReference(0)).Value, out byte? state))
            {
                continue;
            }

            found++;
            float begin = notify.Get("LinkValue", 0f);
            float duration = notify.Get("Duration", 0f);
            float end = EndLink(notify).Get("LinkValue", 0f);
            if (Math.Abs(end - ((double)begin + duration)) > EndTolerance)
            {
                throw new InvalidDataException($"{package.Name}: a branch window end differs from its end link");
            }

            if (state is { } raised)
            {
                windows.Add(new BranchWindow(raised, begin, end));
            }
        }

        return found == tags.Count
            ? [.. windows.OrderBy(window => window.Start).ThenBy(window => window.State)]
            : throw new InvalidDataException($"{package.Name}: {found} branch notify events for {tags.Count} branch tags");
    }

    /// <summary>The branch tag exports by object reference value (export index + 1), with the state each raises.</summary>
    private static Dictionary<int, byte?> BranchTags(CookedPackage package, string tagPrefix, int stateIndex)
    {
        Dictionary<int, byte?> tags = [];
        foreach (ZenExport export in package.Exports.Where(export => export.Name.StartsWith(tagPrefix, StringComparison.Ordinal)))
        {
            int start = CookedPackage.StartOf(export);
            int end = CookedPackage.EndOf(export);
            UnversionedHeader header = UnversionedHeader.Read(package.Data, start);
            if (header.Members.Any(member => !member.IsNonZero) || package.Data.AsSpan(end - Trailer, Trailer).ContainsAnyExcept((byte)0))
            {
                throw new InvalidDataException($"{package.Name}: branch tag {export.Name} does not have the expected layout");
            }

            tags[export.Index + 1] = header.Members.Select(member => member.Index).ToArray() switch
            {
                [] => null,
                [var index] when index == stateIndex => package.ReadByte(header.ValuesOffset),
                _ => throw new InvalidDataException($"{package.Name}: branch tag {export.Name} stores unexpected members"),
            };
        }

        return tags;
    }

    private static PropertyBag? TryReadEvent(CookedPackage package, int offset)
    {
        try
        {
            return PropertyReader.Read(package, offset, AnimationFormats.AnimNotifyEvent, out _);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>The notify's end link; a link the block marks as zero is not a window.</summary>
    private static PropertyBag EndLink(PropertyBag notify)
    {
        PropertyBag link = notify.Get("EndLink", new PropertyBag(AnimationFormats.AnimLinkableElement, []));
        return link.IsZero ? throw new InvalidDataException("A branch window has no end link") : link;
    }
}
