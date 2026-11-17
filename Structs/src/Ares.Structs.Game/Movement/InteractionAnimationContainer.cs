namespace Ares.Structs.Game.Movement;

/// <summary>How an interaction animation lines the character up with the interaction (<c>EAttachmentAlignmentType</c>).</summary>
public enum InteractionAlignment : byte
{
    /// <summary><c>None</c>.</summary>
    None = 0,

    /// <summary><c>LookAtAlignment</c>.</summary>
    LookAtAlignment = 1,

    /// <summary><c>LookAtMatrixComponentUp</c>.</summary>
    LookAtMatrixComponentUp = 2,

    /// <summary><c>ComponentAlignment</c>.</summary>
    ComponentAlignment = 3,

    /// <summary><c>InteractionComponentFunction</c>.</summary>
    InteractionComponentFunction = 4,
}

/// <summary>An interaction animation container (<c>UInteractionAnimationContainer</c>): what it does to movement.</summary>
/// <param name="Path">Object path of the container.</param>
/// <param name="Alignment">How the character lines up; anything but <see cref="InteractionAlignment.None"/> plays the interaction as root motion.</param>
/// <param name="FinishesAlignmentAnimationOnStop">Whether a stopped interaction finishes its alignment animation first.</param>
/// <param name="AllowsFloatingAlignment">Whether the alignment may leave the ground.</param>
/// <param name="FullBody">Whether the animation uses the whole body; such interactions never change the crouch.</param>
/// <param name="CrouchHeight">Height of the interaction above the feet below which the character crouches.</param>
/// <param name="FinishesWithAnimations">Whether the interaction ends when its animations end.</param>
/// <param name="Unending">Whether the interaction never ends on its own.</param>
public sealed record InteractionAnimationContainer(
    string Path,
    InteractionAlignment Alignment,
    bool FinishesAlignmentAnimationOnStop,
    bool AllowsFloatingAlignment,
    bool FullBody,
    double CrouchHeight,
    bool FinishesWithAnimations,
    bool Unending);

/// <summary>A semantic-tag entry of the character's fallback containers.</summary>
/// <param name="SemanticTags">Semantic tags of the interaction that select the entry (none: never selected by tag).</param>
/// <param name="Container">Object path of the container.</param>
public sealed record TaggedInteractionAnimationContainer(IReadOnlyList<string> SemanticTags, string Container);
