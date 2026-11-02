namespace Ares.Structs.Game.Characters;

/// <summary>Default values of an attribute set's attributes.</summary>
/// <param name="Class">Attribute set class path.</param>
/// <param name="Defaults">Attribute defaults, in authored order.</param>
public sealed record AttributeSetDefaults(string Class, IReadOnlyList<AttributeDefault> Defaults);

/// <summary>Default value of one attribute (both its base and current value).</summary>
/// <param name="Name">Attribute name.</param>
/// <param name="Value">Default value.</param>
public readonly record struct AttributeDefault(string Name, double Value);

/// <summary>The ability system component of a character.</summary>
/// <param name="Class">Component class path.</param>
/// <param name="ComponentName">Component name on the character.</param>
/// <param name="ExportChecksum">Checksum the game expects when the class is exported.</param>
public sealed record AbilitySystemComponentInfo(string Class, string ComponentName, uint ExportChecksum);

/// <summary>Default character appearance: no explicit selection in any slot.</summary>
/// <param name="ComponentClass">Customization component class path.</param>
/// <param name="ComponentName">Component name on the character.</param>
/// <param name="RegistryHash">Hash of the customization item registry.</param>
/// <param name="RegistryCount">Number of registry entries.</param>
/// <param name="SlotSelections">Registry index per slot; <see cref="DefaultSelection"/> picks the slot's default.</param>
public sealed record DefaultAppearance(
    string ComponentClass,
    string ComponentName,
    ushort RegistryHash,
    int RegistryCount,
    IReadOnlyList<ushort> SlotSelections)
{
    /// <summary>Slot value that selects the slot's authored default.</summary>
    public const ushort DefaultSelection = 0xFFFF;
}
