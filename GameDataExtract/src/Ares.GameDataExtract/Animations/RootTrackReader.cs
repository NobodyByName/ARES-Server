using System.Text;
using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Animations;

/// <summary>A root position of an animation frame in the character's frame: to its right and ahead of it.</summary>
/// <param name="Right">Distance to the right (the negated X of the mesh, which faces +Y).</param>
/// <param name="Forward">Distance ahead (the mesh's Y).</param>
internal readonly record struct RootPoint(double Right, double Forward);

/// <summary>
/// Reads the root bone track of an animation sequence export compressed with the per-track bone codec
/// (<see cref="AnimationFormats.PerTrackCodecPrefix"/>). The compressed bone data sits right before the codec's name:
/// the track-to-bone table (a count, then bone indices starting at the root), a table of (index, flags) pairs, the
/// stream size (the bytes up to four before the codec name), one <see cref="AnimationFormats.TrackOffsetEntrySize"/>-byte
/// offset entry per track, then the track streams. Each track stream starts with a word of key format, format flags and
/// key count (see <see cref="AnimationFormats"/>). The tables are found by shape; anything unexpected fails.
/// </summary>
internal static class RootTrackReader
{
    private const int MaxTracks = 1024;
    private const int MaxPairs = 64;
    private const int MaxKeys = 1000;
    private const int MinForwardKeys = 2;
    private const int PairSize = 8;
    private const int Axes = 3;
    private const string ForwardCodec = AnimationFormats.PerTrackCodecPrefix + "0";
    private const double KeyTimeTolerance = 1e-5;
    private const double LengthTolerance = 1e-4;
    private const int LengthSearchBytes = 128;
    private const double MinLength = 0.1;
    private const double MaxLength = 60;

    private static readonly byte[] s_codecPrefix = Encoding.ASCII.GetBytes(AnimationFormats.PerTrackCodecPrefix);

    /// <summary>
    /// The root track's forward (Y) translation keys, one per sampled frame, of an animation whose root track has an
    /// identity rotation and a Y-only <see cref="AnimationFormats.Float96NoW"/> translation, compressed with codec
    /// instance 0; <paramref name="codecEnd"/> is the offset after the codec name.
    /// </summary>
    /// <exception cref="InvalidDataException">The codec, the tables or a supported root track are not found.</exception>
    public static float[] ReadForwardKeys(ReadOnlySpan<byte> export, out int codecEnd)
    {
        byte[] name = Encoding.ASCII.GetBytes(ForwardCodec + "\0");
        byte[] codec = [.. ExportBytes.Bytes(name.Length), .. name];
        int end = ExportBytes.Find(export, codec);
        if (end < 0 || ExportBytes.Find(export, codec, end + 1) >= 0)
        {
            throw new InvalidDataException("The per-track bone codec is not found once");
        }

        for (int sizeAt = 0; sizeAt < end - 4; sizeAt++)
        {
            // The stream size excludes the last four bytes before the codec name.
            if (ExportBytes.Int32(export, sizeAt) != end - sizeAt - 8)
            {
                continue;
            }

            for (int tracks = 1; tracks < MaxTracks; tracks++)
            {
                if (!HasIncreasingTrackMap(export, sizeAt, tracks))
                {
                    continue;
                }

                int blob = sizeAt + 4;
                _ = ExportBytes.Int32(export, blob + 4 + (sizeof(int) * ((2 * tracks) - 1)));
                int stream = blob + 4 + (AnimationFormats.TrackOffsetEntrySize * tracks);
                if (stream >= end)
                {
                    continue;
                }

                int translation = ExportBytes.Int32(export, blob + 4);
                int rotation = ExportBytes.Int32(export, blob + 8);
                if (rotation != AnimationFormats.NoTrack || translation < 0)
                {
                    throw new InvalidDataException("The root track is not an identity rotation with a translation");
                }

                (int format, int flags, int keys) = TrackWord(export, stream + translation);
                if (format != AnimationFormats.Float96NoW || flags != AnimationFormats.YComponentOnly || keys is < MinForwardKeys or > MaxKeys)
                {
                    throw new InvalidDataException($"A root translation of format {format}/{flags} with {keys} keys is not supported");
                }

                float[] values = new float[keys];
                for (int key = 0; key < keys; key++)
                {
                    values[key] = ExportBytes.Single(export, stream + translation + 4 + (sizeof(float) * key));
                }

                codecEnd = end + codec.Length;
                return values;
            }
        }

        throw new InvalidDataException("The bone stream tables are not found");
    }

    /// <summary>
    /// Checks that the sequence length spans <paramref name="keys"/> keys at <paramref name="sampleRate"/> (the length is
    /// stored before <paramref name="codecEnd"/>) and that a key time table follows the codec: the times of every key but
    /// the last, from 0 at that rate.
    /// </summary>
    /// <exception cref="InvalidDataException">The length or the time table is missing.</exception>
    public static void CheckKeyTimes(ReadOnlySpan<byte> export, int codecEnd, int keys, int sampleRate)
    {
        double length = (keys - 1) / (double)sampleRate;
        if (ExportBytes.Find(export, ExportBytes.Bytes((float)length), 0, codecEnd) < 0)
        {
            throw new InvalidDataException($"No {length} s sequence length for {keys} keys");
        }

        byte[] second = ExportBytes.Bytes((float)(1.0 / sampleRate));
        int times = keys - 1;
        int at = ExportBytes.Find(export, second, codecEnd);
        while (at >= 4)
        {
            if (at - 4 + (sizeof(float) * times) <= export.Length && times > 0 && IsTimeTable(export, at - 4, times, sampleRate))
            {
                return;
            }

            at = ExportBytes.Find(export, second, at + 1);
        }

        throw new InvalidDataException($"No {times}-key time table at {sampleRate} Hz");
    }

    /// <summary>
    /// The root's horizontal position at each frame at <paramref name="sampleRate"/>, from a root track with an identity
    /// rotation and a <see cref="AnimationFormats.Float96NoW"/> translation of any component mask, optionally keyed on a
    /// frame table (frames between keys interpolate linearly); empty when the root has no translation. The vertical
    /// component is dropped.
    /// </summary>
    /// <exception cref="InvalidDataException">The codec, the tables, a supported root track or the sequence length are not found.</exception>
    public static IReadOnlyList<RootPoint> ReadHorizontalPath(ReadOnlySpan<byte> export, int sampleRate)
    {
        (int stream, int translation, int rotation, int codecEnd) = BoneStream(export);
        if (rotation != AnimationFormats.NoTrack)
        {
            throw new InvalidDataException("The root track has a rotation");
        }

        if (translation == AnimationFormats.NoTrack)
        {
            return [];
        }

        int at = stream + translation;
        (int format, int flags, int count) = TrackWord(export, at);
        if (format != AnimationFormats.Float96NoW || count is < 1 or > MaxKeys)
        {
            throw new InvalidDataException($"A root translation of format {format} with {count} keys is not supported");
        }

        int[] components = [.. Enumerable.Range(0, Axes).Where(axis => ((flags >> axis) & 1) != 0)];
        if (components.Length == 0)
        {
            components = [0, 1, 2];
        }

        at += 4;
        double[][] keys = new double[count][];
        for (int key = 0; key < count; key++)
        {
            keys[key] = new double[Axes];
            foreach (int axis in components)
            {
                keys[key][axis] = ExportBytes.Single(export, at);
                at += sizeof(float);
            }
        }

        int[] frames = (flags & AnimationFormats.FrameTableFlag) != 0 ? FrameTable(export, at, count) : [.. Enumerable.Range(0, count)];
        if (count == 1)
        {
            return [Point(keys[0])];
        }

        double length = frames[^1] / (double)sampleRate;
        if (ExportBytes.Find(export, ExportBytes.Bytes((float)length), 0, codecEnd) < 0)
        {
            throw new InvalidDataException($"No {length} s sequence length for {frames[^1] + 1} frames");
        }

        RootPoint[] points = new RootPoint[frames[^1] + 1];
        for (int frame = 0; frame < points.Length; frame++)
        {
            int index = Array.FindLastIndex(frames, keyFrame => keyFrame <= frame);
            if (frames[index] == frame)
            {
                points[frame] = Point(keys[index]);
                continue;
            }

            double alpha = (frame - frames[index]) / (double)(frames[index + 1] - frames[index]);
            double[] from = keys[index];
            double[] to = keys[index + 1];
            points[frame] = Point([.. from.Select((value, axis) => value + ((to[axis] - value) * alpha))]);
        }

        return points;
    }

    /// <summary>
    /// The sequence's play length: a whole number of frames at <paramref name="sampleRate"/> stored as a float in the
    /// export's first bytes. With a root path (<paramref name="lastSampleTime"/>), the one such value from the last sample
    /// up to one sample step later; without one, the one such value stored more than once other than 1.
    /// </summary>
    /// <exception cref="InvalidDataException">No single such value is found.</exception>
    public static float PlayLength(ReadOnlySpan<byte> export, int sampleRate, double? lastSampleTime, double sampleStep)
    {
        List<float> found = [];
        for (int at = 0; at < LengthSearchBytes; at++)
        {
            float length = ExportBytes.Single(export, at);
            double frames = length * (double)sampleRate;
            if (length >= MinLength && length <= MaxLength && Math.Abs(frames - Math.Round(frames)) < LengthTolerance)
            {
                found.Add(length);
            }
        }

        HashSet<float> lengths = lastSampleTime is { } last
            ? [.. found.Where(length => last - LengthTolerance <= length && length <= last + sampleStep + LengthTolerance)]
            : [.. found.Where(length => found.Count(other => other.Equals(length)) >= 2 && !length.Equals(1f))];
        return lengths.Count == 1
            ? lengths.Single()
            : throw new InvalidDataException($"The play length is not found once ({string.Join(", ", lengths.Order())})");
    }

    /// <summary>
    /// (stream start, root translation offset, root rotation offset, offset after the codec name) of an export holding one
    /// per-track codec of any instance number.
    /// </summary>
    private static (int Stream, int Translation, int Rotation, int CodecEnd) BoneStream(ReadOnlySpan<byte> export)
    {
        (int start, int length) = SingleCodecName(export);
        int end = start - 4;
        if (ExportBytes.Int32(export, end) != length)
        {
            throw new InvalidDataException("The codec name length does not match");
        }

        for (int sizeAt = 0; sizeAt < end - 4; sizeAt++)
        {
            // The stream size excludes the last four bytes before the codec name.
            if (ExportBytes.Int32(export, sizeAt) != end - sizeAt - 8)
            {
                continue;
            }

            for (int pairs = 0; pairs < MaxPairs; pairs++)
            {
                int countAt = sizeAt - 4 - (PairSize * pairs);
                if (countAt < 0 || ExportBytes.Int32(export, countAt) != pairs)
                {
                    continue;
                }

                for (int tracks = 1; tracks < MaxTracks; tracks++)
                {
                    int mapAt = countAt - (sizeof(int) * tracks);
                    if (mapAt < 4 || ExportBytes.Int32(export, mapAt - 4) != tracks)
                    {
                        continue;
                    }

                    int[] bones = Ints(export, mapAt, tracks);
                    if (bones[0] != 0 || bones.Min() < 0 || bones.Distinct().Count() != tracks)
                    {
                        continue;
                    }

                    int blob = sizeAt + 4;
                    return (blob + 4 + (AnimationFormats.TrackOffsetEntrySize * tracks), ExportBytes.Int32(export, blob + 4), ExportBytes.Int32(export, blob + 8),
                        start + length);
                }
            }
        }

        throw new InvalidDataException("The bone stream tables are not found");
    }

    /// <summary>Start and length (terminating zero included) of the one codec name: the prefix, decimal digits and a zero.</summary>
    private static (int Start, int Length) SingleCodecName(ReadOnlySpan<byte> export)
    {
        (int Start, int Length)? match = null;
        int count = 0;
        int from = 0;
        while (ExportBytes.Find(export, s_codecPrefix, from) is var at and >= 0)
        {
            int digits = at + s_codecPrefix.Length;
            int next = digits;
            while (next < export.Length && char.IsAsciiDigit((char)export[next]))
            {
                next++;
            }

            if (next > digits && next < export.Length && export[next] == 0)
            {
                match ??= (at, next + 1 - at);
                count++;
                from = next + 1;
            }
            else
            {
                from = at + 1;
            }
        }

        return count == 1 && match is { } found ? found : throw new InvalidDataException("The per-track bone codec is not found once");
    }

    /// <summary>
    /// Whether a track-to-bone table of <paramref name="tracks"/> strictly increasing bone indices from the root, and a
    /// pair table before the stream size at <paramref name="sizeAt"/>, fit.
    /// </summary>
    private static bool HasIncreasingTrackMap(ReadOnlySpan<byte> export, int sizeAt, int tracks)
    {
        for (int pairs = 0; pairs < MaxPairs; pairs++)
        {
            int countAt = sizeAt - 4 - (PairSize * pairs);
            if (countAt < 0 || ExportBytes.Int32(export, countAt) != pairs)
            {
                continue;
            }

            int mapAt = countAt - (sizeof(int) * tracks);
            if (mapAt < 4 || ExportBytes.Int32(export, mapAt - 4) != tracks)
            {
                continue;
            }

            int[] bones = Ints(export, mapAt, tracks);
            if (bones[0] == 0 && bones.Zip(bones.Skip(1)).All(pair => pair.First < pair.Second))
            {
                return true;
            }
        }

        return false;
    }

    private static (int Format, int Flags, int Keys) TrackWord(ReadOnlySpan<byte> export, int at)
    {
        uint word = ExportBytes.UInt32(export, at);
        return ((int)(word >> AnimationFormats.TrackFormatShift), (int)(word >> AnimationFormats.TrackFlagsShift) & AnimationFormats.TrackFlagsMask,
            (int)(word & AnimationFormats.TrackKeyCountMask));
    }

    private static int[] FrameTable(ReadOnlySpan<byte> export, int at, int count)
    {
        if (at < 0 || at > export.Length - count)
        {
            throw new InvalidDataException("The root frame table is outside the export");
        }

        int[] frames = [.. export.Slice(at, count).ToArray().Select(frame => (int)frame)];
        return frames[0] == 0 && frames.Zip(frames.Skip(1)).All(pair => pair.First < pair.Second)
            ? frames
            : throw new InvalidDataException("The root frame table is not increasing from 0");
    }

    private static bool IsTimeTable(ReadOnlySpan<byte> export, int at, int times, int sampleRate)
    {
        for (int key = 0; key < times; key++)
        {
            double difference = Math.Abs(ExportBytes.Single(export, at + (sizeof(float) * key)) - (key / (double)sampleRate));
            if (double.IsNaN(difference) || difference >= KeyTimeTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private static int[] Ints(ReadOnlySpan<byte> export, int at, int count)
    {
        int[] values = new int[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = ExportBytes.Int32(export, at + (sizeof(int) * i));
        }

        return values;
    }

    /// <summary>The horizontal position of a key: right is the negated X, forward the Y (adding 0 turns −0 into 0).</summary>
    private static RootPoint Point(double[] key) => new(0.0 - key[0], key[1] + 0.0);
}
