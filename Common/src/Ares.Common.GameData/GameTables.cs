using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;

namespace Ares.Common.GameData;

/// <summary>
/// File names of the global JSON tables (at the root of the game data directory, see <see cref="GameDataFiles"/>) and
/// their row types in <see cref="GameDataJsonContext"/>.
/// </summary>
public static class GameTables
{
    /// <summary>
    /// The usable-item catalogue (<see cref="ItemDefinition"/>), by usable index: every item data asset of the asset
    /// registry, so the row count is the catalogue's size.
    /// </summary>
    public const string UsableItems = "usable-items.json";

    /// <summary>The item value cache (<see cref="ItemValue"/>), in the cache's order.</summary>
    public const string ItemValues = "item-values.json";

    /// <summary>
    /// The world item class list (class path strings), in list order: a class's index is its bit in the game state's
    /// preload bitfield.
    /// </summary>
    public const string WorldItemList = "world-item-list.json";

    /// <summary>Every augment (<see cref="AugmentDefinition"/>), by usable index.</summary>
    public const string Augments = "augments.json";

    /// <summary>Every loadout frame (<see cref="LoadoutFrameDefinition"/>), ordered by technical name (ordinal).</summary>
    public const string LoadoutFrames = "loadout-frames.json";

    /// <summary>Every firearm item class (<see cref="FirearmDefinition"/>), ordered by class path (ordinal).</summary>
    public const string Firearms = "firearms.json";

    /// <summary>Every shield item class (<see cref="ShieldDefinition"/>), ordered by class path (ordinal).</summary>
    public const string Shields = "shields.json";

    /// <summary>
    /// Menu definitions (<see cref="MenuItem"/>) of the tier-1 augments and the inventory structures of their loadout
    /// frames and augment slot, ordered by asset id.
    /// </summary>
    public const string MenuAugments = "menu-augments.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the currency items.</summary>
    public const string MenuCurrency = "menu-currency.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the gadget items.</summary>
    public const string MenuGadgets = "menu-gadgets.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the salvage materials, by usable index.</summary>
    public const string MenuSalvage = "menu-salvage.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the shield items.</summary>
    public const string MenuShields = "menu-shields.json";

    /// <summary>Menu definitions (<see cref="MenuItem"/>) of the firearm and ammunition items, then the firearms' mod slots in slot order.</summary>
    public const string MenuWeapons = "menu-weapons.json";

    /// <summary>The dodge-roll variants (<see cref="DodgeRollSequence"/>), in <see cref="DodgeRollKind"/> order.</summary>
    public const string DodgeRolls = "dodge-rolls.json";

    /// <summary>
    /// The vault definition sets (<see cref="VaultDefinitionSet"/>), in the order of <see cref="MovementAssets.VaultPackages"/>:
    /// armed, unarmed, armed exhausted, unarmed exhausted.
    /// </summary>
    public const string VaultDefinitions = "vault-definitions.json";

    /// <summary>The levels of an ordinary heavy landing (<see cref="HeavyLandingLevel"/>), lowest fall height first.</summary>
    public const string HeavyLandingLevels = "heavy-landing-levels.json";

    /// <summary>The levels of a heavy landing softened by a recovery roll (<see cref="HeavyLandingLevel"/>), lowest fall height first.</summary>
    public const string RecoveryLandingLevels = "recovery-landing-levels.json";

    /// <summary>
    /// The gameplay tags the character graph tracks (strings), in bit order: tag <c>k</c> is bit <c>k % 64</c> of tag
    /// word <c>k / 64</c> (see <see cref="MovementAssets.GraphTagWordBits"/>).
    /// </summary>
    public const string GraphStateTags = "graph-state-tags.json";

    /// <summary>The snap hook item's class defaults (<see cref="SnapHookItemDefaults"/>), one row.</summary>
    public const string SnapHookItem = "snap-hook-item.json";

    /// <summary>The sprint burst alpha curve over elapsed seconds (<see cref="WeightedCurveKey"/>), in key order.</summary>
    public const string SprintBurstCurve = "sprint-burst-curve.json";

    /// <summary>The loot container types the game registers (<see cref="LootContainerType"/>), ordered by tag (ordinal).</summary>
    public const string LootContainerTypes = "loot-container-types.json";

    /// <summary>
    /// The loot container classes the container types and the maps name (<see cref="LootContainerClass"/>), ordered by
    /// class path (ordinal). It lists every class that decodes, also those a server does not serve.
    /// </summary>
    public const string LootContainerClasses = "loot-container-classes.json";

    /// <summary>The value loot tables (<see cref="StaticValueLootTable"/>), ordered by package (ordinal).</summary>
    public const string StaticValueLootTables = "static-value-loot-tables.json";

    /// <summary>The skill loot tables (<see cref="SkillLootTable"/>), ordered by package (ordinal).</summary>
    public const string SkillLootTables = "skill-loot-tables.json";

    /// <summary>
    /// The interaction animation containers that change character movement (<see cref="InteractionAnimationContainer"/>):
    /// those of the character's fallback list and knockout, the door breaches, the elevator buttons and the search
    /// animations of every loot container class that decodes, ordered by the last segment of their path (ordinal).
    /// </summary>
    public const string InteractionAnimationContainers = "interaction-animation-containers.json";

    /// <summary>
    /// The character's fallback interaction animation containers (<see cref="TaggedInteractionAnimationContainer"/>), in
    /// list order: an interaction without a container of its own uses the first entry whose tags it has, else the first.
    /// </summary>
    public const string TaggedInteractionAnimationContainers = "tagged-interaction-animation-containers.json";
}
