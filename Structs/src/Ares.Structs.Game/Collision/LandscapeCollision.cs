using System.Buffers.Binary;
using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.Collision;

/// <summary>
/// Landscape collision (<c>ULandscapeHeightfieldCollisionComponent</c>): reading the cooked height field from the
/// component's native data, and placing its samples in the landscape proxy's space.
/// </summary>
/// <remarks>
/// Sample (x, y) of a component lies at <c>relative location + (x · collision scale, y · collision scale,
/// height / 128)</c> in the space of the component's parent (the landscape proxy root); the proxy's transform takes it
/// to world space, so its Z scale sets the height step (scale / 128 per height unit) and its X/Y scale the sample
/// spacing. The PhysX field stores the landscape X axis reversed in its rows: sample (x, y) is row
/// <c>rows − 1 − x</c>, column <c>y</c>.
/// </remarks>
public static class LandscapeCollision
{
    /// <summary>Landscape heights are stored in units of 1/128 of the landscape's Z scale.</summary>
    public const double HeightScale = 1.0 / 128.0;

    /// <summary>
    /// Reads the component's native data after its tagged properties: the object GUID flag (and GUID when set), an
    /// unknown empty count, the cooked flag, then the cooked collision bytes as an array of one-byte elements.
    /// </summary>
    /// <param name="data">The export's bytes after its properties, up to the end of the export.</param>
    /// <returns>The PhysX height-field stream (read by <see cref="PhysXCookedData.ReadHeightField"/>), or empty when
    /// the component stores no cooked collision.</returns>
    /// <exception cref="InvalidDataException">The data is malformed.</exception>
    public static ReadOnlyMemory<byte> ReadCookedHeightField(ReadOnlyMemory<byte> data)
    {
        ReadOnlySpan<byte> span = data.Span;
        int at = 0;
        uint hasObjectGuid = ReadUInt32(span, ref at);
        if (hasObjectGuid > 1)
        {
            throw new InvalidDataException($"Invalid object GUID flag {hasObjectGuid}.");
        }

        at += (int)hasObjectGuid * 16;
        if (ReadUInt32(span, ref at) != 0)
        {
            throw new InvalidDataException("Unsupported non-empty array before the landscape collision data.");
        }

        uint cooked = ReadUInt32(span, ref at);
        ReadOnlyMemory<byte> result = ReadOnlyMemory<byte>.Empty;
        if (cooked == 1)
        {
            uint elementSize = ReadUInt32(span, ref at);
            uint count = ReadUInt32(span, ref at);
            if (elementSize != 1 || count > (uint)(data.Length - at))
            {
                throw new InvalidDataException($"Invalid landscape collision array ({count} elements of {elementSize} bytes).");
            }

            result = data.Slice(at, (int)count);
            at += (int)count;
        }
        else if (cooked != 0)
        {
            throw new InvalidDataException($"Invalid cooked flag {cooked}.");
        }

        return at == data.Length
            ? result
            : throw new InvalidDataException($"{data.Length - at} bytes left after the landscape collision data.");
    }

    /// <summary>Index in <see cref="HeightField.Samples"/> of landscape sample (x, y).</summary>
    /// <param name="field">The component's height field.</param>
    /// <param name="x">Sample index along the landscape X axis, 0 to rows − 1.</param>
    /// <param name="y">Sample index along the landscape Y axis, 0 to columns − 1.</param>
    public static int SampleIndex(HeightField field, int x, int y)
    {
        if ((uint)x >= (uint)field.Rows || (uint)y >= (uint)field.Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Sample outside the height field.");
        }

        return ((field.Rows - 1 - x) * field.Columns) + y;
    }

    /// <summary>Position of a sample in the space of the component's parent (the landscape proxy root).</summary>
    /// <param name="relativeLocation">The component's relative location.</param>
    /// <param name="collisionScale">The component's collision scale (sample spacing in landscape units).</param>
    /// <param name="x">Sample index along X.</param>
    /// <param name="y">Sample index along Y.</param>
    /// <param name="height">The sample's height.</param>
    public static Vector3D SamplePosition(Vector3D relativeLocation, double collisionScale, int x, int y, short height) =>
        new(
            relativeLocation.X + (x * collisionScale),
            relativeLocation.Y + (y * collisionScale),
            relativeLocation.Z + (height * HeightScale));

    private static uint ReadUInt32(ReadOnlySpan<byte> data, ref int at)
    {
        if (at < 0 || data.Length - at < 4)
        {
            throw new InvalidDataException("Landscape collision data truncated.");
        }

        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data[at..]);
        at += 4;
        return value;
    }
}
