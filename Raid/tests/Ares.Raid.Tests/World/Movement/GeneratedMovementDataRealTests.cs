using System.Reflection;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Raid.World.Movement;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

/// <summary>
/// Real-data tier: the transitional constants of <see cref="GeneratedGraphStateFlags"/> and
/// <see cref="GeneratedMovementData"/> equal what the loaded game data gives, so that the raid switches to the loaded data
/// without a change of behaviour.
/// </summary>
public sealed class GeneratedMovementDataRealTests
{
    private static GameDataSet Data => RealGameData.Data;

    [RealDataFact]
    public void Every_generated_flag_group_equals_the_resolved_one()
    {
        PropertyInfo[] groups = typeof(GraphStateFlags).GetProperties();
        FieldInfo[] constants = [.. typeof(GeneratedGraphStateFlags).GetFields(BindingFlags.Public | BindingFlags.Static)];

        Assert.Equal(groups.Select(group => group.Name).Order(StringComparer.Ordinal), constants.Select(field => field.Name).Order(StringComparer.Ordinal));
        Assert.All(constants, field => Assert.Equal((ulong)field.GetRawConstantValue()!, (ulong)typeof(GraphStateFlags).GetProperty(field.Name)!.GetValue(Data.Names.GraphFlags)!));
    }

    [RealDataFact]
    public void The_generated_dodge_rolls_and_vaults_equal_the_loaded_ones()
    {
        Assert.All(Enum.GetValues<DodgeRollKind>(), kind => Assert.Equivalent(GeneratedMovementData.DodgeRoll(kind), Data.Movement.DodgeRoll(kind), strict: true));
        Assert.All(new[] { (false, false), (true, false), (false, true), (true, true) }, condition =>
            Assert.Equivalent(GeneratedMovementData.Vaults(condition.Item1, condition.Item2), Data.Movement.Vaults(condition.Item1, condition.Item2), strict: true));
    }
}
