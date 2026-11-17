namespace Ares.GameDataExtract.Scripts;

/// <summary>
/// What a script function's straight-line code does with literals: the member stores into the object it runs on
/// (<c>SetV</c> into a variable, <c>LoadThisR</c>, <c>WRTV</c>) and every call with its object and arguments (literals,
/// members, locals built by constructor calls). That covers constructors, <c>__InitDefaults</c> (class-body default
/// statements such as <c>Map.Add(key, value)</c> or <c>Array.Add(FStruct(a, b))</c>) and global initializers. The
/// evaluation is symbolic and walks the code in order without taking jumps: <see cref="HasBranches"/> says the
/// function has any, and <see cref="IsComplete"/> is false when the evaluation lost track of the stack, so callers
/// that need exact values check both and the shapes they expect.
/// </summary>
internal sealed class ScriptStatements
{
    private const string ConstructBehaviour = "$beh0";
    private const string ListConstructBehaviour = "$beh1";

    private static readonly HashSet<string> s_jumps =
        ["JMP", "JZ", "JNZ", "JS", "JNS", "JP", "JNP", "JMPP", "JLowZ", "JLowNZ", "JNullR", "JNonNullR"];

    private static readonly Dictionary<string, Func<ScriptConstant, ScriptConstant>> s_conversions = new(StringComparer.Ordinal)
    {
        ["iTOf"] = c => ScriptConstant.Of((float)c.AsInt32),
        ["uTOf"] = c => ScriptConstant.Of((uint)c.Bits),
        ["fTOi"] = c => ScriptConstant.Of((int)c.AsSingle),
        ["dTOi"] = c => ScriptConstant.Of((int)c.AsDouble),
        ["dTOf"] = c => ScriptConstant.Of((float)c.AsDouble),
        ["iTOd"] = c => ScriptConstant.Of((double)c.AsInt32),
        ["uTOd"] = c => ScriptConstant.Of((double)(uint)c.Bits),
        ["fTOd"] = c => ScriptConstant.Of((double)c.AsSingle),
        ["i64TOd"] = c => ScriptConstant.Of((double)(long)c.Bits),
        ["i64TOf"] = c => ScriptConstant.Of((long)c.Bits),
    };

    private readonly ScriptCache _cache;
    private readonly List<Slot> _stack = [];
    private readonly Dictionary<short, ScriptValue> _variables = [];
    private readonly List<ScriptStore> _stores = [];
    private readonly List<ScriptCall> _calls = [];
    private ScriptValue? _address;
    private ScriptValue? _returned;

    private ScriptStatements(ScriptCache cache, IReadOnlyList<ScriptInstruction> instructions)
    {
        _cache = cache;
        Instructions = instructions;
        IsComplete = true;
        foreach (ScriptInstruction instruction in instructions)
        {
            Step(instruction);
        }
    }

    private IReadOnlyList<ScriptInstruction> Instructions { get; }

    /// <summary>Member stores in code order.</summary>
    public IReadOnlyList<ScriptStore> Stores => _stores;

    /// <summary>Calls in code order.</summary>
    public IReadOnlyList<ScriptCall> Calls => _calls;

    /// <summary>Whether the function has jumps (the evaluation ignores them).</summary>
    public bool HasBranches { get; private set; }

    /// <summary>False when a call's function reference was missing or the stack ran out, so later values may be wrong.</summary>
    public bool IsComplete { get; private set; }

    /// <summary>The last store into each member, by member offset in ascending order.</summary>
    public IReadOnlyDictionary<int, ScriptStore> MemberStores
    {
        get
        {
            SortedDictionary<int, ScriptStore> stores = [];
            foreach (ScriptStore store in _stores)
            {
                stores[store.Offset] = store;
            }

            return stores;
        }
    }

    /// <summary>Offsets of the members the function takes the address of (<c>PshThisMemRef</c>), e.g. to construct them.</summary>
    public IReadOnlyList<int> ReferencedMembers =>
        [.. Instructions.Where(instruction => instruction.Name == "PshThisMemRef").Select(instruction => (int)(ushort)instruction.WordArgument).Distinct()];

    /// <summary>Evaluates <paramref name="function"/>.</summary>
    /// <exception cref="InvalidDataException">The bytecode cannot be disassembled.</exception>
    public static ScriptStatements Evaluate(ScriptCache cache, ScriptFunction function)
    {
        return new ScriptStatements(cache, ScriptDisassembler.Disassemble(function));
    }

    private void Step(ScriptInstruction instruction)
    {
        short word = instruction.WordArgument;
        switch (instruction.Name)
        {
            case "SetV1":
                _variables[word] = new ScriptConstant(instruction.DoubleWord(0) & 0xFF, 1);
                break;
            case "SetV2":
                _variables[word] = new ScriptConstant(instruction.DoubleWord(0) & 0xFFFF, 2);
                break;
            case "SetV4":
                _variables[word] = new ScriptConstant(instruction.DoubleWord(0), 4);
                break;
            case "SetV8":
                _variables[word] = new ScriptConstant(instruction.QuadWord, 8);
                break;
            case "PshC4":
                Push(new ScriptConstant(instruction.DoubleWord(0), 4), 1);
                break;
            case "PshC8":
                Push(new ScriptConstant(instruction.QuadWord, 8), 2);
                break;
            case "PshV1" or "PshV4":
                Push(Variable(word), 1);
                break;
            case "PshV8":
                Push(Variable(word), 2);
                break;
            case "PSF":
                _stack.Add(new Slot(new ScriptVariable(word), 2, word));
                break;
            case "PshVPtr":
                Push(word == 0 ? ScriptThis.Instance : Variable(word), 2);
                break;
            case "PshThisMemRef" or "PshThisMemObj":
                Push(new ScriptMember(ScriptThis.Instance, (ushort)word, instruction.Name == "PshThisMemObj"), 2);
                break;
            case "PGA":
                Push(new ScriptGlobalAddress(_cache.GlobalByKey((long)instruction.QuadWord)), 2);
                break;
            case "ADDSi":
            {
                Slot owner = Pop();
                Push(new ScriptMember(Resolve(owner), (ushort)word, false), owner.Size);
                break;
            }
            case "LoadThisR":
                _address = new ScriptMember(ScriptThis.Instance, (ushort)word, false);
                break;
            case "WRTV1" or "WRTV2" or "WRTV4" or "WRTV8":
                if (_address is ScriptMember { Owner: ScriptThis, IsObject: false } member)
                {
                    _stores.Add(new ScriptStore(instruction.Position, member.Offset, instruction.Name[4] - '0', Variable(word)));
                }

                break;
            case "CpyRtoV4" or "CpyRtoV8":
                _variables[word] = _returned ?? new ScriptUnknown(instruction.Name);
                break;
            case "CpyVtoV4" or "CpyVtoV8":
                _variables[word] = Variable(instruction.SecondWordArgument);
                break;
            case "CALL" or "CALLINTF" or "CALLBND":
                Call(instruction, _cache.FunctionById((int)instruction.DoubleWord(0)), objectOnTop: true);
                break;
            case "CALLSYS" or "Thiscall1":
                Call(instruction, _cache.FunctionByKey((long)instruction.QuadWord), objectOnTop: true);
                break;
            case "ALLOC":
                Call(instruction, _cache.FunctionById((int)instruction.DoubleWord(2)), objectOnTop: false);
                break;
            case "RET":
                break;
            default:
                Other(instruction);
                break;
        }
    }

    private void Other(ScriptInstruction instruction)
    {
        ScriptOpcode opcode = instruction.Opcode;
        if (s_conversions.TryGetValue(opcode.Name, out Func<ScriptConstant, ScriptConstant>? convert))
        {
            ScriptValue source = Variable(instruction.SecondWordArgument);
            _variables[instruction.WordArgument] = source is ScriptConstant constant ? convert(constant) : new ScriptConverted(source, opcode.Name);
            return;
        }

        if (s_jumps.Contains(opcode.Name))
        {
            HasBranches = true;
        }

        if (opcode.WritesVariable)
        {
            _variables[instruction.WordArgument] = new ScriptUnknown(opcode.Name);
        }

        switch (opcode.StackChange)
        {
            case null:
                Lost();
                break;
            case > 0:
                Push(new ScriptUnknown(opcode.Name), opcode.StackChange.Value);
                break;
            case < 0:
                for (int size = -opcode.StackChange.Value; size > 0;)
                {
                    size -= Pop().Size;
                }

                break;
        }
    }

    private void Call(ScriptInstruction instruction, ScriptFunctionReference? function, bool objectOnTop)
    {
        if (function is null)
        {
            Lost();
            _calls.Add(new ScriptCall(instruction.Position, null, null, []));
            return;
        }

        Slot? target = function.IsMethod && objectOnTop ? Pop() : null;
        ScriptValue[] arguments = new ScriptValue[function.Parameters.Count];
        for (int i = 0; i < arguments.Length; i++)
        {
            arguments[i] = Resolve(Pop());
        }

        if (function.IsMethod && !objectOnTop)
        {
            target = Pop();
        }

        // A constructor's object is the local it builds, whatever that local held before.
        short? built = target is { FrameVariable: { } local } && (!objectOnTop || IsConstructor(function)) ? local : null;
        ScriptValue? owner = built is { } variable ? new ScriptVariable(variable) : target is { } slot ? Resolve(slot) : null;
        ScriptCall call = new(instruction.Position, function, owner, arguments);
        _calls.Add(call);
        if (built is { } constructed)
        {
            _variables[constructed] = new ScriptConstructed(call);
        }

        _returned = new ScriptReturned(call);
    }

    /// <summary>
    /// Whether <paramref name="function"/> constructs its object: the construct behaviours (<c>$beh0</c>, and
    /// <c>$beh1</c> for list construction) or a script constructor (named like its type).
    /// </summary>
    private bool IsConstructor(ScriptFunctionReference function)
    {
        return function.Name is ConstructBehaviour or ListConstructBehaviour
            || _cache.References.Types.GetValueOrDefault(function.ObjectType)?.Name == function.Name;
    }

    private void Lost()
    {
        IsComplete = false;
        _stack.Clear();
        _returned = null;
    }

    private ScriptValue Variable(short slot) => _variables.TryGetValue(slot, out ScriptValue? value) ? value : new ScriptVariable(slot);

    /// <summary>A popped value; a pushed frame variable address reads the variable as it is now.</summary>
    private ScriptValue Resolve(Slot slot) => slot.FrameVariable is { } local ? Variable(local) : slot.Value;

    private void Push(ScriptValue value, int size) => _stack.Add(new Slot(value, size, null));

    private Slot Pop()
    {
        if (_stack.Count == 0)
        {
            IsComplete = false;
            return new Slot(new ScriptUnknown("empty stack"), 1, null);
        }

        Slot slot = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        return slot;
    }

    /// <summary>A stack entry: its value, size in 32-bit slots, and the frame variable when it is that variable's address.</summary>
    private readonly record struct Slot(ScriptValue Value, int Size, short? FrameVariable);
}
