using Ares.GameDataExtract.Scripts;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Default statements the map rule readers find in script bytecode by their instruction shape (reference checks left
/// out), where the straight-line evaluation (<see cref="ScriptStatements"/>) does not keep the value: global values
/// passed to calls, and literals stored into members of member objects.
/// </summary>
internal static class ScriptPatterns
{
    private const string ReferenceCheck = "CHKREF";
    private const string PushGlobal = "PshGPtr";
    private const string PushMemberObject = "PshThisMemObj";
    private const string PushMemberAddress = "PshThisMemRef";
    private const string AddOffset = "ADDSi";
    private const string PopAddress = "PopRPtr";
    private const string SetByte = "SetV1";
    private const string WriteByte = "WRTV1";
    private const string SystemCall = "CALLSYS";
    private const string InterfaceCall = "CALLINTF";
    private const string Assign = "opAssign";

    /// <summary>
    /// The globals assigned to member <paramref name="offset"/> of the object (<c>member = Global</c>: push the global,
    /// push the member's address, call <c>opAssign</c>), in code order.
    /// </summary>
    public static IEnumerable<string> GlobalsAssignedTo(ScriptCache cache, ScriptFunction function, int offset)
    {
        ScriptInstruction[] code = Code(function);
        for (int i = 2; i < code.Length; i++)
        {
            if (code[i].Name == SystemCall && cache.FunctionByKey((long)code[i].QuadWord)?.Name == Assign
                && code[i - 1] is { Name: PushMemberAddress } member && member.WordArgument == offset
                && code[i - 2].Name == PushGlobal && cache.GlobalByKey((long)code[i - 2].QuadWord) is { } global)
            {
                yield return global;
            }
        }
    }

    /// <summary>
    /// The globals passed to method <paramref name="method"/> of member objects (<c>member.Method(Global)</c>: push the
    /// global, push the member object, call the method), by member offset; a later call replaces an earlier one.
    /// </summary>
    public static IReadOnlyDictionary<int, string> GlobalArguments(ScriptCache cache, ScriptFunction function, string method)
    {
        ScriptInstruction[] code = Code(function);
        Dictionary<int, string> found = [];
        for (int i = 2; i < code.Length; i++)
        {
            if (code[i].Name == InterfaceCall && cache.FunctionById((int)code[i].DoubleWord(0))?.Name == method
                && code[i - 1].Name == PushMemberObject
                && code[i - 2].Name == PushGlobal && cache.GlobalByKey((long)code[i - 2].QuadWord) is { } global)
            {
                found[code[i - 1].WordArgument] = global;
            }
        }

        return found;
    }

    /// <summary>
    /// The one-byte literals stored into the member at <paramref name="memberOffset"/> of member objects
    /// (<c>member.Field = literal</c>: set a variable, push the member object, add the field offset, pop the address,
    /// write the variable), by member object offset; a later store replaces an earlier one.
    /// </summary>
    public static IReadOnlyDictionary<int, byte> BytesStoredInMembers(ScriptFunction function, int memberOffset)
    {
        ScriptInstruction[] code = Code(function);
        Dictionary<int, byte> found = [];
        for (int i = 4; i < code.Length; i++)
        {
            if (code[i].Name == WriteByte && code[i - 1].Name == PopAddress
                && code[i - 2] is { Name: AddOffset } field && field.WordArgument == memberOffset
                && code[i - 3].Name == PushMemberObject
                && code[i - 4] is { Name: SetByte } literal && literal.WordArgument == code[i].WordArgument)
            {
                found[code[i - 3].WordArgument] = (byte)literal.DoubleWord(0);
            }
        }

        return found;
    }

    /// <summary>The default statement functions of <paramref name="name"/>'s script chain, base class first (constructors, then <c>__InitDefaults</c>).</summary>
    public static IEnumerable<ScriptFunction> DefaultFunctions(ScriptCache cache, string name)
    {
        return cache.Chain(name, out _).Reverse().SelectMany(type => type.Constructors.Concat(type.MethodsNamed(ScriptClass.InitDefaults)));
    }

    private static ScriptInstruction[] Code(ScriptFunction function) => [.. ScriptDisassembler.Disassemble(function).Where(instruction => instruction.Name != ReferenceCheck)];
}
