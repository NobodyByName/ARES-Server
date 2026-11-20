namespace Ares.Structs.Game.Scripts;

/// <summary>Script classes, structs and enums of the current game build.</summary>
public static class ScriptTypes
{
    /// <summary>
    /// Script classes and structs by name with prefix (<c>U</c>, <c>A</c> or <c>F</c>): Unreal super class and
    /// reflected members in declaration order (see <see cref="ScriptTypeSchemas"/> for their property chains).
    /// </summary>
    public static IReadOnlyDictionary<string, ScriptTypeDefinition> ByName { get; } =
        CreateTypes().ToDictionary(type => type.Name, StringComparer.Ordinal);

    /// <summary>Script enums by name, with their entry names and values in declaration order.</summary>
    public static IReadOnlyDictionary<string, ScriptEnumDefinition> Enums { get; } =
        CreateEnums().ToDictionary(type => type.Name, StringComparer.Ordinal);

    private static ScriptTypeDefinition[] CreateTypes() =>
        [
            .. ScriptTypesPart0.Entries,
            .. ScriptTypesPart1.Entries,
            .. ScriptTypesPart2.Entries,
            .. ScriptTypesPart3.Entries,
            .. ScriptTypesPart4.Entries,
            .. ScriptTypesPart5.Entries,
            .. ScriptTypesPart6.Entries,
            .. ScriptTypesPart7.Entries,
            .. ScriptTypesPart8.Entries,
            .. ScriptTypesPart9.Entries,
            .. ScriptTypesPart10.Entries,
            .. ScriptTypesPart11.Entries,
            .. ScriptTypesPart12.Entries,
            .. ScriptTypesPart13.Entries,
            .. ScriptTypesPart14.Entries,
            .. ScriptTypesPart15.Entries,
            .. ScriptTypesPart16.Entries,
            .. ScriptTypesPart17.Entries,
            .. ScriptTypesPart18.Entries,
            .. ScriptTypesPart19.Entries,
            .. ScriptTypesPart20.Entries,
            .. ScriptTypesPart21.Entries,
            .. ScriptTypesPart22.Entries,
            .. ScriptTypesPart23.Entries,
            .. ScriptTypesPart24.Entries,
            .. ScriptTypesPart25.Entries,
            .. ScriptTypesPart26.Entries,
            .. ScriptTypesPart27.Entries,
            .. ScriptTypesPart28.Entries,
            .. ScriptTypesPart29.Entries,
            .. ScriptTypesPart30.Entries,
            .. ScriptTypesPart31.Entries,
            .. ScriptTypesPart32.Entries,
            .. ScriptTypesPart33.Entries,
            .. ScriptTypesPart34.Entries,
            .. ScriptTypesPart35.Entries,
            .. ScriptTypesPart36.Entries,
            .. ScriptTypesPart37.Entries,
            .. ScriptTypesPart38.Entries,
            .. ScriptTypesPart39.Entries,
            .. ScriptTypesPart40.Entries,
            .. ScriptTypesPart41.Entries,
        ];

    private static ScriptEnumDefinition[] CreateEnums() =>
        [
            .. ScriptEnumsPart0.Entries,
            .. ScriptEnumsPart1.Entries,
        ];
}
