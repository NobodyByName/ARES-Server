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

            int floats = at + FloatsOffset;
            keys[key] = new CurveKey(ExportBytes.Single(raw, floats), ExportBytes.Single(raw, floats + 4), ExportBytes.Single(raw, floats + 8),
                ExportBytes.Single(raw, floats + 12), ExportBytes.Single(raw, floats + 16), ExportBytes.Single(raw, floats + 20));
        }

        return keys;
    }
}
