using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.Replication;

/// <summary>
/// An actor's replicated movement (<c>FRepMovement</c>) with the engine's default quantization: location and
/// velocities rounded to whole units, rotation axes compressed to one byte each.
/// </summary>
/// <param name="Location">Location.</param>
/// <param name="Rotation">Rotation in degrees.</param>
public sealed record RepMovement(Vector3D Location, Rotator Rotation)
{
    private const int FlagBits = 4;
    private const int AxisSteps = 256;

    /// <summary>Linear velocity.</summary>
    public Vector3D LinearVelocity { get; init; }

    /// <summary>Angular velocity; sent only with <see cref="RepPhysics"/>.</summary>
    public Vector3D AngularVelocity { get; init; }

    /// <summary>Whether the actor's body is simulated (<c>bRepPhysics</c>): clients simulate it from this state.</summary>
    public bool RepPhysics { get; init; }

    /// <summary>Whether the simulated body is asleep (<c>bSimulatedPhysicSleep</c>).</summary>
    public bool SimulatedPhysicSleep { get; init; }

    /// <summary>Server physics frame (<c>ServerFrame</c>), when sent.</summary>
    public uint? ServerFrame { get; init; }

    /// <summary>Server physics object handle (<c>ServerPhysicsHandle</c>), when sent.</summary>
    public uint? ServerPhysicsHandle { get; init; }

    /// <summary>
    /// Writes the movement. A flag bit follows the movement data; it is always clear (what a set bit adds is not
    /// known).
    /// </summary>
    /// <param name="writer">Destination.</param>
    public void Write(BitWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!RepPhysics && AngularVelocity != default)
        {
            throw new InvalidPacketException("Angular velocity is only sent with replicated physics");
        }

        uint flags = (SimulatedPhysicSleep ? 1U : 0) | (RepPhysics ? 2U : 0) | (ServerFrame is null ? 0 : 4U) | (ServerPhysicsHandle is null ? 0 : 8U);
        writer.WriteBits(flags, FlagBits);
        QuantizedVector.Write(writer, Location, 1);
        foreach (double angle in (ReadOnlySpan<double>)[Rotation.Pitch, Rotation.Yaw, Rotation.Roll])
        {
            if (!double.IsFinite(angle))
            {
                throw new InvalidPacketException("Rotation must be finite");
            }

            byte compressed = Compress(angle);
            writer.WriteBit(compressed != 0);
            if (compressed != 0)
            {
                writer.WriteByte(compressed);
            }
        }

        QuantizedVector.Write(writer, LinearVelocity, 1);
        if (RepPhysics)
        {
            QuantizedVector.Write(writer, AngularVelocity, 1);
        }

        if (ServerFrame is { } frame)
        {
            writer.WritePacked(frame);
        }

        if (ServerPhysicsHandle is { } handle)
        {
            writer.WritePacked(handle);
        }

        writer.WriteBit(false);
    }

    /// <summary>Reads movement.</summary>
    /// <param name="reader">Source.</param>
    /// <returns>The movement, quantized as sent.</returns>
    public static RepMovement Read(BitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        uint flags = (uint)reader.ReadBits(FlagBits);
        bool physics = (flags & 2) != 0;
        Vector3D location = QuantizedVector.Read(reader, 1);
        Rotator rotation = new(ReadAxis(reader), ReadAxis(reader), ReadAxis(reader));
        Vector3D linear = QuantizedVector.Read(reader, 1);
        Vector3D angular = physics ? QuantizedVector.Read(reader, 1) : default;
        uint? frame = (flags & 4) != 0 ? reader.ReadPacked() : null;
        uint? handle = (flags & 8) != 0 ? reader.ReadPacked() : null;
        if (reader.ReadBit())
        {
            throw new InvalidPacketException("Unsupported trailing movement data");
        }

        return new RepMovement(location, rotation)
        {
            LinearVelocity = linear,
            AngularVelocity = angular,
            RepPhysics = physics,
            SimulatedPhysicSleep = (flags & 1) != 0,
            ServerFrame = frame,
            ServerPhysicsHandle = handle,
        };
    }

    // Rounds like the engine (FMath::RoundToInt) and keeps the low byte.
    private static byte Compress(double angle) => (byte)((int)Math.Floor(((angle % 360) * AxisSteps / 360) + 0.5) & (AxisSteps - 1));

    private static double Decompress(byte value) => value * 360.0 / AxisSteps;

    private static double ReadAxis(BitReader reader) => reader.ReadBit() ? Decompress(reader.ReadByte()) : 0;
}
