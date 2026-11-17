namespace Ares.GameDataExtract.Scripts;

/// <summary>
/// The literal defaults a script class's code gives its members: the member stores and calls of the constructors and
/// <c>__InitDefaults</c> along its script class chain, base class first, so a derived class's store replaces its base's.
/// </summary>
internal sealed record ScriptClassDefaults(IReadOnlyDictionary<int, ScriptStore> Stores, IReadOnlyList<ScriptCall> Calls, bool IsComplete, bool HasBranches)
{
    /// <summary>Evaluates the defaults of script class <paramref name="name"/>.</summary>
    /// <exception cref="InvalidDataException">The class is missing or ambiguous, or its bytecode is malformed.</exception>
    public static ScriptClassDefaults Of(ScriptCache cache, string name)
    {
        cache.Class(name);
        SortedDictionary<int, ScriptStore> stores = [];
        List<ScriptCall> calls = [];
        bool complete = true;
        bool branches = false;
        foreach (ScriptClass type in cache.Chain(name, out _).Reverse())
        {
            foreach (ScriptFunction function in type.Constructors.Concat(type.MethodsNamed(ScriptClass.InitDefaults)))
            {
                ScriptStatements statements = ScriptStatements.Evaluate(cache, function);
                foreach (ScriptStore store in statements.Stores)
                {
                    stores[store.Offset] = store;
                }

                calls.AddRange(statements.Calls);
                complete &= statements.IsComplete;
                branches |= statements.HasBranches;
            }
        }

        return new ScriptClassDefaults(stores, calls, complete, branches);
    }

    /// <summary>The literal stored last into the member at <paramref name="offset"/>, or <see langword="null"/>.</summary>
    public ScriptConstant? Constant(int offset) => Stores.GetValueOrDefault(offset)?.Constant;
}
