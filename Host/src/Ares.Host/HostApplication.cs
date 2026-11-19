using Ares.Common.Game.Content;
using Ares.Common.GameData;
using Ares.Common.Logging;
using Ares.Host.Services;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Identity;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Matchmaking;
using Ares.Host.Services.Persistence;
using Ares.Host.Services.Players;
using Ares.Host.Services.Routing;

namespace Ares.Host;

/// <summary>Composes the Host services and web server.</summary>
public static class HostApplication
{
    /// <summary>Builds the Host web application for <paramref name="configuration"/>.</summary>
    /// <param name="configuration">Validated configuration.</param>
    /// <param name="log">Structured event output.</param>
    /// <param name="gameData">
    /// The game data Host loaded at startup. The inventory catalogue is built from it and the configuration's content
    /// (<see cref="HostConfiguration.Content"/>); the three are registered as services.
    /// </param>
    /// <param name="time">Clock; the system clock by default.</param>
    /// <param name="raidStarter">Optional Raid server starter (tests); by default Host starts the configured ARES-Raid.</param>
    /// <param name="configureBuilder">Optional builder customisation (tests use an in-memory server).</param>
    /// <param name="loggerProvider">Optional framework log destination.</param>
    /// <exception cref="AresContentException">The content does not fit the game data or the stash upgrade prices.</exception>
    public static WebApplication Build(HostConfiguration configuration, JsonEventLog log, GameDataSet gameData, TimeProvider? time = null,
        IRaidStarter? raidStarter = null, Action<WebApplicationBuilder>? configureBuilder = null, ILoggerProvider? loggerProvider = null)
    {
        ArgumentNullException.ThrowIfNull(gameData);
        time ??= TimeProvider.System;
        HostSettings settings = configuration.Settings;
        InventoryCatalog catalog = new(gameData, configuration.Content, settings.StashUpgrades);

        // Every Raid reads the loot content; checking it here stops a bad file before the first raid.
        _ = configuration.Content.LootSpawn(gameData);
        _ = configuration.Content.LootContents(gameData);
        _ = configuration.Content.LootRules(gameData, catalog.Items);

        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [] });
        builder.Logging.ClearProviders();
        if (loggerProvider is null)
        {
            builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        }
        else
        {
            builder.Logging.AddProvider(loggerProvider);
        }

        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = RequestDispatcher.MaximumBodyBytes + 1;
            options.Listen(configuration.Bind, configuration.Port);
        });
        configureBuilder?.Invoke(builder);

        HostDatabase database = new(settings.MongoDbUri, settings.MongoDbDatabase);
        raidStarter ??= configuration.Raids is { } launch ? new RaidProcesses(launch, log) : null;
        RaidFleet? raids = raidStarter is null ? null : new RaidFleet(raidStarter, settings.RaidCapacity, configuration.Raids?.Players ?? 1, time);
        AccountDirectory accounts = new(database, time);
        PlayerRegistry players = new(database, accounts, catalog, raids, log, time);
        try
        {
            database.Initialize();
            players.LoadReservationOwners();
        }
        catch
        {
            players.Dispose();
            raids?.Dispose();
            database.Dispose();
            throw;
        }

        builder.Services.AddSingleton<HostDatabase>(_ => database);
        builder.Services.AddSingleton(gameData);
        builder.Services.AddSingleton(configuration.Content);
        builder.Services.AddSingleton(catalog);
        builder.Services.AddSingleton(catalog.Items);

        builder.Services.AddSingleton(accounts);
        builder.Services.AddSingleton(players);
        if (raids is not null)
        {
            builder.Services.AddSingleton(raids);
        }

        builder.Services.AddHostedService(_ => new InventoryReservationRecovery(players, time));
        IdentityService identity = new(settings, accounts, time);
        BackendRoutes routes = new(settings, identity, players, new SignInService(accounts, log, time), log, time);
        RequestDispatcher dispatcher = new(settings, routes, identity, players, log, time);

        WebApplication app = builder.Build();
        _ = app.Services.GetRequiredService<HostDatabase>();
        app.Lifetime.ApplicationStopped.Register(() =>
        {
            raids?.Dispose();
            players.Dispose();
        });
        app.Run(dispatcher.HandleAsync);
        return app;
    }
}
