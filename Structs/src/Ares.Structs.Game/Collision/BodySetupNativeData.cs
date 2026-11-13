using System.Buffers.Binary;

namespace Ares.Structs.Game.Collision;

/// <summary>
/// The cooked collision data of a BodySetup export, stored after its tagged properties.
/// </summary>
/// <param name="FormatNameIndex">Name-map index of the cooked format's name (see <see cref="BodySetupNativeData.CookedFormatName"/>).</param>
/// <param name="FormatNameNumber">Number suffix of the format name (0 for none).</param>
/// <param name="BulkDataIndex">Index of the format's entry in the package's bulk data table.</param>
/// <param name="Payload">The body payload, read by <see cref="PhysXCookedData.ReadBodyGeometry"/>.</param>
public readonly record struct BodySetupCookedFormat(int FormatNameIndex, int FormatNameNumber, int BulkDataIndex, ReadOnlyMemory<byte> Payload);

/// <summary>
/// Reads the native data of a BodySetup export: the object GUID flag (and GUID when set), the BodySetup GUID, the
/// cooked flag, one unknown word, the number of cooked formats, then per format its name, bulk data index and inline
/// payload. A body without cooked collision stores no format.
/// </summary>
public static class BodySetupNativeData
{
    /// <summary>Name of the cooked format that holds the body payload.</summary>
    public const string CookedFormatName = "Chaos";

    /// <summary>
    /// Reads the native data that follows a BodySetup export's tagged properties, up to the end of the export.
    /// </summary>
    /// <param name="data">The export's bytes after its properties.</param>
    /// <returns>The cooked format, or <see langword="null"/> when the body stores none.</returns>
    /// <exception cref="InvalidDataException">The data is malformed or holds more than one format.</exception>
    public static BodySetupCookedFormat? Read(ReadOnlyMemory<byte> data)
    {
        ReadOnlySpan<byte> span = data.Span;
        int at = 0;
        uint hasObjectGuid = ReadUInt32(span, ref at);
        if (hasObjectGuid > 1)
        {
            throw new InvalidDataException($"Invalid object GUID flag {hasObjectGuid}.");
        }

        at += (int)hasObjectGuid * 16 + 16; // object GUID when present, BodySetup GUID
        uint cooked = ReadUInt32(span, ref at);
        if (cooked > 1)
        {
            throw new InvalidDataException($"Invalid cooked flag {cooked}.");
        }

        BodySetupCookedFormat? format = cooked == 1 ? ReadFormat(data, ref at) : null;
        return at == data.Length
            ? format
            : throw new InvalidDataException($"{data.Length - at} bytes left after the BodySetup native data.");
    }

    /// <summary>
    /// Reads what follows a cooked flag of 1: an unknown word, the format count and, when there is one, the format,
    /// whose payload runs to the end of the data.
    /// </summary>
    private static BodySetupCookedFormat? ReadFormat(ReadOnlyMemory<byte> data, ref int at)
    {
        ReadOnlySpan<byte> span = data.Span;
        ReadUInt32(span, ref at); // unknown: 1 when a format follows, 0 otherwise
        uint formatCount = ReadUInt32(span, ref at);
        switch (formatCount)
        {
            case 0:
                return null;
            case > 1:
                throw new InvalidDataException($"BodySetups with {formatCount} cooked formats are not supported.");
        }

        int nameIndex = (int)ReadUInt32(span, ref at);
        int nameNumber = (int)ReadUInt32(span, ref at);
        int bulkDataIndex = (int)ReadUInt32(span, ref at);
        BodySetupCookedFormat format = new(nameIndex, nameNumber, bulkDataIndex, data[at..]);
        at = data.Length;
        return format;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, ref int at)
    {
        if (at < 0 || data.Length - at < 4)
        {
            throw new InvalidDataException("BodySetup native data truncated.");
        }

        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data[at..]);
        at += 4;
        return value;
    }
}
