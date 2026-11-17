using Ares.GameDataExtract.Animations;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Game.Movement;

namespace Ares.GameDataExtract.Movement;

/// <summary>Reads the sprint burst alpha curve (<see cref="MovementAssets.SprintBurstCurvePackage"/>): weighted cubic keys in increasing time.</summary>
internal static class SprintCurveReader
{
    /// <summary>The curve's keys.</summary>
    /// <exception cref="InvalidDataException">The curve is not a weighted cubic curve with increasing key times.</exception>
    public static IReadOnlyList<WeightedCurveKey> Read(PackageCache packages)
    {
        CookedPackage package = MovementPackages.Load(packages, MovementAssets.SprintBurstCurvePackage);
        IReadOnlyList<CurveKey> keys = CurveKeyReader.Read(package, MovementPackages.OnlyExport(package));
        if (keys.Zip(keys.Skip(1)).Any(pair => pair.First.Time >= pair.Second.Time))
        {
            throw new InvalidDataException($"{package.Name}: the curve keys are not in increasing time");
        }

        return
        [
            .. keys.Select(key => new WeightedCurveKey(SingleRounding.ToSingle(key.Time), SingleRounding.ToSingle(key.Value), SingleRounding.ToSingle(key.ArriveTangent),
                SingleRounding.ToSingle(key.ArriveWeight), SingleRounding.ToSingle(key.LeaveTangent), SingleRounding.ToSingle(key.LeaveWeight))),
        ];
    }
}
