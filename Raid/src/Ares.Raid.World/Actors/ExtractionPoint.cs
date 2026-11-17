using Ares.Common.GameData;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Actors;

/// <summary>An extraction elevator with its exported ids.</summary>
/// <param name="Index">Index in the map's elevators (<see cref="MapData.ExtractionPoints"/>).</param>
/// <param name="Guid">The elevator actor.</param>
/// <param name="Buttons">Its button components, in <see cref="MapData.ExtractionButtons"/> order.</param>
/// <param name="Placement">Where the map places it.</param>
/// <param name="Elevator">What the map's elevators share: its extraction zone.</param>
public sealed record ExtractionPoint(int Index, ulong Guid, IReadOnlyList<ulong> Buttons, ExtractionPointPlacement Placement, ExtractionElevator Elevator)
{
    /// <summary>Actor name of the elevator in the map's persistent level.</summary>
    public string Name => Placement.Actor;

    /// <summary>Index of a button in <see cref="MapData.ExtractionButtons"/>.</summary>
    /// <param name="button">Button id.</param>
    /// <returns>The index, or -1 when the id is not one of this elevator's buttons.</returns>
    public int ButtonIndex(ulong button)
    {
        for (int index = 0; index < Buttons.Count; index++)
        {
            if (Buttons[index] == button)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>Whether a location is inside the elevator's extraction zone (a box placed relative to the elevator).</summary>
    /// <param name="location">World location, in centimetres.</param>
    /// <returns>Whether it is inside.</returns>
    public bool Contains(Vector3D location)
    {
        ExtractionPointPlacement placement = Placement;
        Vector3D zone = placement.Rotation.Rotate(Elevator.ZoneLocation);
        Vector3D offset = new(location.X - placement.Location.X - zone.X, location.Y - placement.Location.Y - zone.Y, location.Z - placement.Location.Z - zone.Z);
        Vector3D half = Elevator.ZoneHalfExtent;
        return Math.Abs(Dot(offset, Axis(new Vector3D(1, 0, 0)))) <= half.X
            && Math.Abs(Dot(offset, Axis(new Vector3D(0, 1, 0)))) <= half.Y
            && Math.Abs(Dot(offset, Axis(new Vector3D(0, 0, 1)))) <= half.Z;

        Vector3D Axis(Vector3D unit) => placement.Rotation.Rotate(Elevator.ZoneRotation.Rotate(unit));
    }

    private static double Dot(Vector3D a, Vector3D b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
}
