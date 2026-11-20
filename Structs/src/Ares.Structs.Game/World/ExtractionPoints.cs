using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>State of an extraction point (<c>EExtractionState</c>).</summary>
public enum ExtractionState : byte
{
    Dormant = 0,
    Disabled = 1,
    Broken = 2,
    Closed = 3,
    Closing = 4,
    Requested = 5,
    Arriving = 6,
    Ready = 7,
}

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

/// <summary>An interaction component of an extraction elevator.</summary>
/// <param name="Component">Component name on the elevator actor.</param>
/// <param name="Role">What the button does.</param>
/// <param name="Class">Class of the component.</param>
/// <param name="InteractionType">Press or hold; null for repair buttons.</param>
/// <param name="HoldSeconds">Seconds the button is held, after its alignment (0 for a press).</param>
/// <param name="AlignmentSeconds">Seconds the character aligns with the button before the hold starts.</param>
/// <param name="AnimationContainer">Package of the interaction animation container, or null.</param>
public readonly record struct ExtractionButton(
    string Component,
    ExtractionButtonRole Role,
    string Class,
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

/// <summary>Hold times of an extraction point settings asset, in seconds.</summary>
/// <param name="SummonHoldSeconds">Hold time of the call button.</param>
/// <param name="ExtractionHoldSeconds">Hold time of a descend switch.</param>
public readonly record struct ExtractionSettings(
    double SummonHoldSeconds,
    double ExtractionHoldSeconds);
