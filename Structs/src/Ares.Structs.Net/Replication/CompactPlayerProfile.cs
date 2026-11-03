using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>A tenancy user id. Only encoding 1 (a 64-bit id) is supported.</summary>
/// <param name="Value">The id.</param>
public sealed record TenancyUserId(ulong Value)
{
    /// <summary>Writes the id.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteBits(1, 2);
        writer.WriteBits(Value, 64);
    }

    /// <summary>Reads the id, rejecting unsupported encodings.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The id.</returns>
    public static TenancyUserId Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadBits(2) == 1
            ? new TenancyUserId(reader.ReadBits(64))
            : throw new InvalidPacketException("Unsupported tenancy user id encoding");
    }
}

/// <summary>The public profile fields sent to squad members.</summary>
/// <param name="TenancyUserId">Tenancy id.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="DisplayNameDiscriminator">Display-name discriminator.</param>
/// <param name="ThirdPartyUserId">Platform user id.</param>
/// <param name="ThirdPartyLastSeenAccountName">Platform display name.</param>
public sealed record CompactPlayerProfile(
    TenancyUserId TenancyUserId,
    string DisplayName,
    string DisplayNameDiscriminator,
    string ThirdPartyUserId,
    string ThirdPartyLastSeenAccountName)
{
    /// <summary>Writes the profile.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        TenancyUserId.Write(writer);
        writer.WriteString(DisplayName);
        writer.WriteString(DisplayNameDiscriminator);
        writer.WriteString(ThirdPartyUserId);
        writer.WriteString(ThirdPartyLastSeenAccountName);
    }

    /// <summary>Reads the profile.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The profile.</returns>
    public static CompactPlayerProfile Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return new CompactPlayerProfile(TenancyUserId.Read(reader), reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadString());
    }
}
