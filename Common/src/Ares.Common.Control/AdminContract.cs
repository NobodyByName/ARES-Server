namespace Ares.Common.Control;

/// <summary>Operator → Raid: an admin command for the raid's players, as typed on a console.</summary>
/// <param name="Name">Command name, one of <see cref="AdminApi.Commands"/>.</param>
/// <param name="Arguments">Its arguments.</param>
public sealed record AdminCommand(string Name, IReadOnlyList<string> Arguments);

/// <summary>Raid → operator: what an admin command did, or why it was refused.</summary>
/// <param name="Lines">Readable result lines.</param>
/// <param name="Error">Machine-readable reason of a refusal, or null when the command ran.</param>
public sealed record AdminReply(IReadOnlyList<string> Lines, string? Error = null);

/// <summary>The admin API of a Raid server: its path, commands and refusal reasons.</summary>
public static class AdminApi
{
    /// <summary>Command endpoint path.</summary>
    public const string CommandPath = "/admin/command";

    /// <summary>Largest request body, in bytes.</summary>
    public const int MaximumRequestBytes = 4096;

    /// <summary>Most arguments of one command.</summary>
    public const int MaximumArguments = 8;

    /// <summary>Lists the raid's players with their phase, position and heading.</summary>
    public const string Players = "players";

    /// <summary>Moves a player's character.</summary>
    public const string Teleport = "teleport";

    /// <summary>Moves a player's character in front of a loot container.</summary>
    public const string TeleportToLoot = "teleport-to-loot";

    /// <summary>Spawns a loot container in front of a player's character.</summary>
    public const string SpawnLoot = "spawn-loot";

    /// <summary>The call carried no valid operator token.</summary>
    public const string AuthenticationRequired = "operator_authentication_required";

    /// <summary>The body is not a valid command.</summary>
    public const string InvalidRequest = "invalid_admin_request";

    /// <summary>The command or its arguments are not understood; the lines show the usage.</summary>
    public const string InvalidCommand = "invalid_admin_command";

    /// <summary>The raid has no such player.</summary>
    public const string PlayerNotFound = "player_not_found";

    /// <summary>The raid's game loop did not take the command in time.</summary>
    public const string Busy = "raid_busy";

    /// <summary>Command names.</summary>
    public static IReadOnlyList<string> Commands { get; } = [Players, Teleport, TeleportToLoot, SpawnLoot];

    /// <summary>One line per command: its arguments and what it does. A player is a user id or an in-game name.</summary>
    public static IReadOnlyList<string> Usage { get; } =
    [
        "players  List the raid's players: id, name, phase, position and heading",
        "teleport <player> <x> <y> <z> [yaw]  Move a player (feet position, yaw in degrees)",
        "teleport-to-loot <player> <index> | free-standing <n>  Move a player in front of a loot container",
        "spawn-loot <player> <class>  Spawn a loot container (class name or path) in front of a player",
    ];
}
