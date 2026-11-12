using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>What an extraction elevator's interaction component does.</summary>
public enum ExtractionButtonRole
{
    /// <summary>Calls the elevator.</summary>
    Summon,

    /// <summary>Descends with the elevator (leaves the raid).</summary>
    Descend,

    /// <summary>Repairs a broken elevator.</summary>
    Repair,
}

/// <summary>An interaction component of an extraction elevator, and how it starts.</summary>
/// <param name="Component">Component name on the elevator actor.</param>
/// <param name="Role">What the button does.</param>
/// <param name="Class">Class of the component.</param>
/// <param name="StartEnabled">Whether the button can be used from the start.</param>
/// <param name="Prompt">Name of the prompt text the button shows, or null.</param>
/// <param name="InteractionType">Press or hold; null for repair buttons.</param>
/// <param name="HoldSeconds">Seconds the button is held, after its alignment (0 for a press).</param>
/// <param name="AlignmentSeconds">Seconds the character aligns with the button before the hold starts.</param>
/// <param name="AnimationContainer">Package of the interaction animation container, or null.</param>
public readonly record struct ExtractionButton(
    string Component,
    ExtractionButtonRole Role,
    string Class,
    bool StartEnabled,
    string? Prompt,
    StandardInteractionType? InteractionType,
    double HoldSeconds,
    double AlignmentSeconds,
    string? AnimationContainer);

/// <summary>An extraction elevator placed in the map's persistent level.</summary>
/// <param name="Actor">Actor name in the persistent level.</param>
/// <param name="DataAsset">Package of the extraction data asset.</param>
/// <param name="TitleKey">String-table key of the title.</param>
/// <param name="DescriptionKey">String-table key of the description.</param>
/// <param name="DialogueTag">Dialogue tag of the location.</param>
/// <param name="Location">World location of the elevator (its root component), in centimetres.</param>
/// <param name="Rotation">World rotation of the elevator, in degrees.</param>
public readonly record struct ExtractionPointPlacement(
    string Actor,
    string DataAsset,
    string TitleKey,
    string DescriptionKey,
    string DialogueTag,
    Vector3D Location,
    Rotator Rotation);

/// <summary>Timings of an extraction point settings asset, in seconds.</summary>
/// <param name="DefaultExtractionSeconds">Extraction timer once the point is ready.</param>
/// <param name="ShutdownMaxExtractionSeconds">Longest extraction time left when a planned shutdown starts.</param>
/// <param name="TimeToExtractReadySeconds">Time from calling to the doors opening.</param>
/// <param name="SummonHoldSeconds">Hold time of the call button.</param>
/// <param name="ExtractionHoldSeconds">Hold time of a descend switch.</param>
public readonly record struct ExtractionSettings(
    double DefaultExtractionSeconds,
    double ShutdownMaxExtractionSeconds,
    double TimeToExtractReadySeconds,
    double SummonHoldSeconds,
    double ExtractionHoldSeconds);
