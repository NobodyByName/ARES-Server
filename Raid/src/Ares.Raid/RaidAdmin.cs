using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json.Nodes;
using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Logging;
using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Loot;

namespace Ares.Raid;

/// <summary>An admin command with its arguments read.</summary>
/// <param name="Name">Command name (see <see cref="AdminApi"/>).</param>
/// <param name="Player">The player it acts on: a user id or an in-game name; null for <see cref="AdminApi.Players"/>.</param>
/// <param name="Position">Teleport destination.</param>
/// <param name="YawDegrees">Teleport heading, if given.</param>
/// <param name="Container">Container index of a teleport to loot.</param>
/// <param name="FreeStanding">Free-standing container number of a teleport to loot.</param>
/// <param name="Class">Container class to spawn.</param>
public sealed record AdminRequest(string Name, string? Player = null, Vector3D? Position = null, double? YawDegrees = null,
    int? Container = null, int? FreeStanding = null, LootContainerClass? Class = null);

/// <summary>
/// The raid's admin commands. A command is read on the caller's thread, then runs on the game loop between packets
/// (<see cref="RunPending"/>), where the players' state lives. Each command and its outcome is logged.
/// </summary>
public sealed class RaidAdmin : IAdminService
{
    private const string FreeStandingWord = "free-standing";
    private const int ListedClasses = 10;

    private readonly ConcurrentQueue<Action> _pending = new();
    private readonly Func<IEnumerable<PlayerChannels>> _players;
    private readonly JsonEventLog? _log;
    private readonly TimeSpan _timeout;

    /// <summary>Creates the service.</summary>
    /// <param name="players">The connected players; called on the game loop only.</param>
    /// <param name="log">Where commands and their outcomes are logged, or null.</param>
    /// <param name="timeout">How long a command waits for the game loop; 2 seconds by default.</param>
    public RaidAdmin(Func<IEnumerable<PlayerChannels>> players, JsonEventLog? log = null, TimeSpan? timeout = null)
    {
        _players = players ?? throw new ArgumentNullException(nameof(players));
        _log = log;
        _timeout = timeout ?? TimeSpan.FromSeconds(2);
    }

    /// <inheritdoc />
    public async Task<AdminResult> RunAsync(AdminCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        (AdminRequest? request, IReadOnlyList<string> problem) = Parse(command);
        AdminResult result;
        if (request is null)
        {
            result = AdminResult.Failure(StatusCodes.Status400BadRequest, AdminApi.InvalidCommand, problem);
        }
        else
        {
            TaskCompletionSource<AdminResult> done = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending.Enqueue(() =>
            {
                if (!done.Task.IsCompleted)
                {
                    done.TrySetResult(Run(request));
                }
            });
            try
            {
                result = await done.Task.WaitAsync(_timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                done.TrySetCanceled(CancellationToken.None);
                result = AdminResult.Failure(StatusCodes.Status503ServiceUnavailable, AdminApi.Busy, ["The raid did not take the command in time."]);
            }
        }

        _log?.Write(new JsonObject
        {
            ["raid_admin"] = command.Name,
            ["arguments"] = new JsonArray([.. command.Arguments.Select(argument => (JsonNode?)argument)]),
            ["status"] = result.StatusCode,
            ["error"] = result.Reply.Error,
            ["result"] = string.Join(" | ", result.Reply.Lines),
        });
        return result;
    }

    /// <summary>Runs the commands waiting for the game loop. The game loop calls it between packets.</summary>
    public void RunPending()
    {
        while (_pending.TryDequeue(out Action? work))
        {
            work();
        }
    }

    /// <summary>Reads a command's arguments.</summary>
    /// <param name="command">The command.</param>
    /// <returns>The request, or null and the lines explaining why it is not one.</returns>
    public static (AdminRequest? Request, IReadOnlyList<string> Problem) Parse(AdminCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        string[] words = [.. command.Arguments];
        if (HasNull(words))
        {
            return (null, Usage());
        }

        switch (command.Name.ToLowerInvariant(), words.Length)
        {
            case (AdminApi.Players, 0):
                return (new AdminRequest(AdminApi.Players), []);
            case (AdminApi.Teleport, 4 or 5):
                double[] values = [.. words.Skip(1).Select(Number).OfType<double>()];
                return values.Length == words.Length - 1
                    ? (new AdminRequest(AdminApi.Teleport, words[0], new Vector3D(values[0], values[1], values[2]), values.Length == 4 ? values[3] : null), [])
                    : (null, Usage());
            case (AdminApi.TeleportToLoot, 2) when Count(words[1]) is { } index:
                return (new AdminRequest(AdminApi.TeleportToLoot, words[0], Container: index), []);
            case (AdminApi.TeleportToLoot, 3) when words[1].Equals(FreeStandingWord, StringComparison.OrdinalIgnoreCase) && Count(words[2]) is { } number:
                return (new AdminRequest(AdminApi.TeleportToLoot, words[0], FreeStanding: number), []);
            case (AdminApi.SpawnLoot, 2):
                IReadOnlyList<LootContainerClass> classes = Classes(words[1]);
                return classes.Count == 1
                    ? (new AdminRequest(AdminApi.SpawnLoot, words[0], Class: classes[0]), [])
                    : (null, classes.Count == 0
                        ? [$"No supported container class matches {words[1]}."]
                        : [$"{classes.Count} container classes match {words[1]}:", .. classes.Take(ListedClasses).Select(ShortName)]);
            default:
                return (null, Usage());
        }

        static double? Number(string word) =>
            double.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value) ? value : null;

        static int? Count(string word) =>
            int.TryParse(word, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ? value : null;
    }

    // Arguments come from JSON, whose arrays can hold nulls the declared element type does not show.
    private static bool HasNull(IEnumerable<string?> words) => words.Any(word => word is null);

    /// <summary>The supported container classes a name picks: an exact class path or name (with or without its <c>_C</c>), else every name containing it.</summary>
    /// <param name="name">Class path, class name or part of a name.</param>
    /// <returns>The classes.</returns>
    public static IReadOnlyList<LootContainerClass> Classes(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        LootContainerClass[] exact = [.. LootContainerClasses.All.Where(type => type.Container.Class.Equals(name, StringComparison.OrdinalIgnoreCase)
            || ShortName(type).Equals(name, StringComparison.OrdinalIgnoreCase) || (ShortName(type) + "_C").Equals(name, StringComparison.OrdinalIgnoreCase))];
        return exact.Length > 0 ? exact : [.. LootContainerClasses.All.Where(type => ShortName(type).Contains(name, StringComparison.OrdinalIgnoreCase))];
    }

    private static string ShortName(LootContainerClass type)
    {
        string name = type.Container.Class[(type.Container.Class.LastIndexOf('.') + 1)..];
        return name.EndsWith("_C", StringComparison.Ordinal) ? name[..^2] : name;
    }

    private static string[] Usage() => ["Usage:", .. AdminApi.Usage];

    private AdminResult Run(AdminRequest request)
    {
        try
        {
            PlayerChannels[] players = [.. _players()];
            if (request.Name == AdminApi.Players)
            {
                return AdminResult.Success(players.Length == 0 ? ["No players connected."] : [.. players.Select(Describe)]);
            }

            PlayerChannels[] matches = [.. players.Where(player => long.TryParse(request.Player, NumberStyles.None, CultureInfo.InvariantCulture, out long id)
                ? player.UserId == id
                : string.Equals(player.DisplayName, request.Player, StringComparison.OrdinalIgnoreCase))];
            if (matches is not [{ } target])
            {
                return AdminResult.Failure(matches.Length == 0 ? StatusCodes.Status404NotFound : StatusCodes.Status409Conflict,
                    matches.Length == 0 ? AdminApi.PlayerNotFound : "player_ambiguous",
                    [matches.Length == 0 ? $"No connected player {request.Player}." : $"Several players are named {request.Player}; use the user id."]);
            }

            return request.Name switch
            {
                AdminApi.Teleport => Teleport(target, request.Position!.Value, request.YawDegrees),
                AdminApi.TeleportToLoot => TeleportToLoot(target, request),
                _ => SpawnLoot(target, request.Class!),
            };
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            return AdminResult.Failure(StatusCodes.Status500InternalServerError, "admin_command_failed", [error.Message]);
        }
    }

    private static AdminResult Teleport(PlayerChannels player, Vector3D position, double? yawDegrees)
    {
        AdminRefusal refusal = player.Teleport(position, yawDegrees * Math.PI / 180);
        return refusal == AdminRefusal.None
            ? AdminResult.Success([$"Teleporting {player.UserId} to {Text(position)}{(yawDegrees is { } yaw ? $" yaw {yaw.ToString("0.#", CultureInfo.InvariantCulture)}" : "")}."])
            : Refused(refusal);
    }

    private static AdminResult TeleportToLoot(PlayerChannels player, AdminRequest request)
    {
        IReadOnlyList<LootContainer> containers = player.LootContainers;
        int? index = request.Container ?? containers
            .Where(container => container.Source is not (LootContainerSource.Socket or LootContainerSource.Operator))
            .Skip(request.FreeStanding!.Value).Select(container => (int?)container.Index).FirstOrDefault();
        if (index is not { } found || found >= containers.Count)
        {
            return Refused(AdminRefusal.NoSuchContainer);
        }

        AdminRefusal refusal = player.TeleportToLoot(found);
        LootContainer chosen = containers[found];
        return refusal == AdminRefusal.None
            ? AdminResult.Success([$"Teleporting {player.UserId} to container {found} ({ShortName(chosen.Class)}, {chosen.Source}) at {Text(chosen.Location)}; now at {Text(player.CharacterPosition!.Value)}."])
            : Refused(refusal);
    }

    private static AdminResult SpawnLoot(PlayerChannels player, LootContainerClass type)
    {
        AdminRefusal refusal = player.SpawnLoot(type, out LootContainer? container);
        return refusal == AdminRefusal.None && container is not null
            ? AdminResult.Success([$"Spawned container {container.Index} ({ShortName(type)}) id 0x{container.Guid:x} at {Text(container.Location)} yaw {container.Rotation.Yaw.ToString("0.#", CultureInfo.InvariantCulture)}."])
            : Refused(refusal);
    }

    private static AdminResult Refused(AdminRefusal refusal) => refusal switch
    {
        AdminRefusal.NoSuchContainer => AdminResult.Failure(StatusCodes.Status404NotFound, "no_such_container", ["No container has that index."]),
        AdminRefusal.NotPossessed => AdminResult.Failure(StatusCodes.Status409Conflict, "player_not_possessed", ["The player's character is not possessed yet."]),
        AdminRefusal.Departed => AdminResult.Failure(StatusCodes.Status409Conflict, "player_departed", ["The player has left with an elevator."]),
        AdminRefusal.TeleportPending => AdminResult.Failure(StatusCodes.Status409Conflict, "teleport_pending", ["An earlier teleport has not reached the client yet."]),
        AdminRefusal.Busy => AdminResult.Failure(StatusCodes.Status409Conflict, "player_busy", ["The player is breaching, searching, looting or holding an elevator switch."]),
        AdminRefusal.OutOfRange => AdminResult.Failure(StatusCodes.Status400BadRequest, "position_out_of_range", ["The position is outside the map's range."]),
        _ => AdminResult.Failure(StatusCodes.Status409Conflict, "no_container_ids", ["The raid has no ids left for another container."]),
    };

    private static string Describe(PlayerChannels player)
    {
        string position = player.CharacterPosition is { } at ? Text(at) : "(no position yet)";
        string yaw = player.CharacterYaw is { } radians ? (radians * 180 / Math.PI).ToString("0.#", CultureInfo.InvariantCulture) : "-";
        return $"{player.UserId} {player.DisplayName ?? "-"} {player.Phase} at {position} yaw {yaw}{(player.TeleportPending ? " (teleport pending)" : "")}";
    }

    private static string Text(Vector3D position) =>
        string.Create(CultureInfo.InvariantCulture, $"({position.X:0}, {position.Y:0}, {position.Z:0})");
}
