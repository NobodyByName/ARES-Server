using Ares.Common.Control;
using Ares.Common.Logging;
using Ares.Host.Services;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Identity;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Matchmaking;
using Ares.Host.Services.Persistence;
using Ares.Host.Services.Routing;
using Ares.Host.Services.Social;

namespace Ares.Host;

/// <summary>Composes the Host services and web server.</summary>
public static class HostApplication
{
    /// <summary>Builds the Host web application for <paramref name="configuration"/>.</summary>
    /// <param name="configuration">Validated configuration.</param>
    /// <param name="log">Structured event output.</param>
    /// <param name="time">Clock; the system clock by default.</param>
    /// <param name="allocatorHandler">Optional HTTP handler for the Raid allocation client (tests).</param>
    /// <param name="configureBuilder">Optional builder customisation (tests use an in-memory server).</param>
    /// <param name="loggerProvider">Optional framework log destination.</param>
    public static WebApplication Build(HostConfiguration configuration, JsonEventLog log, TimeProvider? time = null,
        HttpMessageHandler? allocatorHandler = null, Action<WebApplicationBuilder>? configureBuilder = null, ILoggerProvider? loggerProvider = null)
    {
        time ??= TimeProvider.System;
        HostSettings settings = configuration.Settings;

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
        AccountStore accounts;
        InventoryStore inventory;
        try
        {
            database.Initialize();
            accounts = new AccountStore(settings.DataDirectory, time);
            inventory = new InventoryStore(database);
            inventory.InitializeLayout(time.GetUtcNow());
        }
        catch
        {
            database.Dispose();
            throw;
        }

        SessionAllocatorClient? allocator = configuration.SessionControlUrl is null
            ? null
            : new SessionAllocatorClient(configuration.SessionControlUrl, configuration.OperatorToken!, allocatorHandler);
        builder.Services.AddSingleton<HostDatabase>(_ => database);
        builder.Services.AddSingleton(inventory);
        ProfileFactory profiles = new(accounts, time);
        IdentityService identity = new(settings, profiles, time, inventory);
        BackendGate gate = new();
        PartyService party = new(gate, profiles, log, time);
        MatchmakingService matchmaking = new(gate, party, profiles, allocator, log, inventory);
        BackendRoutes routes = new(identity, profiles, accounts, party, matchmaking, inventory, time);
        RequestDispatcher dispatcher = new(settings, routes, matchmaking, log, time);

        WebApplication app = builder.Build();
        _ = app.Services.GetRequiredService<HostDatabase>();
        app.Lifetime.ApplicationStopped.Register(() =>
        {
            allocator?.Dispose();
            gate.Dispose();
        });
        app.Run(dispatcher.HandleAsync);
        return app;
    }
}
