namespace Ares.Structs.Core.Assets;

/// <summary>Engine structs that cooked properties hold, with their stored layouts.</summary>
public static class EngineStructs
{
    /// <summary><c>FVector</c>: three doubles.</summary>
    public static StructSchema Vector { get; } = StructSchema.NativeStruct("Vector", NativeLayout.Vector);

    /// <summary><c>FVector3f</c>: three floats.</summary>
    public static StructSchema Vector3F { get; } = StructSchema.NativeStruct("Vector3f", NativeLayout.Vector3F);

    /// <summary><c>FVector2D</c>: two doubles.</summary>
    public static StructSchema Vector2D { get; } = StructSchema.NativeStruct("Vector2D", NativeLayout.Vector2D);

    /// <summary><c>FRotator</c>: three doubles.</summary>
    public static StructSchema Rotator { get; } = StructSchema.NativeStruct("Rotator", NativeLayout.Rotator);

    /// <summary><c>FQuat</c>: four doubles.</summary>
    public static StructSchema Quat { get; } = StructSchema.NativeStruct("Quat", NativeLayout.Quat);

    /// <summary><c>FLinearColor</c>: four floats.</summary>
    public static StructSchema LinearColor { get; } = StructSchema.NativeStruct("LinearColor", NativeLayout.LinearColor);

    /// <summary><c>FGuid</c>: sixteen bytes.</summary>
    public static StructSchema Guid { get; } = StructSchema.NativeStruct("Guid", NativeLayout.Guid);

    /// <summary><c>FSoftObjectPath</c>: package name, asset name and sub-path.</summary>
    public static StructSchema SoftObjectPath { get; } = StructSchema.NativeStruct("SoftObjectPath", NativeLayout.SoftObjectPath);

    /// <summary><c>FSoftClassPath</c>: stored like <see cref="SoftObjectPath"/>.</summary>
    public static StructSchema SoftClassPath { get; } = StructSchema.NativeStruct("SoftClassPath", NativeLayout.SoftObjectPath);

    /// <summary><c>FGameplayTagContainer</c>: the tag names.</summary>
    public static StructSchema GameplayTagContainer { get; } = StructSchema.NativeStruct("GameplayTagContainer", NativeLayout.GameplayTagContainer);

    /// <summary><c>FGameplayTag</c>: a property block holding <c>TagName</c>.</summary>
    public static StructSchema GameplayTag { get; } = new("GameplayTag", [new PropertyField("TagName", PropertyType.Name)]);

    /// <summary>
    /// <c>FGameplayTagQuery</c>: a property block holding the stream version, the tag dictionary, the query token stream
    /// (bytes that index the dictionary) and two descriptions.
    /// </summary>
    public static StructSchema GameplayTagQuery { get; } = new("GameplayTagQuery",
    [
        new PropertyField("TokenStreamVersion", PropertyType.Int32),
        new PropertyField("TagDictionary", PropertyType.ArrayOf(PropertyType.StructOf(GameplayTag))),
        new PropertyField("QueryTokenStream", PropertyType.ArrayOf(PropertyType.Byte)),
        new PropertyField("UserDescription", PropertyType.String),
        new PropertyField("AutoDescription", PropertyType.String),
    ]);

    /// <summary><c>FPrimaryAssetType</c>: a property block holding <c>Name</c>.</summary>
    public static StructSchema PrimaryAssetType { get; } = new("PrimaryAssetType", [new PropertyField("Name", PropertyType.Name)]);

    /// <summary><c>FTransform</c>: a property block holding <c>Rotation</c>, <c>Translation</c> and <c>Scale3D</c>.</summary>
    public static StructSchema Transform { get; } = new("Transform",
    [
        new PropertyField("Rotation", PropertyType.StructOf(Quat)),
        new PropertyField("Translation", PropertyType.StructOf(Vector)),
        new PropertyField("Scale3D", PropertyType.StructOf(Vector)),
    ]);

    /// <summary><c>FInt32Interval</c>: a property block holding <c>Min</c> and <c>Max</c>.</summary>
    public static StructSchema Int32Interval { get; } = new("Int32Interval",
    [
        new PropertyField("Min", PropertyType.Int32),
        new PropertyField("Max", PropertyType.Int32),
    ]);

    /// <summary><c>FPointerToUberGraphFrame</c>, a Blueprint's event graph frame: a property block with no members.</summary>
    public static StructSchema PointerToUberGraphFrame { get; } = new("PointerToUberGraphFrame", []);

    /// <summary>The structs above by script path, for example <c>/Script/CoreUObject.Vector</c>.</summary>
    public static IReadOnlyDictionary<string, StructSchema> ByPath { get; } = new Dictionary<string, StructSchema>(StringComparer.Ordinal)
    {
        ["/Script/CoreUObject.Vector"] = Vector,
        ["/Script/CoreUObject.Vector3f"] = Vector3F,
        ["/Script/CoreUObject.Vector2D"] = Vector2D,
        ["/Script/CoreUObject.Rotator"] = Rotator,
        ["/Script/CoreUObject.Quat"] = Quat,
        ["/Script/CoreUObject.LinearColor"] = LinearColor,
        ["/Script/CoreUObject.Guid"] = Guid,
        ["/Script/CoreUObject.SoftObjectPath"] = SoftObjectPath,
        ["/Script/CoreUObject.SoftClassPath"] = SoftClassPath,
        ["/Script/CoreUObject.PrimaryAssetType"] = PrimaryAssetType,
        ["/Script/CoreUObject.Transform"] = Transform,
        ["/Script/CoreUObject.Int32Interval"] = Int32Interval,
        ["/Script/GameplayTags.GameplayTagContainer"] = GameplayTagContainer,
        ["/Script/GameplayTags.GameplayTagQuery"] = GameplayTagQuery,
        ["/Script/GameplayTags.GameplayTag"] = GameplayTag,
        ["/Script/Engine.PointerToUberGraphFrame"] = PointerToUberGraphFrame,
    };
}
