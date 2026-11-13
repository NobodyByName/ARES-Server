using Ares.Common.Logging;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Identity;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Matchmaking;
using Ares.Host.Services.Persistence;
using Ares.Host.Services.Social;

namespace Ares.Host.Services.Players;

/// <summary>One account's settings, inventory, party and matchmaking.</summary>
public sealed class PlayerServices : IDisposable
{
    internal PlayerServices(Account account, HostDatabase database, RaidFleet? raids, JsonEventLog log, TimeProvider time)
    {
        Account = account;
        Settings = new AccountStore(database, account.UserId);
        Inventory = new InventoryStore(database, account.UserId);
        Inventory.InitializeLayout(time.GetUtcNow());
        Profiles = new ProfileFactory(() => Account, time);
        Gate = new BackendGate();
        Party = new PartyService(Gate, Profiles, log, time);
        Matchmaking = new MatchmakingService(Gate, Party, Profiles, raids, log, Inventory, account.UserId, time);
    }

    /// <summary>The account, as last loaded or changed on this Host.</summary>
    public Account Account { get; internal set; }

    /// <summary>Persistent game settings.</summary>
    public AccountStore Settings { get; }

    /// <summary>Owned items and loadout.</summary>
    public InventoryStore Inventory { get; }

    /// <summary>Profile builder.</summary>
    public ProfileFactory Profiles { get; }

    /// <summary>The player's party and notifications.</summary>
    public PartyService Party { get; }

    /// <summary>The player's matchmaking ticket and raid assignment.</summary>
    public MatchmakingService Matchmaking { get; }

    private BackendGate Gate { get; }

    /// <inheritdoc/>
    public void Dispose() => Gate.Dispose();
}

/// <summary>
/// The players this Host has loaded, keyed by user id. A player is loaded on its first signed-in
/// request, or at startup while it holds a raid reservation, and stays loaded until its account is deleted.
/// </summary>
public sealed class PlayerRegistry(HostDatabase database, AccountDirectory accounts, RaidFleet? raids, JsonEventLog log, TimeProvider time) : IDisposable
{
    private readonly Dictionary<long, PlayerServices> _players = [];
    private readonly Lock _lock = new();

    /// <summary>True when Host starts Raid servers.</summary>
    public bool HasAllocator => raids is not null;

    /// <summary>Every loaded player.</summary>
    public IReadOnlyList<PlayerServices> Loaded
    {
        get
        {
            lock (_lock)
            {
                return [.. _players.Values];
            }
        }
    }

    /// <summary>The signed-in player with <paramref name="userId"/>; null when the account is missing or banned.</summary>
    public PlayerServices? SignedIn(long userId)
    {
        PlayerServices? player = Find(userId);
        return player is { Account.Record.Banned: false } ? player : null;
    }

    /// <summary>Loads every player that holds a raid reservation, so their locks are reconciled.</summary>
    public void LoadReservationOwners()
    {
        foreach (long userId in database.ReservationOwners())
        {
            _ = Find(userId);
        }
    }

    /// <summary>Applies an account change made on this Host to a loaded player.</summary>
    public void Update(Account account)
    {
        lock (_lock)
        {
            if (_players.TryGetValue(account.UserId, out PlayerServices? player))
            {
                player.Account = account;
            }
        }
    }

    /// <summary>Unloads a deleted account.</summary>
    public void Forget(long userId)
    {
        lock (_lock)
        {
            if (_players.Remove(userId, out PlayerServices? player))
            {
                player.Dispose();
            }
        }
    }

    /// <summary>Players in live raid assignments, and how many assignments are live.</summary>
    public (int Raids, int Players) CurrentRaids
    {
        get
        {
            int active = 0;
            int players = 0;
            foreach (PlayerServices player in Loaded)
            {
                (bool live, int count) = player.Matchmaking.CurrentRaid;
                active += live ? 1 : 0;
                players += count;
            }

            return (active, players);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            foreach (PlayerServices player in _players.Values)
            {
                player.Dispose();
            }

            _players.Clear();
        }
    }

    /// <summary>The player with <paramref name="userId"/>, loading it when needed; null when the account is missing.</summary>
    public PlayerServices? Find(long userId)
    {
        lock (_lock)
        {
            if (_players.TryGetValue(userId, out PlayerServices? loaded))
            {
                return loaded;
            }

            if (accounts.Find(userId) is not { } account)
            {
                return null;
            }

            PlayerServices player = new(account, database, raids, log, time);
            _players.Add(userId, player);
            return player;
        }
    }
}
