using System.Collections.ObjectModel;
using Ares.Common.Game.Inventory;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Game.Content;

/// <summary>
/// ARES content: the backend data the game's menu reads from its backend and the installed game does not hold (the
/// stash and loadout structures, recycling yields, the trader offers and item quality levels), and the per-container
/// loot rules. It lives in one folder, the content folder (<c>content-dir</c>, which Host passes to every Raid it starts):
/// ARES ships it as <c>content/</c>, as close to the official game as known, and an operator edits it in place. Every
/// file must be there. Every file is JSON (the backend's wire format where the backend serves it). Host and Raid load it once at startup; it is independent of the game data, against
/// which <see cref="ItemContent"/> and <see cref="ItemDefinitions"/> resolve it.
/// </summary>
public sealed class AresContent
{
    /// <summary>The stash structure and its upgrade chain.</summary>
    public const string StashFile = "stash.json";

    /// <summary>The loadout structure and its carried places.</summary>
    public const string LoadoutFile = "loadout.json";

    /// <summary>What recycling each item gives.</summary>
    public const string RecyclingFile = "item-recycling.json";

    /// <summary>The trader offers.</summary>
    public const string TraderOffersFile = "trader-offers.json";

    /// <summary>The quality level of item menu descriptions, by technical name.</summary>
    public const string QualityLevelsFile = "item-quality-levels.json";

    /// <summary>Loot rules: spawn chances and contents of chosen containers.</summary>
    public const string LootRulesFile = "loot-rules.json";

    private readonly LootRulesDocument _lootRules;

    private AresContent(string directory)
    {
        Directory = directory;
        StashDocument stash = Read<StashDocument>(StashFile);
        LoadoutDocument loadout = Read<LoadoutDocument>(LoadoutFile);
        IReadOnlyList<RecyclingRow> recycling = Read<IReadOnlyList<RecyclingRow>>(RecyclingFile);
        TraderOffers = Read<IReadOnlyList<OfferDescription>>(TraderOffersFile);
        QualityLevels = Read<IReadOnlyDictionary<string, long>>(QualityLevelsFile);
        _lootRules = Read<LootRulesDocument>(LootRulesFile);
        Stash = new StashContent(stash, problem => Invalid(StashFile, problem));
        Loadout = new LoadoutContent(loadout, problem => Invalid(LoadoutFile, problem));
        if (Stash.Definitions.Concat(Loadout.Definitions).GroupBy(definition => definition.GameAssetId).FirstOrDefault(group => group.Count() > 1) is { } shared)
        {
            throw Invalid(LoadoutFile, $"defines {shared.Key}, which {StashFile} defines too");
        }

        RecycleOutputs = Unique(recycling.Select(row => (row.GameAssetId, row.Outputs)), RecyclingFile, "recycles");
        if (recycling.FirstOrDefault(row => row.Outputs.Count == 0 || row.Outputs.Any(output => output.Amount <= 0)) is { } empty)
        {
            throw Invalid(RecyclingFile, $"item {empty.GameAssetId} must give at least one item, each a positive amount");
        }

        _ = Unique(TraderOffers.Select(offer => (offer.OfferId, offer)), TraderOffersFile, "lists offer");
        if (QualityLevels.FirstOrDefault(pair => pair.Value < 0) is { Key: { } negative })
        {
            throw Invalid(QualityLevelsFile, $"quality level of {negative} must be zero or more");
        }

        return;

        T Read<T>(string file) => AresContentReader.Read<T>(directory, file);

        AresContentException Invalid(string file, string problem) => AresContentReader.Invalid(directory, file, problem);

        ReadOnlyDictionary<long, TValue> Unique<TValue>(IEnumerable<(long Key, TValue Value)> rows, string file, string what)
        {
            Dictionary<long, TValue> unique = [];
            foreach ((long key, TValue value) in rows)
            {
                if (!unique.TryAdd(key, value))
                {
                    throw Invalid(file, $"{what} {key} twice");
                }
            }

            return unique.AsReadOnly();
        }
    }

    /// <summary>Every content file, by name.</summary>
    public static IReadOnlyList<string> FileNames { get; } = [StashFile, LoadoutFile, RecyclingFile, TraderOffersFile, QualityLevelsFile, LootRulesFile];

    /// <summary>Full path of the content folder it was loaded from.</summary>
    public string Directory { get; }

    /// <summary>The stash structure.</summary>
    public StashContent Stash { get; }

    /// <summary>The loadout structure.</summary>
    public LoadoutContent Loadout { get; }

    /// <summary>The trader offers, in serving order.</summary>
    public IReadOnlyList<OfferDescription> TraderOffers { get; }

    /// <summary>Items one recycled item gives, in package order, by item asset id; items absent here cannot be recycled.</summary>
    public IReadOnlyDictionary<long, IReadOnlyList<ItemAssetAmount>> RecycleOutputs { get; }

    /// <summary>Quality level of an item's menu description, by technical name; items absent here have quality 0.</summary>
    public IReadOnlyDictionary<string, long> QualityLevels { get; }

    /// <summary>Loads the content of a content folder, which must hold every content file.</summary>
    /// <param name="directory">The content folder.</param>
    /// <exception cref="AresContentException">
    /// No folder is given, the folder or one of its files is missing, it holds a JSON file that is not a content file,
    /// or a file is malformed or inconsistent.
    /// </exception>
    public static AresContent Load(string? directory)
    {
        if (directory is null)
        {
            throw new AresContentException("No ARES content folder is set (content-dir).");
        }

        string full = Path.GetFullPath(directory);
        if (!System.IO.Directory.Exists(full))
        {
            throw new AresContentException($"The content folder {full} (content-dir) does not exist.");
        }

        if (System.IO.Directory.EnumerateFiles(full, "*.json").Select(Path.GetFileName).FirstOrDefault(name => !FileNames.Contains(name, StringComparer.Ordinal)) is { } unknown)
        {
            throw new AresContentException($"The content folder {full} holds {unknown}, which is not a content file. Content files: {string.Join(", ", FileNames)}.");
        }

        return new AresContent(full);
    }

    /// <summary>The backend item content of the shared item rules, with the quality levels resolved against the game data.</summary>
    /// <param name="data">The game data.</param>
    /// <exception cref="AresContentException">A quality level names an item the game data does not have.</exception>
    public BackendItemContent ItemContent(GameDataSet data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Dictionary<long, long> qualityLevels = [];
        foreach ((string name, long level) in QualityLevels)
        {
            qualityLevels[data.Items.ByTechnicalName.TryGetValue(name, out ItemDefinition? item) ? item.AssetId
                : throw AresContentReader.Invalid(Directory, QualityLevelsFile, $"the game data has no item {name}")] = level;
        }

        return new BackendItemContent(Stash.Definitions, Loadout.Definitions, Loadout.FrameSlotAssetId, Loadout.StandardFrameAssetId, RecycleOutputs,
            qualityLevels.AsReadOnly());
    }

    /// <summary>The loot rules, checked against the game data.</summary>
    /// <param name="data">The game data: container tags and the maps' container ids.</param>
    /// <param name="items">The item definitions: the items a world container can hold.</param>
    /// <exception cref="AresContentException">A rule is malformed or names something the game data does not have.</exception>
    public LootRules LootRules(GameDataSet data, LoadoutDefinitions items) =>
        LootRulesContent.Resolve(_lootRules, data, items, problem => AresContentReader.Invalid(Directory, LootRulesFile, problem));

    /// <summary>The shared item definitions of the game data and this content.</summary>
    /// <param name="data">The game data.</param>
    /// <exception cref="AresContentException">The content does not fit the game data.</exception>
    public LoadoutDefinitions ItemDefinitions(GameDataSet data)
    {
        BackendItemContent content = ItemContent(data);
        try
        {
            return new LoadoutDefinitions(data, content);
        }
        catch (Exception error) when (error is KeyNotFoundException or ArgumentException or InvalidDataException or InvalidCastException)
        {
            throw new AresContentException($"The ARES content does not fit the game data: {error.Message}", error);
        }
    }
}
