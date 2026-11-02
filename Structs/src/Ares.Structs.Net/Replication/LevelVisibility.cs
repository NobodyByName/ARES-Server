using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>Identifies a level visibility change so its acknowledgement can be matched.</summary>
/// <param name="Index">Transaction index (31 bits).</param>
/// <param name="Client">Whether the client started the transaction.</param>
public sealed record VisibilityTransaction(int Index, bool Client)
{
    /// <summary>Writes the transaction.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentOutOfRangeException.ThrowIfNegative(Index);
        writer.WriteBit(Client);
        writer.WritePacked((uint)Index);
    }

    /// <summary>Reads a transaction.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The transaction.</returns>
    public static VisibilityTransaction Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        bool client = reader.ReadBit();
        uint index = reader.ReadPacked();
        return index <= int.MaxValue ? new VisibilityTransaction((int)index, client) : throw new InvalidPacketException("Transaction index above 31 bits");
    }
}

/// <summary>A level's visibility as the client reports it to the server.</summary>
/// <param name="Package">Level package name.</param>
/// <param name="File">Level file name (<c>None</c> or equal to the package when the same).</param>
/// <param name="Visible">Whether the level is visible.</param>
/// <param name="Trying">Whether the client is still making it visible.</param>
/// <param name="Transaction">Transaction to acknowledge.</param>
public sealed record LevelVisibility(string Package, string File, bool Visible, bool Trying, VisibilityTransaction Transaction)
{
    private const int NameLimit = 1024;

    /// <summary>Writes the report.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Validate();
        bool same = File == "None" || File == Package;
        writer.WriteBit(same);
        writer.WriteBit(Visible);
        writer.WriteBit(Trying);
        new NetworkName(Package, 0).Write(writer);
        if (!same)
        {
            new NetworkName(File, 0).Write(writer);
        }

        Transaction.Write(writer);
    }

    /// <summary>Reads a report. Numbered level names are not supported.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The report.</returns>
    public static LevelVisibility Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        bool same = reader.ReadBit();
        bool visible = reader.ReadBit();
        bool trying = reader.ReadBit();
        string package = ReadName(reader);
        string file = same ? package : ReadName(reader);
        LevelVisibility result = new(package, file, visible, trying, VisibilityTransaction.Read(reader));
        result.Validate();
        return result;
    }

    private void Validate()
    {
        if (!IsName(Package) || !IsName(File) || (Visible && Trying))
        {
            throw new InvalidPacketException("Invalid level visibility");
        }
    }

    private static string ReadName(BitReader reader)
    {
        NetworkName name = NetworkName.Read(reader);
        return name.Number == 0 ? name.Text : throw new InvalidPacketException("Numbered level names are not supported");
    }

    private static bool IsName(string name) => name.Length is > 0 and <= NameLimit && !name.Contains('\0', StringComparison.Ordinal);
}
