namespace Ares.Structs.Game;

/// <summary>
/// What the game's script classes set up in their own code, which no cooked asset stores: the game state class a game
/// mode creates and the extraction type an extraction point assigns.
/// </summary>
public static class ScriptWiring
{
    /// <summary>Script class of the game mode the maps run; a map's game mode class is it or derives from it.</summary>
    public const string SalvageGameMode = "APioneerGameModeSalvage";

    /// <summary>Script class of the game state <see cref="SalvageGameMode"/> creates.</summary>
    public const string SalvageGameState = "APioneerGameStateSalvage";

    /// <summary>Extraction type tag of the elevator extraction points (<c>ASalvageExtractionPoint_Elevator</c>).</summary>
    public const string ElevatorExtractionType = "GameMode.Salvage.ExtractionPoint.Elevator";
}
