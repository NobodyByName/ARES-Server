namespace Ares.GameDataExtract.Scripts;

/// <summary>A value the straight-line evaluation of a script function (<see cref="ScriptStatements"/>) tracked.</summary>
internal abstract record ScriptValue;

/// <summary>A literal of <see cref="Size"/> bytes (1, 2, 4 or 8), as raw bits.</summary>
internal sealed record ScriptConstant(ulong Bits, int Size) : ScriptValue
{
    public byte AsByte => (byte)Bits;

    public int AsInt32 => (int)Bits;

    public float AsSingle => BitConverter.Int32BitsToSingle((int)Bits);

    public double AsDouble => BitConverter.Int64BitsToDouble((long)Bits);

    public static ScriptConstant Of(float value) => new((uint)BitConverter.SingleToInt32Bits(value), sizeof(float));

    public static ScriptConstant Of(double value) => new((ulong)BitConverter.DoubleToInt64Bits(value), sizeof(double));

    public static ScriptConstant Of(int value) => new((uint)value, sizeof(int));
}

/// <summary>The object the function runs on (frame variable 0 of a method).</summary>
internal sealed record ScriptThis : ScriptValue
{
    private ScriptThis()
    {
    }

    public static ScriptThis Instance { get; } = new();
}

/// <summary>
/// The member at byte <see cref="Offset"/> of <see cref="Owner"/> (its address), or with <see cref="IsObject"/> the
/// object that member points to.
/// </summary>
internal sealed record ScriptMember(ScriptValue Owner, int Offset, bool IsObject) : ScriptValue;

/// <summary>The address of a global variable (<see langword="null"/> name when its reference is missing).</summary>
internal sealed record ScriptGlobalAddress(string? Name) : ScriptValue;

/// <summary>A frame variable whose value the evaluation does not know.</summary>
internal sealed record ScriptVariable(short Slot) : ScriptValue;

/// <summary>A local object built by <see cref="Call"/> (a constructor called on a frame variable, or <c>ALLOC</c>).</summary>
internal sealed record ScriptConstructed(ScriptCall Call) : ScriptValue;

/// <summary>The value <see cref="Call"/> returned.</summary>
internal sealed record ScriptReturned(ScriptCall Call) : ScriptValue;

/// <summary><see cref="Value"/> after the numeric conversion instruction <see cref="Conversion"/> (e.g. <c>iTOf</c>).</summary>
internal sealed record ScriptConverted(ScriptValue Value, string Conversion) : ScriptValue;

/// <summary>A value made by an instruction the evaluation does not follow.</summary>
internal sealed record ScriptUnknown(string Opcode) : ScriptValue;

/// <summary>
/// A call: the function (<see langword="null"/> when its reference is missing), the object it is called on (for a
/// method) and its arguments in declaration order.
/// </summary>
internal sealed record ScriptCall(int Position, ScriptFunctionReference? Function, ScriptValue? Object, IReadOnlyList<ScriptValue> Arguments)
{
    public string? Name => Function?.Name;

    /// <summary>The argument at <paramref name="index"/> as a literal, or <see langword="null"/>.</summary>
    public ScriptConstant? Constant(int index) => index < Arguments.Count ? Arguments[index] as ScriptConstant : null;
}

/// <summary>A store of <see cref="Size"/> bytes into the member at byte <see cref="Offset"/> of the object the function runs on.</summary>
internal sealed record ScriptStore(int Position, int Offset, int Size, ScriptValue Value)
{
    /// <summary>The stored value as a literal, or <see langword="null"/>.</summary>
    public ScriptConstant? Constant => Value as ScriptConstant;
}
