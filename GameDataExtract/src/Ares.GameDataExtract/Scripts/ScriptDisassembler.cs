namespace Ares.GameDataExtract.Scripts;

/// <summary>
/// One decoded bytecode instruction: its position (in 32-bit words from the function start), kind and words, opcode
/// word first.
/// </summary>
internal sealed record ScriptInstruction(int Position, ScriptOpcode Opcode, uint[] Words)
{
    public string Name => Opcode.Name;

    /// <summary>The 16-bit argument in the upper half of the opcode word (a frame variable, member offset or stack offset).</summary>
    public short WordArgument => (short)(Words[0] >> 16);

    /// <summary>The second 16-bit argument (low half of the second word), of the <c>…ReadWord</c> layouts.</summary>
    public short SecondWordArgument => (short)Words[1];

    /// <summary>The 32-bit word at <paramref name="index"/> after the opcode word.</summary>
    public uint DoubleWord(int index) => Words[1 + index];

    /// <summary>The 64-bit value (constant or cook-time key) in the two words after the opcode word.</summary>
    public ulong QuadWord => Words[1] | ((ulong)Words[2] << 32);
}

/// <summary>Splits a script function's bytecode into instructions (no execution).</summary>
internal static class ScriptDisassembler
{
    /// <summary>The instructions of <paramref name="function"/>, in order.</summary>
    /// <exception cref="InvalidDataException">An opcode is undefined or an instruction runs past the end.</exception>
    public static IReadOnlyList<ScriptInstruction> Disassemble(ScriptFunction function) => Disassemble(function.Bytecode, function.Name);

    /// <summary>The instructions of <paramref name="bytecode"/>; <paramref name="name"/> names it in errors.</summary>
    /// <exception cref="InvalidDataException">An opcode is undefined or an instruction runs past the end.</exception>
    public static IReadOnlyList<ScriptInstruction> Disassemble(uint[] bytecode, string name)
    {
        List<ScriptInstruction> instructions = [];
        int position = 0;
        while (position < bytecode.Length)
        {
            int code = (int)(bytecode[position] & 0xFF);
            ScriptOpcode opcode = ScriptOpcodes.Find(code)
                ?? throw new InvalidDataException($"{name}: undefined opcode {code} at {position}");
            if (position + opcode.Size > bytecode.Length)
            {
                throw new InvalidDataException($"{name}: {opcode.Name} at {position} does not fit the bytecode ({bytecode.Length} words)");
            }

            instructions.Add(new ScriptInstruction(position, opcode, bytecode[position..(position + opcode.Size)]));
            position += opcode.Size;
        }

        return instructions;
    }
}
