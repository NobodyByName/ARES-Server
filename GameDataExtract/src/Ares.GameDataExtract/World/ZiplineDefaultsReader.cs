using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads what every rideable zipline shares (<see cref="ZiplineLineDefaults"/>) and the capsule of generated group spawn
/// points (<see cref="PlayerStartCapsule"/>): the placed line Blueprint's class default, the lines' attachment
/// information asset, the player character Blueprint's mesh template and the group player start's script constructor.
/// </summary>
internal static class ZiplineDefaultsReader
{
    private const string ClassDefaultPrefix = ".Default__";
    private const string ClassSuffix = "_C";
    private const string Rotation = "Rotation";
    private const string Translation = "Translation";
    private const string Scale3D = "Scale3D";
    private const double UnitTolerance = 1e-6;
    private const double LevelTolerance = 1e-9;

    /// <summary>The zipline line defaults.</summary>
    /// <exception cref="InvalidDataException">A value is missing or does not decode as expected.</exception>
    public static ZiplineLineDefaults Read(MapRuleSources sources)
    {
        const string Line = ZiplineClasses.LineBlueprintPackage;
        ExportRef lineDefault = sources.Export(Line, ClassDefaultPrefix + Line[(Line.LastIndexOf('/') + 1)..] + ClassSuffix);
        double threshold = sources.Members(lineDefault, ZiplineDefaultMembers.ForcedTravelDirectionThresholdHome).Get<double>(ZiplineDefaultMembers.ForcedTravelDirectionThresholdHome);

        const string Information = ZiplineDefaultMembers.AttachmentInformation;
        ExportRef asset = sources.Export(Information, "." + Information[(Information.LastIndexOf('/') + 1)..]);
        PropertyBag offset = sources.Members(asset, ZiplineDefaultMembers.OffsetToUse).Get<PropertyBag>(ZiplineDefaultMembers.OffsetToUse);
        Quaternion rotation = offset.Get(Rotation, new Quaternion(0, 0, 0, 1));
        Vector3D translation = offset.Get(Translation, default(Vector3D));
        if (Math.Abs((rotation.X * rotation.X) + (rotation.Y * rotation.Y) + (rotation.Z * rotation.Z) + (rotation.W * rotation.W) - 1) > UnitTolerance)
        {
            throw new InvalidDataException($"{asset.Path}: the attachment offset rotation is not a unit quaternion");
        }

        Transform attachment = new(rotation, new Vector3D(translation.X + 0.0, translation.Y + 0.0, translation.Z + 0.0), offset.Get(Scale3D, Vector3D.One));
        const string Character = ZiplineDefaultMembers.CharacterBlueprint;
        ExportRef mesh = sources.Export(Character, $"{ClassDefaultPrefix}{Character[(Character.LastIndexOf('/') + 1)..]}{ClassSuffix}:{ZiplineDefaultMembers.CharacterMesh}");
        if (sources.Packages.ScriptPath(mesh.Export.Class) != CharacterMeshMembers.ClassPath
            || ScenePart.Read(mesh.Package, mesh.Export, CharacterMeshMembers.ScenePart, NativeTail.None) is not { Location: { } location, Rotation: { } meshRotation }
            || Math.Abs(meshRotation.Pitch) + Math.Abs(meshRotation.Roll) > LevelTolerance)
        {
            throw new InvalidDataException($"{mesh.Path}: unexpected character mesh template");
        }

        return new ZiplineLineDefaults(threshold, attachment, new Vector3D(location.X + 0.0, location.Y + 0.0, location.Z + 0.0), meshRotation.Yaw + 0.0);
    }

    /// <summary>The capsule the group player start's script constructor sets.</summary>
    /// <exception cref="InvalidDataException">The constructor stores no double into a capsule member.</exception>
    public static PlayerStartCapsule Capsule(MapRuleSources sources)
    {
        return new PlayerStartCapsule(
            Double(sources, PlayerStartCapsuleMembers.CapsuleHalfHeight),
            Double(sources, PlayerStartCapsuleMembers.CapsuleRadius));
    }

    private static double Double(MapRuleSources sources, string member)
    {
        ScriptConstant value = sources.Default(PlayerStartCapsuleMembers.ScriptClass, member);
        return value.Size == sizeof(double) ? value.AsDouble : throw new InvalidDataException($"{PlayerStartCapsuleMembers.ScriptClass}.{member} is not set to a double");
    }
}
