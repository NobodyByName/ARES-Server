using Ares.Structs.Core.Geometry;

namespace Ares.MapData.Components;

/// <summary>
/// A transform as a matrix in the engine's row-vector convention: the scaled X, Y and Z axes as rows, then the origin
/// (<c>FTransform::ToMatrixWithScale</c>). A point p maps to <c>p.X · AxisX + p.Y · AxisY + p.Z · AxisZ + Origin</c>.
/// </summary>
internal readonly record struct TransformMatrix(Vector3D AxisX, Vector3D AxisY, Vector3D AxisZ, Vector3D Origin)
{
    /// <summary>This matrix followed by <paramref name="parent"/>: a child-space matrix taken to the parent's space.</summary>
    public TransformMatrix Then(TransformMatrix parent)
    {
        return new TransformMatrix(parent.Map(AxisX), parent.Map(AxisY), parent.Map(AxisZ), Add(parent.Map(Origin), parent.Origin));
    }

    /// <summary>Applies the 3×3 part to a row vector (no translation).</summary>
    public Vector3D Map(Vector3D v)
    {
        return new Vector3D(
            (v.X * AxisX.X) + (v.Y * AxisY.X) + (v.Z * AxisZ.X),
            (v.X * AxisX.Y) + (v.Y * AxisY.Y) + (v.Z * AxisZ.Y),
            (v.X * AxisX.Z) + (v.Y * AxisY.Z) + (v.Z * AxisZ.Z));
    }

    private static Vector3D Add(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
}

/// <summary>Engine rotation and transform arithmetic on <see cref="Transform"/> (<c>FRotator</c>, <c>FQuat</c> and <c>FTransform</c> rules).</summary>
internal static class TransformMath
{
    private const double DegreesToRadians = Math.PI / 180.0;

    /// <summary>The quaternion of a rotator (<c>FRotator::Quaternion</c>).</summary>
    public static Quaternion ToQuaternion(Rotator rotator)
    {
        double pitch = rotator.Pitch * DegreesToRadians / 2;
        double yaw = rotator.Yaw * DegreesToRadians / 2;
        double roll = rotator.Roll * DegreesToRadians / 2;
        double sp = Math.Sin(pitch);
        double cp = Math.Cos(pitch);
        double sy = Math.Sin(yaw);
        double cy = Math.Cos(yaw);
        double sr = Math.Sin(roll);
        double cr = Math.Cos(roll);
        return new Quaternion(
            (cr * sp * sy) - (sr * cp * cy),
            (-cr * sp * cy) - (sr * cp * sy),
            (cr * cp * sy) - (sr * sp * cy),
            (cr * cp * cy) + (sr * sp * sy));
    }

    /// <summary>The transform of a relative location, rotation and scale.</summary>
    public static Transform FromParts(Vector3D location, Rotator rotation, Vector3D scale) => new(ToQuaternion(rotation), location, scale);

    /// <summary><paramref name="a"/> · <paramref name="b"/>: rotation <paramref name="b"/>, then <paramref name="a"/>.</summary>
    private static Quaternion Multiply(Quaternion a, Quaternion b)
    {
        return new Quaternion(
            (a.W * b.X) + (a.X * b.W) + (a.Y * b.Z) - (a.Z * b.Y),
            (a.W * b.Y) - (a.X * b.Z) + (a.Y * b.W) + (a.Z * b.X),
            (a.W * b.Z) + (a.X * b.Y) - (a.Y * b.X) + (a.Z * b.W),
            (a.W * b.W) - (a.X * b.X) - (a.Y * b.Y) - (a.Z * b.Z));
    }

    /// <summary>Rotates <paramref name="v"/> by <paramref name="q"/>.</summary>
    public static Vector3D Rotate(Quaternion q, Vector3D v)
    {
        double tx = 2 * ((q.Y * v.Z) - (q.Z * v.Y));
        double ty = 2 * ((q.Z * v.X) - (q.X * v.Z));
        double tz = 2 * ((q.X * v.Y) - (q.Y * v.X));
        return new Vector3D(
            v.X + (q.W * tx) + ((q.Y * tz) - (q.Z * ty)),
            v.Y + (q.W * ty) + ((q.Z * tx) - (q.X * tz)),
            v.Z + (q.W * tz) + ((q.X * ty) - (q.Y * tx)));
    }

    /// <summary>The matrix of a transform (<c>FTransform::ToMatrixWithScale</c>).</summary>
    public static TransformMatrix ToMatrix(Transform transform)
    {
        Vector3D x = Rotate(transform.Rotation, new Vector3D(1, 0, 0));
        Vector3D y = Rotate(transform.Rotation, new Vector3D(0, 1, 0));
        Vector3D z = Rotate(transform.Rotation, new Vector3D(0, 0, 1));
        Vector3D s = transform.Scale3D;
        return new TransformMatrix(Scale(x, s.X), Scale(y, s.Y), Scale(z, s.Z), transform.Translation);
    }

    /// <summary>
    /// <paramref name="child"/> relative to <paramref name="parent"/> (<c>FTransform</c> child · parent). An absolute
    /// location, rotation or scale ignores the parent's. With a negative scale on either side the matrices are
    /// multiplied and the combined scale keeps its signs on the axes, as <c>FTransform::MultiplyUsingMatrixWithScale</c> does.
    /// </summary>
    /// <exception cref="InvalidDataException">An absolute member is combined with a negative scale.</exception>
    public static Transform Compose(Transform child, Transform parent, AbsoluteMembers absolute = default)
    {
        Vector3D parentScale = parent.Scale3D;
        Vector3D childScale = child.Scale3D;
        Vector3D scale = absolute.Scale
            ? childScale
            : new Vector3D(childScale.X * parentScale.X, childScale.Y * parentScale.Y, childScale.Z * parentScale.Z);
        bool negative = childScale.X < 0 || childScale.Y < 0 || childScale.Z < 0 || parentScale.X < 0 || parentScale.Y < 0 || parentScale.Z < 0;
        if (negative)
        {
            if (absolute.Any)
            {
                throw new InvalidDataException("An absolute transform member is combined with a negative scale");
            }

            TransformMatrix a = ToMatrix(child);
            TransformMatrix b = ToMatrix(parent);
            Vector3D origin = Add(b.Map(a.Origin), b.Origin);
            Vector3D rowX = Normalize(b.Map(a.AxisX), scale.X);
            Vector3D rowY = Normalize(b.Map(a.AxisY), scale.Y);
            Vector3D rowZ = Normalize(b.Map(a.AxisZ), scale.Z);
            return new Transform(MatrixQuaternion(rowX, rowY, rowZ), origin, scale);
        }

        Vector3D location = absolute.Location
            ? child.Translation
            : Add(parent.Translation, Rotate(parent.Rotation, new Vector3D(child.Translation.X * parentScale.X, child.Translation.Y * parentScale.Y,
                child.Translation.Z * parentScale.Z)));
        Quaternion rotation = absolute.Rotation ? child.Rotation : Multiply(parent.Rotation, child.Rotation);
        return new Transform(rotation, location, scale);
    }

    /// <summary>
    /// The transform of a matrix without shear (<c>FTransform::SetFromMatrix</c>): the scale is the length of each axis
    /// row, with the X scale negated when the matrix flips handedness; the rotation is that of the unit axes.
    /// </summary>
    /// <exception cref="InvalidDataException">An axis has zero length or the matrix is not finite.</exception>
    public static Transform FromMatrix(TransformMatrix matrix)
    {
        double scaleX = Length(matrix.AxisX);
        double scaleY = Length(matrix.AxisY);
        double scaleZ = Length(matrix.AxisZ);
        if (!(scaleX > 0 && scaleY > 0 && scaleZ > 0) || !double.IsFinite(scaleX + scaleY + scaleZ))
        {
            throw new InvalidDataException("The matrix has a zero or invalid axis");
        }

        Vector3D x = matrix.AxisX;
        Vector3D y = matrix.AxisY;
        Vector3D z = matrix.AxisZ;
        double determinant = (x.X * ((y.Y * z.Z) - (y.Z * z.Y))) - (x.Y * ((y.X * z.Z) - (y.Z * z.X))) + (x.Z * ((y.X * z.Y) - (y.Y * z.X)));
        if (determinant < 0)
        {
            scaleX = -scaleX;
        }

        Quaternion rotation = MatrixQuaternion(Scale(x, 1 / scaleX), Scale(y, 1 / scaleY), Scale(z, 1 / scaleZ));
        return new Transform(rotation, matrix.Origin, new Vector3D(scaleX, scaleY, scaleZ));
    }

    /// <summary>The rotation of a matrix given by its unit axis rows (<c>FQuat(const FMatrix&amp;)</c>).</summary>
    private static Quaternion MatrixQuaternion(Vector3D rowX, Vector3D rowY, Vector3D rowZ)
    {
        double[][] rows = [[rowX.X, rowX.Y, rowX.Z], [rowY.X, rowY.Y, rowY.Z], [rowZ.X, rowZ.Y, rowZ.Z]];
        double trace = rows[0][0] + rows[1][1] + rows[2][2];
        if (trace > 0)
        {
            double inverseRoot = 1 / Math.Sqrt(trace + 1);
            double half = 0.5 * inverseRoot;
            return new Quaternion((rows[1][2] - rows[2][1]) * half, (rows[2][0] - rows[0][2]) * half, (rows[0][1] - rows[1][0]) * half, 0.5 / inverseRoot);
        }

        int i = 0;
        if (rows[1][1] > rows[0][0])
        {
            i = 1;
        }

        if (rows[2][2] > rows[i][i])
        {
            i = 2;
        }

        int j = (i + 1) % 3;
        int k = (i + 2) % 3;
        double inverse = 1 / Math.Sqrt(rows[i][i] - rows[j][j] - rows[k][k] + 1);
        double s = 0.5 * inverse;
        double[] q = new double[4];
        q[i] = 0.5 / inverse;
        q[3] = (rows[j][k] - rows[k][j]) * s;
        q[j] = (rows[i][j] + rows[j][i]) * s;
        q[k] = (rows[i][k] + rows[k][i]) * s;
        double length = Math.Sqrt((q[0] * q[0]) + (q[1] * q[1]) + (q[2] * q[2]) + (q[3] * q[3]));
        return new Quaternion(q[0] / length, q[1] / length, q[2] / length, q[3] / length);
    }

    private static Vector3D Normalize(Vector3D row, double sign)
    {
        double length = Math.Sqrt((row.X * row.X) + (row.Y * row.Y) + (row.Z * row.Z));
        double factor = Math.CopySign(1.0, sign);
        return new Vector3D(row.X / length * factor, row.Y / length * factor, row.Z / length * factor);
    }

    private static double Length(Vector3D v) => Math.Sqrt((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z));

    private static Vector3D Scale(Vector3D v, double factor) => new(v.X * factor, v.Y * factor, v.Z * factor);

    private static Vector3D Add(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
}

/// <summary>Which of a scene component's location, rotation and scale ignore its attach parent's (<c>bAbsoluteLocation</c> and so on).</summary>
internal readonly record struct AbsoluteMembers(bool Location, bool Rotation, bool Scale)
{
    /// <summary>Whether any member is absolute.</summary>
    public bool Any => Location || Rotation || Scale;
}
