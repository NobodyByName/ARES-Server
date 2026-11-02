using System.Globalization;
using System.Security.Cryptography;
using Ares.Structs.Net.Control;

namespace Ares.Raid.Transport;

/// <summary>A player admitted to a raid.</summary>
/// <param name="UserId">Tenancy user id.</param>
/// <param name="MatchId">The raid's match id.</param>
public sealed record SessionPrincipal(long UserId, string MatchId);

/// <summary>Resolves routing credentials to the players they admit.</summary>
public interface IRouteResolver
{
    /// <summary>The player a routing credential admits, or null when it is unknown or expired.</summary>
    /// <param name="token">The 16-byte credential.</param>
    /// <param name="renew">Whether to extend the reservation (the player is active).</param>
    /// <returns>The player, or null.</returns>
    SessionPrincipal? Route(ReadOnlySpan<byte> token, bool renew);
}

/// <summary>A connection or login was not admitted.</summary>
/// <param name="message">Why.</param>
public sealed class AuthenticationRejectedException(string message) : Exception(message);

/// <summary>Login progress of one connection.</summary>
/// <param name="principal">The routed player.</param>
/// <param name="routingToken">The routing credential of the connection.</param>
/// <param name="challenge">Challenge text sent to the client.</param>
public sealed class PendingLogin(SessionPrincipal principal, byte[] routingToken, string challenge)
{
    /// <summary>The routed player.</summary>
    public SessionPrincipal Principal { get; } = principal;

    /// <summary>The routing credential of the connection.</summary>
    public byte[] RoutingToken { get; } = routingToken;

    /// <summary>Challenge text sent to the client.</summary>
    public string Challenge { get; } = challenge;

    /// <summary>Whether the challenge was sent.</summary>
    public bool ChallengeSent { get; set; }

    /// <summary>Whether the client acknowledged the challenge.</summary>
    public bool ChallengeAcknowledged { get; set; }

    /// <summary>Whether the login completed.</summary>
    public bool Authenticated { get; set; }

    /// <summary>Hash of the login message, to detect a conflicting retransmission.</summary>
    public byte[]? LoginFingerprint { get; set; }

    /// <summary>A copy for speculative packet processing.</summary>
    /// <returns>The copy.</returns>
    public PendingLogin Copy() => (PendingLogin)MemberwiseClone();
}

/// <summary>
/// Admits players by the routing credential the backend assigned to their reservation. Every client
/// datagram ends with the 16-byte credential and a zero byte; Hello and Login must name the routed player.
/// </summary>
/// <param name="routes">Resolves credentials.</param>
public sealed class RoutingAuthenticator(IRouteResolver routes)
{
    /// <summary>Size of a routing credential.</summary>
    public const int TokenBytes = 16;

    private readonly IRouteResolver _routes = routes ?? throw new ArgumentNullException(nameof(routes));

    /// <summary>Splits a datagram into its packet and routing credential, rejecting unknown credentials.</summary>
    /// <param name="datagram">The received datagram.</param>
    /// <returns>The packet and the credential.</returns>
    public (byte[] Packet, byte[] Token) Unwrap(byte[] datagram)
    {
        ArgumentNullException.ThrowIfNull(datagram);
        if (datagram.Length < TokenBytes + 2 || datagram[^1] != 0)
        {
            throw new AuthenticationRejectedException("Missing or unsupported routing wrapper");
        }

        byte[] token = datagram[^(TokenBytes + 1)..^1];
        Principal(token);
        return (datagram[..^(TokenBytes + 1)], token);
    }

    /// <summary>Starts a login at Hello: the Hello identity must be the routed player.</summary>
    /// <param name="helloToken">Identity text from Hello.</param>
    /// <param name="routingToken">The connection's credential.</param>
    /// <returns>The login state.</returns>
    public PendingLogin BeginHello(string helloToken, byte[] routingToken)
    {
        ArgumentNullException.ThrowIfNull(helloToken);
        SessionPrincipal principal = Principal(routingToken);
        return helloToken == UserText(principal)
            ? new PendingLogin(principal, routingToken, Convert.ToHexString(RandomNumberGenerator.GetBytes(4)))
            : throw new AuthenticationRejectedException("Hello identity does not match the routed reservation");
    }

    /// <summary>Checks that the credential still admits the same player.</summary>
    /// <param name="pending">The login state.</param>
    public void Validate(PendingLogin pending)
    {
        ArgumentNullException.ThrowIfNull(pending);
        if (Principal(pending.RoutingToken) != pending.Principal)
        {
            throw new AuthenticationRejectedException("Routing credential no longer owns this session");
        }
    }

    /// <summary>Extends the reservation after accepted traffic.</summary>
    /// <param name="pending">The login state.</param>
    public void Refresh(PendingLogin pending)
    {
        ArgumentNullException.ThrowIfNull(pending);
        _routes.Route(pending.RoutingToken, renew: true);
    }

    /// <summary>
    /// Completes the login: the response must be <c>0</c> after the challenge, and the login's identity
    /// option and unique id must name the routed player.
    /// </summary>
    /// <param name="pending">The login state.</param>
    /// <param name="login">The login message.</param>
    public void Complete(PendingLogin pending, ControlLogin login)
    {
        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(login);
        if (login.Response != "0" || !pending.ChallengeSent)
        {
            throw new AuthenticationRejectedException("Unexpected login response or order");
        }

        Dictionary<string, string> options = LoginOptions.Parse(login.Url) ?? throw new AuthenticationRejectedException("Duplicate login option");

        Validate(pending);
        string user = UserText(pending.Principal);
        if (options.GetValueOrDefault("encryptiontoken") != user || login.UniqueId.Contents != user)
        {
            throw new AuthenticationRejectedException("Login identity does not match the routed reservation");
        }

        pending.Authenticated = true;
    }

    private SessionPrincipal Principal(ReadOnlySpan<byte> token) =>
        token.Length == TokenBytes && _routes.Route(token, renew: false) is { } principal
            ? principal
            : throw new AuthenticationRejectedException("Missing, expired or wrong routing credential");

    private static string UserText(SessionPrincipal principal) => principal.UserId.ToString(CultureInfo.InvariantCulture);
}
