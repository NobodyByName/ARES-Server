using System.Globalization;
using System.Text.RegularExpressions;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.Transport;

/// <summary>Options of a login URL (<c>map?Name=Value?...#portal</c>).</summary>
public static class LoginOptions
{
    /// <summary>Options by lowercase name; null when a name repeats.</summary>
    /// <param name="url">The login URL.</param>
    /// <returns>The options, or null.</returns>
    public static Dictionary<string, string>? Parse(string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        (string Name, string Value)[] options =
        [
            .. url.Split('#')[0].Split('?').Skip(1)
                .Select(option => option.Split('=', 2))
                .Select(parts => (parts[0].ToLowerInvariant(), parts.Length > 1 ? parts[1] : "")),
        ];
        return options.DistinctBy(option => option.Name).Count() == options.Length
            ? options.ToDictionary(option => option.Name, option => option.Value, StringComparer.Ordinal)
            : null;
    }
}

/// <summary>
/// What a login gives the player's actors: viewport options for the player controller when it opens, and the
/// player's unique id for its player state.
/// </summary>
/// <param name="HandshakeId">Handshake id option.</param>
/// <param name="LocalPlayerId">Local player id option.</param>
/// <param name="UniqueId">The unique id the client logged in with.</param>
public sealed partial record PlayerLogin(uint HandshakeId, int LocalPlayerId, UniqueNetId UniqueId)
{
    /// <summary>Reads a login; missing viewport options are 0.</summary>
    /// <param name="login">The login.</param>
    /// <returns>The player login.</returns>
    public static PlayerLogin FromLogin(ControlLogin login)
    {
        ArgumentNullException.ThrowIfNull(login);
        Dictionary<string, string> options = LoginOptions.Parse(login.Url) ?? throw new InvalidPacketException("Duplicate login option");

        return new PlayerLogin(
            (uint)Number(options, "handshakeid", 0, uint.MaxValue),
            (int)Number(options, "localplayerid", int.MinValue, int.MaxValue),
            login.UniqueId);
    }

    /// <summary>The player controller's opening fields (primary player only).</summary>
    /// <returns>The fields.</returns>
    public PlayerControllerOpen ControllerOpen() => new(0, HandshakeId, LocalPlayerId);

    private static long Number(Dictionary<string, string> options, string name, long minimum, long maximum)
    {
        if (!options.TryGetValue(name, out string? text))
        {
            return 0;
        }

        return text.Length <= 11 && NumberPattern().IsMatch(text) && long.Parse(text, CultureInfo.InvariantCulture) is var value && value >= minimum && value <= maximum
            ? value
            : throw new InvalidPacketException("Invalid numeric viewport option");
    }

    [GeneratedRegex("^[+-]?[0-9]+$")]
    private static partial Regex NumberPattern();
}

/// <summary>
/// The control exchange after Welcome: the client's network speed, then its Join once the map is
/// loaded; actor channel failures are reported as they come.
/// </summary>
/// <param name="maxNetSpeed">Largest accepted network speed.</param>
public sealed class WorldTravel(int maxNetSpeed = 1_000_000)
{
    private const int MinNetSpeed = 1800;
    private const int MaxMessages = 64;

    /// <summary>Whether the Welcome was sent.</summary>
    private bool WelcomeSent { get; set; }

    /// <summary>Client network speed, once received.</summary>
    private int? NetSpeed { get; set; }

    /// <summary>Whether the client joined the map.</summary>
    public bool Joined { get; private set; }

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public WorldTravel Copy() => (WorldTravel)MemberwiseClone();

    /// <summary>Records that the Welcome was sent.</summary>
    public void SentWelcome() => WelcomeSent = true;

    /// <summary>Reads the control messages of one bunch; nothing changes if any message is invalid.</summary>
    /// <param name="payload">Bunch payload.</param>
    /// <param name="bits">Payload bits.</param>
    /// <returns>Channel indexes the client reported failures for, and whether this bunch joined the map.</returns>
    public (IReadOnlyList<int> ChannelFailures, bool JoinedNow) Receive(byte[] payload, long bits)
    {
        if (!WelcomeSent)
        {
            throw new InvalidPacketException("Travel control arrived before Welcome");
        }

        BitReader reader = new(payload, bits);
        int? netSpeed = NetSpeed;
        bool joined = Joined;
        bool joinedNow = false;
        List<int> failures = [];
        int messages = 0;
        while (reader.Remaining > 0)
        {
            if (++messages > MaxMessages)
            {
                throw new InvalidPacketException("Too many travel control messages");
            }

            switch (ControlMessage.Read(reader))
            {
                case ControlNetspeed speed:
                    netSpeed = Math.Clamp(speed.Rate, MinNetSpeed, maxNetSpeed);
                    break;
                case ControlActorChannelFailure failure:
                    failures.Add(failure.Channel);
                    break;
                case ControlJoin when netSpeed is not null:
                    joinedNow |= !joined;
                    joined = true;
                    break;
                case ControlJoin:
                    throw new InvalidPacketException("Join arrived before Netspeed");
                default:
                    throw new InvalidPacketException("Unsupported travel control message");
            }
        }

        if (messages == 0)
        {
            throw new InvalidPacketException("Empty travel control payload");
        }

        NetSpeed = netSpeed;
        Joined = joined;
        return (failures, joinedNow);
    }
}
