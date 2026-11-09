using System.Diagnostics;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ares.Host.Tests;

/// <summary>
/// Creates and drops a uniquely named MongoDB database for each test. Names carry the test
/// process id; databases left by test processes that no longer run are dropped once per run.
/// </summary>
internal sealed class TestDatabase : IDisposable
{
    private const string Prefix = "ares_test_";
    internal static string ConnectionUri { get; } = Environment.GetEnvironmentVariable("ARES_TEST_MONGODB_URI_FILE") is { } file
        ? File.ReadAllText(file).Trim()
        : Environment.GetEnvironmentVariable("ARES_TEST_MONGODB_URI") ?? "mongodb://127.0.0.1:27017/?directConnection=true";
    private static readonly MongoClient s_client = new(ConnectionUri);
    private static readonly Lazy<bool> s_staleDropped = new(DropStaleDatabases);
    private HostDatabase? _store;

    /// <summary>User id of the account every test database starts with.</summary>
    internal const long UserId = 1000000000000000000;

    /// <summary>Name of that account.</summary>
    internal const string AccountName = "tester";

    internal string Name { get; } = $"{Prefix}{Environment.ProcessId}_{Guid.NewGuid():N}";

    internal TestDatabase()
    {
        _ = s_staleDropped.Value;
    }
    internal string Directory { get; } = Path.Combine(Path.GetTempPath(), "ares-mongodb-test-" + Guid.NewGuid().ToString("N"));
    internal IMongoDatabase Native => s_client.GetDatabase(Name);
    internal IMongoCollection<BsonDocument> Accounts => Native.GetCollection<BsonDocument>("accounts");
    internal BsonDocument Account => Accounts.Find(FilterDefinition<BsonDocument>.Empty).Single();
    internal string InventoryJson => Account["inventory"].ToJson(new MongoDB.Bson.IO.JsonWriterSettings
    {
        OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson,
    });

    internal HostDatabase Store
    {
        get
        {
            if (_store is not null)
            {
                return _store;
            }

            _store = new HostDatabase(ConnectionUri, Name);
            _store.InsertAccount(UserId, new AccountRecord(AccountName, AccountName, 1000000000000000256, 0, Banned: false,
                new SrpVerifierRecord("AA==", "AA==", 1), LaunchKey: null));
            return _store;
        }
    }

    internal void ReplaceInventory(string json)
    {
        Accounts.UpdateOne(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set("inventory", BsonDocument.Parse(json)));
    }

    // A killed or crashed test run cannot drop its databases; each one adds open files to MongoDB.
    private static bool DropStaleDatabases()
    {
        List<string> stale = [.. s_client.ListDatabaseNames().ToList()
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && !IsRunning(name[Prefix.Length..].Split('_')[0]))];
        foreach (string name in stale)
        {
            s_client.DropDatabase(name);
        }

        return true;
    }

    private static bool IsRunning(string processId)
    {
        if (!int.TryParse(processId, out int id))
        {
            return false;
        }

        try
        {
            using Process process = Process.GetProcessById(id);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _store?.Dispose();
        s_client.DropDatabase(Name);
        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
