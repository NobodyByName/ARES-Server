namespace Ares.Structs.Core.Assets;

/// <summary>
/// Cooked animation data: the per-track bone codec's track words, the notify event structs and the curve keys of
/// curve assets.
/// </summary>
public static class AnimationFormats
{
    /// <summary>Name of the per-track bone codec object, followed by its decimal instance number.</summary>
    public const string PerTrackCodecPrefix = "AnimCompress_PerTrackCompression_";

    /// <summary>Bit position of a track word's key format (bits 28–31).</summary>
    public const int TrackFormatShift = 28;

    /// <summary>Bit position of a track word's format flags (bits 24–27).</summary>
    public const int TrackFlagsShift = 24;

    /// <summary>Mask of a track word's format flags after the shift.</summary>
    public const int TrackFlagsMask = 0xF;

    /// <summary>Mask of a track word's key count (bits 0–23).</summary>
    public const int TrackKeyCountMask = 0xFFFFFF;

    /// <summary>Key format of uncompressed float keys (three floats per translation key, no W).</summary>
    public const int Float96NoW = 1;

    /// <summary>Format flags of a track storing only its Y component (flags bits 0–2 select X, Y, Z; none set stores all three).</summary>
    public const int YComponentOnly = 2;

    /// <summary>Format flag of a track whose keys are followed by a frame table, one byte per key.</summary>
    public const int FrameTableFlag = 8;

    /// <summary>Track offset of a track without data (identity rotation, no translation).</summary>
    public const int NoTrack = -1;

    /// <summary>Bytes of a track-offset table entry per track: translation, rotation and scale offsets.</summary>
    public const int TrackOffsetEntrySize = 12;

    /// <summary>Unversioned header word of a stored <see cref="AnimNotifyEvent"/>: all 26 members listed in one last fragment with a zero mask.</summary>
    public const ushort NotifyEventHeader = 0x3580;

    /// <summary>Index of <c>NotifyStateClass</c> in <see cref="AnimNotifyEvent"/>'s chain.</summary>
    public const int NotifyStateClassIndex = 5;

    /// <summary>Bytes of one weighted rich-curve key: interpolation, tangent mode and weight mode bytes, then six floats.</summary>
    public const int RichCurveKeySize = 27;

    /// <summary>Rich-curve key interpolation <c>RCIM_Cubic</c>.</summary>
    public const byte CubicInterpolation = 2;

    /// <summary>Rich-curve key tangent weight mode <c>RCTWM_WeightedBoth</c>.</summary>
    public const byte WeightedBothTangents = 3;

    /// <summary>Most zero bytes that follow the rich curves of a translate-rotate curve asset.</summary>
    public const int TranslateRotateCurveTrailer = 8;

    /// <summary><c>FAnimLinkableElement</c>: where a notify or its end is linked in a montage or sequence.</summary>
    public static StructSchema AnimLinkableElement { get; } = new("AnimLinkableElement",
    [
        new PropertyField("LinkedMontage", PropertyType.Object),
        new PropertyField("SlotIndex", PropertyType.Int32),
        new PropertyField("SegmentIndex", PropertyType.Int32),
        new PropertyField("LinkMethod", PropertyType.ByteEnum),
        new PropertyField("CachedLinkMethod", PropertyType.ByteEnum),
        new PropertyField("SegmentBeginTime", PropertyType.Float),
        new PropertyField("SegmentLength", PropertyType.Float),
        new PropertyField("LinkValue", PropertyType.Float),
        new PropertyField("LinkedSequence", PropertyType.Object),
    ]);

    /// <summary><c>FAnimNotifyEvent</c>: a notify of an animation; its own start is the linkable element's <c>LinkValue</c>.</summary>
    public static StructSchema AnimNotifyEvent { get; } = new("AnimNotifyEvent",
    [
        new PropertyField("TriggerTimeOffset", PropertyType.Float),
        new PropertyField("EndTriggerTimeOffset", PropertyType.Float),
        new PropertyField("TriggerWeightThreshold", PropertyType.Float),
        new PropertyField("NotifyName", PropertyType.Name),
        new PropertyField("Notify", PropertyType.Object),
        new PropertyField("NotifyStateClass", PropertyType.Object),
        new PropertyField("Duration", PropertyType.Float),
        new PropertyField("EndLink", PropertyType.StructOf(AnimLinkableElement)),
        new PropertyField("bConvertedFromBranchingPoint", PropertyType.Bool),
        new PropertyField("MontageTickType", PropertyType.ByteEnum),
        new PropertyField("NotifyTriggerChance", PropertyType.Float),
        new PropertyField("NotifyFilterType", PropertyType.ByteEnum),
        new PropertyField("NotifyFilterLOD", PropertyType.Int32),
        new PropertyField("bCanBeFilteredViaRequest", PropertyType.Bool),
        new PropertyField("bTriggerOnDedicatedServer", PropertyType.Bool),
        new PropertyField("bTriggerOnFollower", PropertyType.Bool),
        new PropertyField("TrackIndex", PropertyType.Int32),
    ], AnimLinkableElement);
}
