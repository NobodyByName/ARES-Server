using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json.Serialization;
using Ares.Common.Game.Enemies;
using Ares.Common.GameData;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Enemies;

namespace Ares.Common.Game.Content;

/// <summary><c>arc-spawners.json</c>: the ARC spawners of each map, by world name.</summary>
internal sealed record ArcSpawnersDocument(
    [property: JsonPropertyName("maps")] IReadOnlyDictionary<string, IReadOnlyList<ArcSpawnerDocument>> Maps);

/// <summary>One ARC spawner of <c>arc-spawners.json</c> (see <see cref="ArcSpawner"/>).</summary>
internal sealed record ArcSpawnerDocument(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("class")] string Class,
    [property: JsonPropertyName("location")] IReadOnlyList<double> Location,
    [property: JsonPropertyName("yaw")] double Yaw,
    [property: JsonPropertyName("enemyTable")] string EnemyTable,
    [property: JsonPropertyName("enemyTypes")] IReadOnlyList<string> EnemyTypes,
    [property: JsonPropertyName("routes")] IReadOnlyList<string?> Routes,
    [property: JsonPropertyName("groupSearchRadius")] double GroupSearchRadius,
    [property: JsonPropertyName("spawnAreaRadius")] double SpawnAreaRadius);

/// <summary>One ARC's idle movement in <c>arc-movement.json</c> (see <see cref="ArcMovementProfile"/>).</summary>
internal sealed record ArcMovementDocument(
    [property: JsonPropertyName("cruiseSpeed")] double CruiseSpeed,
    [property: JsonPropertyName("acceleration")] double Acceleration,
    [property: JsonPropertyName("deceleration")] double Deceleration,
    [property: JsonPropertyName("velocityBlend")] double VelocityBlend,
    [property: JsonPropertyName("turnRate")] double TurnRate,
    [property: JsonPropertyName("pitchPerSpeed")] double PitchPerSpeed,
    [property: JsonPropertyName("bankPerTurnRate")] double BankPerTurnRate,
    [property: JsonPropertyName("maxBank")] double MaxBank,
    [property: JsonPropertyName("hoverHeight")] double HoverHeight,
    [property: JsonPropertyName("bodyRadius")] double BodyRadius,
    [property: JsonPropertyName("homePointSpacing")] double HomePointSpacing,
    [property: JsonPropertyName("minHomePointSpacing")] double MinHomePointSpacing,
    [property: JsonPropertyName("replanSeconds")] double ReplanSeconds,
    [property: JsonPropertyName("probeDistance")] double ProbeDistance,
    [property: JsonPropertyName("probeAngles")] IReadOnlyList<double> ProbeAngles,
    [property: JsonPropertyName("wanderShare")] double WanderShare,
    [property: JsonPropertyName("minDwellSeconds")] double MinDwellSeconds,
    [property: JsonPropertyName("maxDwellSeconds")] double MaxDwellSeconds,
    [property: JsonPropertyName("turnBackChance")] double TurnBackChance,
    [property: JsonPropertyName("arriveDistance")] double ArriveDistance);

/// <summary>
/// Turns <c>arc-spawners.json</c> and <c>arc-movement.json</c> into spawners and movement profiles, refusing anything the
/// game data does not have: maps and routes must exist, and every ARC the servers spawn needs a profile.
/// </summary>
internal static class ArcContent
{
    /// <summary>Checks the spawners against the game data's maps and their spline paths.</summary>
    /// <param name="document">The file.</param>
    /// <param name="data">The game data.</param>
    /// <param name="invalid">Makes the error for a problem.</param>
    /// <returns>The spawners of each map, by world name.</returns>
    public static FrozenDictionary<string, IReadOnlyList<ArcSpawner>> Spawners(ArcSpawnersDocument document, GameDataSet data, Func<string, AresContentException> invalid)
    {
        Dictionary<string, IReadOnlyList<ArcSpawner>> maps = new(StringComparer.Ordinal);
        foreach ((string world, IReadOnlyList<ArcSpawnerDocument> rows) in document.Maps)
        {
            MapData map = data.Maps.FirstOrDefault(item => item.WorldName.Equals(world, StringComparison.Ordinal))
                ?? throw invalid($"the game data has no map {world}");
            HashSet<string> routes = [.. map.SplinePaths.Select(path => path.Name)];
            HashSet<string> ids = new(StringComparer.Ordinal);
            List<ArcSpawner> spawners = [];
            foreach ((ArcSpawnerDocument? entry, int position) in rows.Select((row, index) => ((ArcSpawnerDocument?)row, index + 1)))
            {
                // The reader keeps null list entries, so each list is checked for them.
                ArcSpawnerDocument row = entry ?? throw invalid(string.Create(CultureInfo.InvariantCulture, $"{world} spawner #{position} is null"));
                string where = $"{world} spawner {row.Id}";
                if (string.IsNullOrWhiteSpace(row.Id) || !ids.Add(row.Id))
                {
                    throw invalid(string.Create(CultureInfo.InvariantCulture, $"{world} spawner #{position} needs an id of its own"));
                }

                if (row.Location is not [var x, var y, var z] || !double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z) || !double.IsFinite(row.Yaw))
                {
                    throw invalid($"{where} needs a location of three finite numbers and a finite yaw");
                }

                if (row.EnemyTypes.Count == 0 || row.EnemyTypes.Any(string.IsNullOrWhiteSpace))
                {
                    throw invalid($"{where} needs at least one enemy type, none empty");
                }

                string[] named = [.. row.Routes.OfType<string>()];
                if (named.Length != row.Routes.Count)
                {
                    throw invalid($"{where} has a null route");
                }

                if (named.FirstOrDefault(route => !routes.Contains(route)) is { } missing)
                {
                    throw invalid($"{where} keeps to route {missing}, which {world} does not have");
                }

                if (row.GroupSearchRadius <= 0 || !double.IsFinite(row.GroupSearchRadius) || row.SpawnAreaRadius < 0 || !double.IsFinite(row.SpawnAreaRadius))
                {
                    throw invalid($"{where} needs a positive groupSearchRadius and a spawnAreaRadius of zero or more");
                }

                spawners.Add(new ArcSpawner(row.Id, row.Label, row.Class, new Vector3D(x, y, z), row.Yaw, row.EnemyTable, [.. row.EnemyTypes], named,
                    row.GroupSearchRadius, row.SpawnAreaRadius));
            }

            maps[world] = spawners;
        }

        return maps.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>Checks the movement profiles: one for each ARC the servers spawn, every value in its range.</summary>
    /// <param name="document">The file: profiles by ARC name.</param>
    /// <param name="invalid">Makes the error for a problem.</param>
    /// <returns>The profiles, by ARC.</returns>
    public static FrozenDictionary<ArcClass, ArcMovementProfile> Movement(IReadOnlyDictionary<string, ArcMovementDocument> document, Func<string, AresContentException> invalid)
    {
        Dictionary<ArcClass, ArcMovementProfile> profiles = [];
        foreach ((string name, ArcMovementDocument? entry) in document)
        {
            ArcClass arc = Arcs.Find(name) ?? throw invalid($"names {name}, which is not an ARC the servers spawn");
            ArcMovementDocument row = entry ?? throw invalid($"{name} is null");
            double[] positive = [row.CruiseSpeed, row.Acceleration, row.Deceleration, row.TurnRate, row.BodyRadius, row.HomePointSpacing, row.MinHomePointSpacing, row.ReplanSeconds,
                row.ProbeDistance, row.ArriveDistance];
            double[] shares = [row.VelocityBlend, row.WanderShare, row.TurnBackChance];
            if (positive.Any(value => value <= 0 || !double.IsFinite(value)) || shares.Any(value => value is not (>= 0 and <= 1))
                || row.PitchPerSpeed < 0 || !double.IsFinite(row.PitchPerSpeed) || row.HoverHeight < 0 || !double.IsFinite(row.HoverHeight)
                || row.BankPerTurnRate < 0 || !double.IsFinite(row.BankPerTurnRate) || row.MaxBank is not (>= 0 and < 90)
                || row.MinDwellSeconds < 0 || !double.IsFinite(row.MinDwellSeconds) || row.MaxDwellSeconds < row.MinDwellSeconds || !double.IsFinite(row.MaxDwellSeconds))
            {
                throw invalid($"{name}: speeds, distances, radii and times must be positive (pitch, bank, hover height and dwell zero or more, maxBank below 90, "
                    + "maxDwellSeconds at least minDwellSeconds), velocityBlend, wanderShare and turnBackChance from 0 to 1");
            }

            if (row.ProbeAngles.Count == 0 || row.ProbeAngles.Any(angle => !double.IsFinite(angle)))
            {
                throw invalid($"{name} needs at least one finite probe angle");
            }

            profiles[arc] = new ArcMovementProfile(row.CruiseSpeed, row.Acceleration, row.Deceleration, row.VelocityBlend, row.TurnRate, row.PitchPerSpeed, row.BankPerTurnRate,
                row.MaxBank, row.HoverHeight,
                row.BodyRadius, row.HomePointSpacing, row.MinHomePointSpacing, row.ReplanSeconds, row.ProbeDistance, [.. row.ProbeAngles], row.WanderShare,
                row.MinDwellSeconds, row.MaxDwellSeconds, row.TurnBackChance, row.ArriveDistance);
        }

        if (Arcs.All.FirstOrDefault(arc => !profiles.ContainsKey(arc)) is { } unprofiled)
        {
            throw invalid($"has no profile for {unprofiled.Name}");
        }

        return profiles.ToFrozenDictionary();
    }
}
