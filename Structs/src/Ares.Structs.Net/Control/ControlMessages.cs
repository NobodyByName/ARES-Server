using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Control;

/// <summary>Control channel message types used by the connection flow.</summary>
public enum ControlMessageType : byte
{
    /// <summary>Client hello.</summary>
    Hello = 0,

    /// <summary>Server welcome with the map to load.</summary>
    Welcome = 1,

    /// <summary>Server login challenge.</summary>
    Challenge = 3,

    /// <summary>Client network speed.</summary>
    Netspeed = 4,

    /// <summary>Client login.</summary>
    Login = 5,

    /// <summary>Connection failure with a reason.</summary>
    Failure = 6,

    /// <summary>Client join request.</summary>
    Join = 9,

    /// <summary>The client could not create the actor of a channel.</summary>
    ActorChannelFailure = 16,

    /// <summary>Close reason.</summary>
    CloseReason = 23,

    /// <summary>Server request for the client's product user id.</summary>
    ProductUserIdRequest = 81,

    /// <summary>Client product user id and token.</summary>
    ProductUserIdResponse = 82,

    /// <summary>Server request for a client proof.</summary>
    ProofRequest = 87,

    /// <summary>Client proof.</summary>
    ProofResponse = 88,
}

/// <summary>A control channel message: a type byte, then its fields.</summary>
public abstract record ControlMessage
{
    /// <summary>Message type.</summary>
    public abstract ControlMessageType Type { get; }

    /// <summary>Writes the message.</summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte((byte)Type);
        WriteFields(writer);
    }

    /// <summary>Reads a message of any supported type.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The message.</returns>
    public static ControlMessage Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return (ControlMessageType)reader.ReadByte() switch
        {
            ControlMessageType.Hello => ControlHello.ReadFields(reader),
            ControlMessageType.Welcome => new ControlWelcome(reader.ReadString(), reader.ReadString(), reader.ReadString()),
            ControlMessageType.Challenge => new ControlChallenge(reader.ReadString()),
            ControlMessageType.Netspeed => new ControlNetspeed((int)(uint)reader.ReadBits(32)),
            ControlMessageType.Login => ControlLogin.ReadFields(reader),
            ControlMessageType.Failure => new ControlFailure(reader.ReadString()),
            ControlMessageType.Join => new ControlJoin(),
            ControlMessageType.ActorChannelFailure => ControlActorChannelFailure.ReadFields(reader),
            ControlMessageType.CloseReason => new ControlCloseReason(reader.ReadString()),
            ControlMessageType.ProductUserIdRequest => new ControlProductUserIdRequest(),
            ControlMessageType.ProductUserIdResponse => new ControlProductUserIdResponse(
                reader.ReadString(ControlProductUserIdResponse.UserIdLimit), reader.ReadString(ControlProductUserIdResponse.TokenLimit)),
            ControlMessageType.ProofRequest => new ControlProofRequest(reader.ReadString()),
            ControlMessageType.ProofResponse => new ControlProofResponse(reader.ReadString(ControlProofResponse.ProofLimit)),
            _ => throw new InvalidPacketException("Unsupported control message"),
        };
    }

    /// <summary>Writes the fields after the type byte.</summary>
    /// <param name="writer">Destination.</param>
    protected abstract void WriteFields(BitWriter writer);
}

/// <summary>Client hello. Only little-endian clients are supported.</summary>
/// <param name="NetworkVersion">Network version checksum.</param>
/// <param name="EncryptionToken">Encryption token (empty without encryption).</param>
/// <param name="NetworkFeatures">Network feature flags.</param>
public sealed record ControlHello(uint NetworkVersion, string EncryptionToken, ushort NetworkFeatures) : ControlMessage
{
    private const byte LittleEndian = 1;

    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.Hello;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer)
    {
        writer.WriteByte(LittleEndian);
        writer.WriteBits(NetworkVersion, 32);
        writer.WriteString(EncryptionToken);
        writer.WriteBits(NetworkFeatures, 16);
    }

    internal static ControlHello ReadFields(BitReader reader)
    {
        if (reader.ReadByte() != LittleEndian)
        {
            throw new InvalidPacketException("Only little-endian clients are supported");
        }

        uint version = (uint)reader.ReadBits(32);
        string token = reader.ReadString();
        return new ControlHello(version, token, (ushort)reader.ReadBits(16));
    }
}

/// <summary>Server welcome.</summary>
/// <param name="MapPackage">Map package to load.</param>
/// <param name="GameMode">Game mode class.</param>
/// <param name="RedirectUrl">Redirect URL (usually empty).</param>
public sealed record ControlWelcome(string MapPackage, string GameMode, string RedirectUrl) : ControlMessage
{
    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.Welcome;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer)
    {
        writer.WriteString(MapPackage);
        writer.WriteString(GameMode);
        writer.WriteString(RedirectUrl);
    }
}

/// <summary>Server login challenge.</summary>
/// <param name="Challenge">Challenge text.</param>
public sealed record ControlChallenge(string Challenge) : ControlMessage
{
    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.Challenge;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer) => writer.WriteString(Challenge);
}

/// <summary>Client network speed.</summary>
/// <param name="Rate">Rate in bytes per second.</param>
public sealed record ControlNetspeed(int Rate) : ControlMessage
{
    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.Netspeed;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer) => writer.WriteBits((uint)Rate, 32);
}

/// <summary>Connection failure.</summary>
/// <param name="Reason">Failure reason.</param>
public sealed record ControlFailure(string Reason) : ControlMessage
{
    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.Failure;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer) => writer.WriteString(Reason);
}

/// <summary>Client join request.</summary>
public sealed record ControlJoin : ControlMessage
{
    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.Join;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer)
    {
    }
}

/// <summary>The client could not create the actor of a channel.</summary>
/// <param name="Channel">Channel index.</param>
public sealed record ControlActorChannelFailure(int Channel) : ControlMessage
{
    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.ActorChannelFailure;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer) => writer.WriteBits((uint)Channel, 32);

    internal static ControlActorChannelFailure ReadFields(BitReader reader)
    {
        int channel = (int)(uint)reader.ReadBits(32);
        return channel >= 0 ? new ControlActorChannelFailure(channel) : throw new InvalidPacketException("Invalid actor channel index");
    }
}

/// <summary>Close reason.</summary>
/// <param name="Reason">Reason text.</param>
public sealed record ControlCloseReason(string Reason) : ControlMessage
{
    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.CloseReason;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer) => writer.WriteString(Reason);
}

/// <summary>Server request for the client's product user id.</summary>
public sealed record ControlProductUserIdRequest : ControlMessage
{
    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.ProductUserIdRequest;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer)
    {
    }
}

/// <summary>Client product user id and its token.</summary>
/// <param name="ProductUserId">Product user id.</param>
/// <param name="Token">Identity token.</param>
public sealed record ControlProductUserIdResponse(string ProductUserId, string Token) : ControlMessage
{
    /// <summary>Largest product user id, terminator included.</summary>
    public const int UserIdLimit = 128;

    /// <summary>Largest token, terminator included.</summary>
    public const int TokenLimit = 8192;

    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.ProductUserIdResponse;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer)
    {
        writer.WriteString(ProductUserId, UserIdLimit);
        writer.WriteString(Token, TokenLimit);
    }
}

/// <summary>Server request for a client proof.</summary>
/// <param name="Nonce">Nonce to prove.</param>
public sealed record ControlProofRequest(string Nonce) : ControlMessage
{
    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.ProofRequest;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer) => writer.WriteString(Nonce);
}

/// <summary>Client proof.</summary>
/// <param name="Proof">The proof.</param>
public sealed record ControlProofResponse(string Proof) : ControlMessage
{
    /// <summary>Largest proof, terminator included.</summary>
    public const int ProofLimit = 8192;

    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.ProofResponse;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer) => writer.WriteString(Proof, ProofLimit);
}

/// <summary>Client login.</summary>
/// <param name="Response">Challenge response.</param>
/// <param name="Url">Login URL with options.</param>
/// <param name="UniqueId">Player unique id (never trusted for authorization).</param>
/// <param name="Platform">Platform name.</param>
public sealed record ControlLogin(string Response, string Url, UniqueNetId UniqueId, string Platform) : ControlMessage
{
    /// <summary>Largest platform name, terminator included.</summary>
    public const int PlatformLimit = 128;

    /// <inheritdoc />
    public override ControlMessageType Type => ControlMessageType.Login;

    /// <inheritdoc />
    protected override void WriteFields(BitWriter writer)
    {
        writer.WriteString(Response);
        writer.WriteString(Url);
        UniqueId.Write(writer);
        writer.WriteString(Platform, PlatformLimit);
    }

    internal static ControlLogin ReadFields(BitReader reader)
    {
        string response = reader.ReadString();
        string url = reader.ReadString();
        UniqueNetId id = UniqueNetId.Read(reader);
        return new ControlLogin(response, url, id, reader.ReadString(PlatformLimit));
    }
}
