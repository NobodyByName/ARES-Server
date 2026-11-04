using Ares.Host.Services.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ares.Host.Tests;

/// <summary>Creates and drops a uniquely named MongoDB database for each test.</summary>
internal sealed class TestDatabase : IDisposable
{
    internal static string ConnectionUri { get; } = Environment.GetEnvironmentVariable("ARES_TEST_MONGODB_URI_FILE") is { } file
        ? File.ReadAllText(file).Trim()
        : Environment.GetEnvironmentVariable("ARES_TEST_MONGODB_URI") ?? "mongodb://127.0.0.1:27017/?directConnection=true";
    private static readonly MongoClient s_client = new(ConnectionUri);
    private HostDatabase? _store;

    internal string Name { get; } = "ares_test_" + Guid.NewGuid().ToString("N");
    internal string Directory { get; } = Path.Combine(Path.GetTempPath(), "ares-mongodb-test-" + Guid.NewGuid().ToString("N"));
    internal IMongoDatabase Native => s_client.GetDatabase(Name);
    internal IMongoCollection<BsonDocument> Players => Native.GetCollection<BsonDocument>("players");
    internal BsonDocument Player => Players.Find(FilterDefinition<BsonDocument>.Empty).Single();
    internal string InventoryJson => Player["inventory"].ToJson(new MongoDB.Bson.IO.JsonWriterSettings
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
            _store.Initialize();
            return _store;
        }
    }

    internal void ReplaceInventory(string json)
    {
        Players.UpdateOne(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set("inventory", BsonDocument.Parse(json)));
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
