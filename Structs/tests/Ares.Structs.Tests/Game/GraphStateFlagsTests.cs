using Ares.Structs.Game;
using Ares.Structs.Game.Movement;

namespace Ares.Structs.Tests.Game;

/// <summary>The graph tag groups follow a made-up tag list, and a tag outside its group's tag word is refused.</summary>
public sealed class GraphStateFlagsTests
{
    private static readonly string[] s_upper =
    [
        GameNames.GraphTags.AbilityBlockEquipItemInteract, GameNames.GraphTags.AbilityBlockFirePrimaryInteract, GameNames.GraphTags.AbilityBlockFireSecondaryInteract,
        GameNames.GraphTags.AbilityBlockJumpInteract, GameNames.GraphTags.AbilityBlockReloadInteract, GameNames.GraphTags.StatusImmobilizedInteract,
        GameNames.GraphTags.UiSkillStateActiveSprintBoost, GameNames.GraphTags.StatusExhausted,
    ];

    [Fact]
    public void Groups_take_the_bits_of_the_tag_list()
    {
        string[] lower = [.. Names().Except(s_upper)];
        GraphTagBits tags = Bits([.. lower, .. Enumerable.Range(0, MovementAssets.GraphTagWordBits - lower.Length).Select(index => $"Filler{index}"), .. s_upper]);
        GraphStateFlags flags = new(tags);

        Assert.Equal(new GraphTagBit(GameNames.GraphTags.AbilityCrouch, Array.IndexOf(lower, GameNames.GraphTags.AbilityCrouch)), tags.AbilityCrouch);
        Assert.Equal(tags.AbilityLandingHeavy.Mask, flags.SnapHookCancelling);
        Assert.Equal(tags.AbilityBlockFirePrimaryPendulumMove.Mask | tags.AbilityBlockFireSecondaryPendulumMove.Mask | tags.AbilityBlockEquipItemPendulumMove.Mask
            | tags.AbilityBlockReloadPendulumMove.Mask, flags.BlockedActions);
        Assert.Equal(flags.BlockedActions | tags.AbilityBlockContextInteractPendulumMove.Mask | tags.AnimationPendulumDisplaySyncedPose.Mask
            | tags.AbilityDodgeRollActive.Mask, flags.DodgeRolling);
        Assert.Equal(tags.UiSkillStateActiveSprintBoost.Mask | tags.StatusExhausted.Mask, flags.SimulatedUpper);
        Assert.Equal(1, tags.StatusExhausted.Word);
        Assert.Equal(1UL << (s_upper.Length - 1), tags.StatusExhausted.Mask);
    }

    [Fact]
    public void A_tag_outside_its_groups_word_is_refused()
    {
        ArgumentException refused = Assert.Throws<ArgumentException>(() => new GraphStateFlags(Bits([.. Names()])));

        Assert.Contains(GameNames.GraphTags.UiSkillStateActiveSprintBoost, refused.Message, StringComparison.Ordinal);
    }

    private static IEnumerable<string> Names() => typeof(GameNames.GraphTags).GetFields().Select(field => (string)field.GetRawConstantValue()!);

    private static GraphTagBits Bits(string[] list) => new(tag => new GraphTagBit(tag, Array.IndexOf(list, tag)));
}
