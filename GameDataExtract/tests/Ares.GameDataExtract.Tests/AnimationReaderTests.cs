using System.Text;
using Ares.GameDataExtract.Animations;
using Ares.GameDataExtract.Packages;

namespace Ares.GameDataExtract.Tests;

public sealed class AnimationReaderTests
{
    private const int SampleRate = 30;
    private const string BranchTagPrefix = "BranchTag_";

    [Fact]
    public void Forward_keys_come_from_a_y_only_root_translation()
    {
        byte[] export = PerTrackExport(Track(1, 2, [0f, 10f, 20.5f]), length: 2 / 30f, after: Times(2));

        float[] keys = RootTrackReader.ReadForwardKeys(export, out int codecEnd);

        Assert.Equal([0f, 10f, 20.5f], keys);
        RootTrackReader.CheckKeyTimes(export, codecEnd, keys.Length, SampleRate);
    }

    [Fact]
    public void Forward_keys_need_the_length_and_the_key_times()
    {
        byte[] withoutTimes = PerTrackExport(Track(1, 2, [0f, 10f, 20f]), length: 2 / 30f);
        byte[] withoutLength = PerTrackExport(Track(1, 2, [0f, 10f, 20f]), length: 1f, after: Times(2));

        RootTrackReader.ReadForwardKeys(withoutTimes, out int codecEnd);
        Assert.Throws<InvalidDataException>(() => RootTrackReader.CheckKeyTimes(withoutTimes, codecEnd, 3, SampleRate));
        RootTrackReader.ReadForwardKeys(withoutLength, out codecEnd);
        Assert.Throws<InvalidDataException>(() => RootTrackReader.CheckKeyTimes(withoutLength, codecEnd, 3, SampleRate));
    }

    [Fact]
    public void Forward_keys_reject_other_root_tracks_and_codecs()
    {
        Assert.Throws<InvalidDataException>(() => RootTrackReader.ReadForwardKeys(PerTrackExport(Track(1, 0, [0f, 1f, 2f, 3f, 4f, 5f]), 1f), out _));
        Assert.Throws<InvalidDataException>(() => RootTrackReader.ReadForwardKeys(PerTrackExport(Track(1, 2, [0f, 1f]), 1f, rotation: 0), out _));
        Assert.Throws<InvalidDataException>(() => RootTrackReader.ReadForwardKeys(PerTrackExport(Track(1, 2, [0f, 1f]), 1f, codec: "AnimCompress_PerTrackCompression_3"), out _));
        byte[] twice = PerTrackExport(Track(1, 2, [0f, 1f]), 1f);
        Assert.Throws<InvalidDataException>(() => RootTrackReader.ReadForwardKeys([.. twice, .. Codec("AnimCompress_PerTrackCompression_0")], out _));
    }

    [Fact]
    public void A_horizontal_path_interpolates_between_keyed_frames()
    {
        // X and Y components (flags 1 | 2) with a frame table (8): frame 0 at (2, 4), frame 2 at (6, 8).
        byte[] track = [.. Track(1, 1 | 2 | 8, [2f, 4f, 6f, 8f], keys: 2), 0, 2];

        IReadOnlyList<RootPoint> path = RootTrackReader.ReadHorizontalPath(PerTrackExport(track, 2 / 30f, codec: "AnimCompress_PerTrackCompression_7"), SampleRate);

        Assert.Equal([new RootPoint(-2, 4), new RootPoint(-4, 6), new RootPoint(-6, 8)], path);
    }

    [Fact]
    public void A_horizontal_path_drops_the_vertical_component_and_is_empty_without_a_translation()
    {
        // Y and Z components (2 | 4), one key per frame.
        byte[] track = Track(1, 2 | 4, [1f, 100f, -0f, 50f], keys: 2);

        Assert.Equal([new RootPoint(0, 1), new RootPoint(0, 0)], RootTrackReader.ReadHorizontalPath(PerTrackExport(track, 1 / 30f), SampleRate));
        Assert.True(double.IsPositive(RootTrackReader.ReadHorizontalPath(PerTrackExport(track, 1 / 30f), SampleRate)[1].Forward));
        Assert.Empty(RootTrackReader.ReadHorizontalPath(PerTrackExport(track, 1 / 30f, translation: -1), SampleRate));
        Assert.Throws<InvalidDataException>(() => RootTrackReader.ReadHorizontalPath(PerTrackExport(track, 1f), SampleRate));
    }

    [Fact]
    public void The_play_length_is_the_whole_frame_value_that_fits_the_root_path()
    {
        byte[] export = [.. Floats(1f, 0.5f, 2f, 2f, 0.05f), .. new byte[128]];

        Assert.Equal(0.5f, RootTrackReader.PlayLength(export, SampleRate, lastSampleTime: 0.45, sampleStep: 0.05));
        Assert.Equal(2f, RootTrackReader.PlayLength(export, SampleRate, lastSampleTime: null, sampleStep: 0.05));
        Assert.Throws<InvalidDataException>(() => RootTrackReader.PlayLength(export, SampleRate, lastSampleTime: 5, sampleStep: 0.05));
    }

    [Fact]
    public void Branch_windows_come_from_the_notify_events_of_state_raising_tags()
    {
        byte[] sequence = [0, 0, 0, 0, .. NotifyEvent(stateClass: 2, start: 0.5f, duration: 0.25f), .. NotifyEvent(stateClass: 3, start: 0.1f, duration: 0.2f), 0, 0, 0, 0];
        CookedPackage package = Package(sequence, shiftEnds: false);

        BranchWindow window = Assert.Single(BranchWindowReader.Read(package, package.Exports[0], BranchTagPrefix, 1));

        Assert.Equal(new BranchWindow(4, 0.5f, 0.75f), window);
    }

    [Fact]
    public void Branch_windows_need_one_event_per_tag_and_a_matching_end()
    {
        CookedPackage missing = Package([0, 0, 0, 0, .. NotifyEvent(stateClass: 2, start: 0.5f, duration: 0.25f), 0, 0, 0, 0], shiftEnds: false);
        CookedPackage shifted = Package([0, 0, 0, 0, .. NotifyEvent(stateClass: 2, start: 0.5f, duration: 0.25f), .. NotifyEvent(stateClass: 3, start: 0.1f, duration: 0.2f), 0, 0], shiftEnds: true);

        Assert.Throws<InvalidDataException>(() => BranchWindowReader.Read(missing, missing.Exports[0], BranchTagPrefix, 1));
        Assert.Throws<InvalidDataException>(() => BranchWindowReader.Read(shifted, shifted.Exports[0], BranchTagPrefix, 1));
    }

    [Fact]
    public void Curve_keys_are_weighted_cubic_keys()
    {
        byte[] key = [2, 0, 3, .. Floats(1f, 2f, 3f, 4f, 5f, 6f)];
        CookedPackage package = CookedPackage.Parse(SyntheticArchives.Package("/Game/Curve", [("Curve", PackageObjectIndex.Null, PackageObjectIndex.Null, 0, [0x00, 0x03, 0x00, 0x03, .. BitConverter.GetBytes(1), .. key, 0, 0, 0, 0])]));
        CookedPackage linear = CookedPackage.Parse(SyntheticArchives.Package("/Game/Curve", [("Curve", PackageObjectIndex.Null, PackageObjectIndex.Null, 0, [0x00, 0x03, 0x00, 0x03, .. BitConverter.GetBytes(1), 1, .. key[1..], 0, 0, 0, 0])]));

        Assert.Equal(new CurveKey(1f, 2f, 3f, 4f, 5f, 6f), Assert.Single(CurveKeyReader.Read(package, package.Exports[0])));
        Assert.Throws<InvalidDataException>(() => CurveKeyReader.Read(linear, linear.Exports[0]));
    }

    [Fact]
    public void The_latest_key_time_spans_every_rich_curve_of_the_asset()
    {
        static byte[] Key(float time) => [1, 0, 0, .. Floats(time, 0f, 0f, 0f, 0f, 0f)];
        static CookedPackage Curves(byte[] first, byte[] trailer) => CookedPackage.Parse(SyntheticArchives.Package("/Game/Curves", [("Curves", PackageObjectIndex.Null, PackageObjectIndex.Null, 0,
            [0x00, 0x05, 0x00, 0x03, .. BitConverter.GetBytes(2), .. first, 0x00, 0x03, .. BitConverter.GetBytes(1), .. Key(3.5f), .. trailer])]));
        CookedPackage package = Curves([.. Key(0.5f), .. Key(2f)], new byte[8]);
        CookedPackage unordered = Curves([.. Key(2f), .. Key(0.5f)], new byte[8]);
        CookedPackage unread = Curves([.. Key(0.5f), .. Key(2f)], [0, 0, 0, 1]);

        Assert.Equal(3.5f, CurveKeyReader.LatestKeyTime(package, package.Exports[0], 8));
        Assert.Throws<InvalidDataException>(() => CurveKeyReader.LatestKeyTime(unordered, unordered.Exports[0], 8));
        Assert.Throws<InvalidDataException>(() => CurveKeyReader.LatestKeyTime(unread, unread.Exports[0], 8));
    }

    /// <summary>
    /// An export holding one track's compressed bone data: the track-to-bone table, an empty pair table, the stream
    /// size, a word holding the sequence length, the track offsets, the track stream and the codec name, then <paramref name="after"/>.
    /// </summary>
    internal static byte[] PerTrackExport(byte[] track, float length, int translation = 0, int rotation = -1, string codec = "AnimCompress_PerTrackCompression_0", byte[]? after = null)
    {
        byte[] head = [.. BitConverter.GetBytes(1), .. BitConverter.GetBytes(0), .. BitConverter.GetBytes(0)];
        byte[] blob = [.. BitConverter.GetBytes(length), .. BitConverter.GetBytes(translation), .. BitConverter.GetBytes(rotation), .. BitConverter.GetBytes(-1), .. track];
        int sizeAt = head.Length;
        int end = sizeAt + 4 + blob.Length;
        return [.. head, .. BitConverter.GetBytes(end - sizeAt - 8), .. blob, .. Codec(codec), .. after ?? []];
    }

    private static byte[] Codec(string name)
    {
        byte[] text = [.. Encoding.ASCII.GetBytes(name), 0];
        return [.. BitConverter.GetBytes(text.Length), .. text];
    }

    /// <summary>A track stream: its word (format, flags, key count) and the key floats.</summary>
    internal static byte[] Track(int format, int flags, float[] values, int? keys = null)
    {
        uint word = ((uint)format << 28) | ((uint)flags << 24) | (uint)(keys ?? values.Length);
        return [.. BitConverter.GetBytes(word), .. Floats(values)];
    }

    /// <summary>A key time table at 30 Hz: 0 and the next <paramref name="count"/> − 1 frame times.</summary>
    internal static byte[] Times(int count) => Floats([.. Enumerable.Range(0, count).Select(frame => (float)(frame / 30.0))]);

    internal static byte[] Floats(params float[] values) => [.. values.SelectMany(BitConverter.GetBytes)];

    /// <summary>A notify event storing only its state class, duration, end link and start; every other member is zero.</summary>
    internal static byte[] NotifyEvent(int stateClass, float start, float duration)
    {
        const uint Stored = (1u << 5) | (1u << 6) | (1u << 7) | (1u << 24);
        uint zeroMask = ((1u << 26) - 1) & ~Stored;
        return
        [
            0x80, 0x35, .. BitConverter.GetBytes(zeroMask),
            .. BitConverter.GetBytes(stateClass), .. BitConverter.GetBytes(duration),
            .. PropertyBlocks.Header(7), .. BitConverter.GetBytes(start + duration),
            .. BitConverter.GetBytes(start),
        ];
    }

    /// <summary>
    /// A package of a sequence export, a branch tag raising state 4 (reference 2) and one raising none (reference 3);
    /// with <paramref name="shiftEnds"/>, every end link is moved 0.01 s later.
    /// </summary>
    private static CookedPackage Package(byte[] sequence, bool shiftEnds)
    {
        if (shiftEnds)
        {
            for (int at = 0; at + 2 <= sequence.Length; at++)
            {
                if (sequence[at] == 0x80 && sequence[at + 1] == 0x35)
                {
                    int link = at + 2 + 4 + 4 + 4 + 2;
                    BitConverter.TryWriteBytes(sequence.AsSpan(link), BitConverter.ToSingle(sequence, link) + 0.01f);
                }
            }
        }

        byte[] raising = [.. PropertyBlocks.Header(1), 4, 0, 0, 0, 0];
        byte[] silent = [.. PropertyBlocks.Header(), 0, 0, 0, 0];
        return CookedPackage.Parse(SyntheticArchives.Package("/Game/Anims/Roll",
        [
            ("Roll", PackageObjectIndex.Null, PackageObjectIndex.Null, 0, sequence),
            (BranchTagPrefix + "0", PackageObjectIndex.FromExport(0), PackageObjectIndex.Null, 0, raising),
            (BranchTagPrefix + "1", PackageObjectIndex.FromExport(0), PackageObjectIndex.Null, 0, silent),
        ]));
    }
}
