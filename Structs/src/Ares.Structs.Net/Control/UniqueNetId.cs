using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Control;

/// <summary>
/// A player unique id as sent on the wire. It identifies, it never authorizes. Online-services
/// ids (type 30) are not supported.
/// </summary>
/// <param name="TypeHash">Id type (0 to 31; 31 means the type is named by <paramref name="TypeName"/>).</param>
/// <param name="Contents">Id text, or lowercase hex digits when <paramref name="Encoded"/>.</param>
/// <param name="TypeName">Type name when <paramref name="TypeHash"/> is 31.</param>
/// <param name="Encoded">Whether the id is sent as bytes.</param>
/// <param name="Padded">Whether the hex digits were padded with a leading zero to whole bytes.</param>
public sealed record UniqueNetId(int TypeHash, string Contents, string TypeName, bool Encoded, bool Padded)
{
    private const int EncodedFlag = 1;
    private const int EmptyFlag = 2;
    private const int PaddedFlag = 4;
    private const int OnlineServicesType = 30;
    private const int NamedType = 31;
    private const int NameLimit = 64;
    private const int ContentsLimit = 256;

    /// <summary>The empty id.</summary>
    public static UniqueNetId Empty { get; } = new(0, "", "", false, false);

    /// <summary>Writes the id.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (Contents.Length == 0)
        {
            writer.WriteByte(EncodedFlag | EmptyFlag);
            return;
        }

        if (TypeHash is < 0 or > NamedType || TypeHash == OnlineServicesType || (Padded && !Encoded))
        {
            throw new InvalidPacketException("Unsupported unique id encoding");
        }

        writer.WriteByte((byte)((TypeHash << 3) | (Encoded ? EncodedFlag : 0) | (Padded ? PaddedFlag : 0)));
        if (TypeHash == NamedType)
        {
            if (TypeName.Length == 0 || TypeName == "None")
            {
                throw new InvalidPacketException("Missing unique id type name");
            }

            writer.WriteString(TypeName, NameLimit);
        }

        if (!Encoded)
        {
            writer.WriteString(Contents, ContentsLimit);
            return;
        }

        byte[] raw;
        try
        {
            raw = Convert.FromHexString((Padded ? "0" : "") + Contents);
        }
        catch (FormatException error)
        {
            throw new InvalidPacketException("Invalid encoded unique id", error);
        }

        if (raw.Length is < 1 or > 255)
        {
            throw new InvalidPacketException("Invalid encoded unique id size");
        }

        writer.WriteByte((byte)raw.Length);
        writer.WriteBytes(raw);
    }

    /// <summary>Reads an id.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The id.</returns>
    public static UniqueNetId Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        int flags = reader.ReadByte();
        bool encoded = (flags & EncodedFlag) != 0;
        bool padded = (flags & PaddedFlag) != 0;
        int type = flags >> 3;
        if ((flags & EmptyFlag) != 0)
        {
            return flags == (EncodedFlag | EmptyFlag) ? Empty : throw new InvalidPacketException("Unsupported empty unique id flags");
        }

        if (type == OnlineServicesType || (padded && !encoded))
        {
            throw new InvalidPacketException("Unsupported unique id");
        }

        string name = type == NamedType ? reader.ReadString(NameLimit) : "";
        if (type == NamedType && (name.Length == 0 || name == "None"))
        {
            throw new InvalidPacketException("Missing unique id type name");
        }

        string contents;
        if (encoded)
        {
            int size = reader.ReadByte();
            if (size == 0)
            {
                throw new InvalidPacketException("Empty encoded unique id");
            }

            contents = Convert.ToHexStringLower(reader.ReadBytes(size));
            if (padded)
            {
                contents = contents[0] == '0' ? contents[1..] : throw new InvalidPacketException("Invalid unique id padding");
            }
        }
        else
        {
            contents = reader.ReadString(ContentsLimit);
            if (contents.Length == 0)
            {
                throw new InvalidPacketException("Empty unique id");
            }
        }

        return new UniqueNetId(type, contents, name, encoded, padded);
    }
}
