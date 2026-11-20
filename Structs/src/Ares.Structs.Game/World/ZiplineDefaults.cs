using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>
/// What every rideable zipline shares: the placed line class's default threshold and the pose of a character hanging
/// from the line (the attachment offset and the player character's mesh placement).
/// </summary>
/// <param name="ForcedTravelDirectionThresholdHome">
/// Distance from the line end within which a character attaching on the end half rides back towards the start
/// (<c>ForcedTravelDirectionThresholdHome</c> of the placed line class).
/// </param>
/// <param name="AttachmentOffset">The line attachment pose (<c>OffsetToUse</c> of the lines' attachment information).</param>
/// <param name="CharacterMeshLocation">Relative location of the player character's mesh.</param>
/// <param name="CharacterMeshYaw">Relative yaw of the player character's mesh, in degrees.</param>
public sealed record ZiplineLineDefaults(
    double ForcedTravelDirectionThresholdHome,
    Transform AttachmentOffset,
    Vector3D CharacterMeshLocation,
    double CharacterMeshYaw);

/// <summary>The assets and members the zipline defaults are read from.</summary>
public static class ZiplineDefaultMembers
{
    /// <summary>The placed line Blueprint's member holding the forced travel threshold.</summary>
    public const string ForcedTravelDirectionThresholdHome = "ForcedTravelDirectionThresholdHome";

    /// <summary>Package of the lines' attachment information asset.</summary>
    public const string AttachmentInformation = "/Game/Pioneer/Core/Gameplay/DA_AttachmentInformation_Zipline";

    /// <summary>The attachment information's member holding the attachment pose (a transform).</summary>
    public const string OffsetToUse = "OffsetToUse";

    /// <summary>Package of the player character Blueprint.</summary>
    public const string CharacterBlueprint = "/Game/Pioneer/Core/Blueprints/Player/BP_PioneerCharacter";

    /// <summary>Name of the player character's mesh component template.</summary>
    public const string CharacterMesh = "CharacterMesh0";
}

/// <summary>
/// Chain facts of the player character's mesh (<c>UEmbarkSkeletalMeshComponent</c>): its scene part starts at
/// <see cref="ScenePart"/> (fitted to the character Blueprint's mesh template, whose relative location and rotation are
/// its last members).
/// </summary>
public static class CharacterMeshMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/EmbarkGameplay.EmbarkSkeletalMeshComponent";

    /// <summary>Index of the scene part.</summary>
    public const int ScenePart = 256;
}
