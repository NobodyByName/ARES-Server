using Ares.Structs.Game.World;

namespace Ares.Structs.Game.Characters;

/// <summary>Classes of the player actors the server spawns for each player.</summary>
public static class PlayerClasses
{
    /// <summary>The player character (pawn).</summary>
    public static ArchetypeClass Pawn { get; } = new("/Game/Pioneer/Core/Blueprints/Player/BP_PioneerCharacter.BP_PioneerCharacter_C", "/Game/Pioneer/Core/Blueprints/Player/BP_PioneerCharacter.Default__BP_PioneerCharacter_C");

    /// <summary>The player controller.</summary>
    public static ArchetypeClass PlayerController { get; } = new("/Game/Pioneer/Core/Blueprints/Player/BP_PioneerPlayerController.BP_PioneerPlayerController_C", "/Game/Pioneer/Core/Blueprints/Player/BP_PioneerPlayerController.Default__BP_PioneerPlayerController_C");

    /// <summary>The player state.</summary>
    public static ArchetypeClass PlayerState { get; } = new("/Game/Pioneer/Core/Blueprints/Player/BP_PioneerPlayerState.BP_PioneerPlayerState_C", "/Game/Pioneer/Core/Blueprints/Player/BP_PioneerPlayerState.Default__BP_PioneerPlayerState_C");

    /// <summary>The player's squad actor.</summary>
    public static ArchetypeClass Squad { get; } = new("/Script/EmbarkGameplay.EmbarkSquad", "/Script/EmbarkGameplay.Default__EmbarkSquad");

    /// <summary>The replicated member subobject inside a squad.</summary>
    public const string SquadMember = "/Script/EmbarkGameplay.EmbarkSquadMember";

    /// <summary>HUD class the server assigns to the client.</summary>
    public const string Hud = "/Game/Pioneer/Core/UI/BP_PioneerHUD.BP_PioneerHUD_C";

    /// <summary>Checksum the game expects when <see cref="Hud"/> is exported.</summary>
    public const uint HudExportChecksum = 0x295E1466U;

    /// <summary>Name of the player controller's input-correction component.</summary>
    public const string CorrectionComponent = "CorrectionComp";

    /// <summary>Class of <see cref="CorrectionComponent"/>. Its export checksum is not verified yet (0 skips the check).</summary>
    public const string CorrectionComponentClass = "/Script/PendulumRuntime.EmbarkBlackBoardCorrectionComponent";

    /// <summary>Name of the player controller's end-of-round component (class <c>EndOfRoundComponent</c>).</summary>
    public const string EndOfRoundComponent = "EORComponent";

    /// <summary>Name of the player controller's spectator manager component (class <c>PioneerSpectatorManagerComponent</c>).</summary>
    public const string SpectatorManagerComponent = "SpectatorManagerComponent";
}
