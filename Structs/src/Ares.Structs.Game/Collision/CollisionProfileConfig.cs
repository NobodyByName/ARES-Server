using Ares.Structs.Core;

namespace Ares.Structs.Game.Collision;

/// <summary>
/// Where the game defines its collision channels, profiles and physics settings, and the profile and channel names that
/// decide what blocks a player's movement and vaulting.
/// </summary>
public static class CollisionProfileConfig
{
    /// <summary>The pak file holding the config files, relative to the game's install folder.</summary>
    public const string ConfigPak = GameBuild.ProjectName + "/Content/Paks/pakchunk0-WindowsClient.pak";

    /// <summary>The config section of the collision channels and profiles.</summary>
    public const string Section = "/Script/Engine.CollisionProfile";

    /// <summary>The config section of the physics settings.</summary>
    public const string PhysicsSection = "/Script/Engine.PhysicsSettings";

    /// <summary>
    /// The physics setting (in <see cref="PhysicsSection"/>) that decides which collision a body whose trace flag is
    /// <see cref="CollisionTraceFlag.UseDefault"/> offers, written as a trace flag's config name.
    /// </summary>
    public const string DefaultShapeComplexity = "DefaultShapeComplexity";

    /// <summary>The value of <see cref="DefaultShapeComplexity"/> when no config file sets it.</summary>
    public const CollisionTraceFlag EngineDefaultShapeComplexity = CollisionTraceFlag.SimpleAndComplex;

    /// <summary>The profile of the player character's movement capsule.</summary>
    public const string PlayerCapsuleProfile = "PlayerPawn";

    /// <summary>The trace channel of vault, mantle and wall checks.</summary>
    public const string ClimbableChannel = "Climbable";

    /// <summary>
    /// Config files that set <see cref="Section"/> and <see cref="PhysicsSection"/>, relative to the game's install
    /// folder, in the order the game applies them.
    /// </summary>
    public static IReadOnlyList<string> ConfigFiles { get; } =
    [
        "Engine/Config/BaseEngine.ini",
        GameBuild.ProjectName + "/Config/DefaultEngine.ini",
    ];

    /// <summary>The trace flag a config value names (<c>CTF_UseSimpleAndComplex</c> and so on, any case), or <see langword="null"/>.</summary>
    public static CollisionTraceFlag? ParseTraceFlag(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ToUpperInvariant() switch
        {
            "CTF_USEDEFAULT" => CollisionTraceFlag.UseDefault,
            "CTF_USESIMPLEANDCOMPLEX" => CollisionTraceFlag.SimpleAndComplex,
            "CTF_USESIMPLEASCOMPLEX" => CollisionTraceFlag.SimpleAsComplex,
            "CTF_USECOMPLEXASSIMPLE" => CollisionTraceFlag.ComplexAsSimple,
            _ => null,
        };
    }
}
