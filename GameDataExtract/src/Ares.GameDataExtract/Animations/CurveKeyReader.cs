using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Animations;

/// <summary>A weighted cubic key of a rich curve.</summary>
internal readonly record struct CurveKey(float Time, float Value, float ArriveTangent, float ArriveWeight, float LeaveTangent, float LeaveWeight);

/// <summary>
/// Reads the keys of a float curve asset whose only stored member is its rich curve, whose only stored member is its
/// keys: two one-member headers, an int32 key count, the keys (<see cref="AnimationFormats.RichCurveKeySize"/> bytes
/// each), then the four-byte zero trailer. Every key must be a cubic key with both tangents weighted.
/// </summary>
internal static class CurveKeyReader
{
    private const int Trailer = 4;
    private const int MaxKeys = 1000;

    // Highest ERichCurveInterpMode, ERichCurveTangentMode and ERichCurveTangentWeightMode values.
    private const byte MaxInterpolation = 3;
    private const byte MaxTangentMode = 4;
    private const byte MaxTangentWeightMode = 3;
    private const int KeysOffset = 8;
    private const int FloatsOffset = 3;
    private static readonly byte[] s_headers = [0x00, 0x03, 0x00, 0x03];

    /// <summary>The keys of <paramref name="export"/> in stored order.</summary>
    /// <exception cref="InvalidDataException">The export is not laid out as described, or a key is not weighted cubic.</exception>
    public static IReadOnlyList<CurveKey> Read(CookedPackage package, ZenExport export)
    {
        ReadOnlySpan<byte> raw = package.Data.AsSpan(CookedPackage.StartOf(export), (int)export.SerialSize);
        int count = raw.Length >= KeysOffset ? ExportBytes.Int32(raw, 4) : -1;
        if (count < 1 || raw.Length != KeysOffset + (count * AnimationFormats.RichCurveKeySize) + Trailer || !raw[..s_headers.Length].SequenceEqual(s_headers)
            || raw[^Trailer..].ContainsAnyExcept((byte)0))
        {
            throw new InvalidDataException($"{package.Name}: unsupported curve serialization");
        }

        CurveKey[] keys = new CurveKey[count];
        for (int key = 0; key < count; key++)
        {
            int at = KeysOffset + (key * AnimationFormats.RichCurveKeySize);
            if (raw[at] != AnimationFormats.CubicInterpolation || raw[at + 2] != AnimationFormats.WeightedBothTangents)
            {
                throw new InvalidDataException($"{package.Name}: key {key} is not a weighted cubic key");
            }

            keys[key] = Key(raw, at);
        }

        return keys;
    }

    /// <summary>
    /// The latest key time of a curve asset whose stored members are all rich curves that store only their keys (each
    /// a one-member header, an int32 key count and the keys, any interpolation), followed by at most
    /// <paramref name="trailer"/> zero bytes. The keys of every curve must be in time order from zero or later.
    /// </summary>
    /// <exception cref="InvalidDataException">The export is not laid out as described.</exception>
    public static float LatestKeyTime(CookedPackage package, ZenExport export, int trailer)
    {
        int start = CookedPackage.StartOf(export);
        int end = start + (int)export.SerialSize;
        ReadOnlySpan<byte> data = package.Data.AsSpan(0, end);
        UnversionedHeader curves = UnversionedHeader.Read(data, start);
        if (curves.Members.Count == 0 || curves.NonZeroCount != curves.Members.Count)
        {
            throw new InvalidDataException($"{package.Name}: the curve asset stores no curve or a zero member");
        }

        float latest = 0;
        int at = curves.ValuesOffset;
        foreach (UnversionedMember _ in curves.Members)
        {
            UnversionedHeader curve = UnversionedHeader.Read(data, at);
            if (curve.Members is not [{ Index: 0, IsNonZero: true }])
            {
                throw new InvalidDataException($"{package.Name}: a curve stores more than its keys");
            }

            int count = ExportBytes.Int32(data, curve.ValuesOffset);
            if (count is < 1 or > MaxKeys)
            {
                throw new InvalidDataException($"{package.Name}: unexpected key count {count}");
            }

            at = curve.ValuesOffset + sizeof(int);
            float previous = 0;
            for (int key = 0; key < count; key++, at += AnimationFormats.RichCurveKeySize)
            {
                if (at + AnimationFormats.RichCurveKeySize > end || data[at] > MaxInterpolation || data[at + 1] > MaxTangentMode || data[at + 2] > MaxTangentWeightMode)
                {
                    throw new InvalidDataException($"{package.Name}: key {key} runs past the export or has unknown modes");
                }

                float time = Key(data, at).Time;
                if (time < previous)
                {
                    throw new InvalidDataException($"{package.Name}: curve keys out of order");
                }

                previous = time;
            }

            latest = Math.Max(latest, previous);
        }

        if (end - at > trailer || data[at..end].ContainsAnyExcept((byte)0))
        {
            throw new InvalidDataException($"{package.Name}: {end - at} unread bytes follow the curves");
        }

        return latest;
    }

    private static CurveKey Key(ReadOnlySpan<byte> raw, int at)
    {
        int floats = at + FloatsOffset;
        return new CurveKey(ExportBytes.Single(raw, floats), ExportBytes.Single(raw, floats + 4), ExportBytes.Single(raw, floats + 8),
            ExportBytes.Single(raw, floats + 12), ExportBytes.Single(raw, floats + 16), ExportBytes.Single(raw, floats + 20));
    }
}
