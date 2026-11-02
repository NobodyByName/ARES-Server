namespace Ares.Structs.Tests.Net;

/// <summary>Gameplay-tag queries and their results on tag sets.</summary>
internal static class GameplayTagQueryVectors
{
    /// <summary>Dictionary of the query vectors.</summary>
    public static IReadOnlyList<string> Dictionary { get; } = ["Spawn.Player", "Spawn.Player.Squad", "Zone.North", "Zone.South", "Disabled"];

    /// <summary>Tag sets the queries are evaluated on.</summary>
    public static IReadOnlyList<string[]> TagSets { get; } =
    [
        [],
        ["Spawn.Player"],
        ["spawn.player.squad.a"],
        ["Zone.North", "Spawn.Player"],
        ["Zone.South", "Disabled"],
        ["Zone.Northern"],
        ["Spawn.Player.Squad", "Zone.South"],
    ];

    /// <summary>Token stream (hex), then the result on each tag set.</summary>
    public static IReadOnlyList<(byte[] Tokens, bool[] Results)> Queries { get; } =
    [
        (Convert.FromHexString(""), [false, false, false, false, false, false, false]),
        (Convert.FromHexString("0000"), [false, false, false, false, false, false, false]),
        (Convert.FromHexString("0001010100"), [false, true, true, true, false, false, true]),
        (Convert.FromHexString("000102020002"), [false, false, false, true, false, false, false]),
        (Convert.FromHexString("0001030104"), [true, true, true, true, false, true, true]),
        (Convert.FromHexString("000101020203"), [false, false, false, true, true, false, true]),
        (Convert.FromHexString("000105020101000601030104"), [false, false, false, false, false, false, false]),
        (Convert.FromHexString("000104020202020301020103"), [false, false, true, false, true, false, true]),
        (Convert.FromHexString("00010601010102"), [true, true, true, false, true, true, true]),
        (Convert.FromHexString("0001040105010601030104"), [false, false, false, false, true, false, false]),
        (Convert.FromHexString("00010100"), [false, false, false, false, false, false, false]),
        (Convert.FromHexString("00010400"), [false, false, false, false, false, false, false]),
        (Convert.FromHexString("00010500"), [true, true, true, true, true, true, true]),
        (Convert.FromHexString("00010600"), [true, true, true, true, true, true, true]),
        (Convert.FromHexString("00010200"), [true, true, true, true, true, true, true]),
        (Convert.FromHexString("00010300"), [true, true, true, true, true, true, true]),
    ];

    /// <summary>Malformed queries: dictionary, tokens, what is wrong.</summary>
    public static IReadOnlyList<(string[] Dictionary, byte[] Tokens, string Reason)> Malformed { get; } =
    [
        (["Spawn.Player", "Spawn.Player.Squad", "Zone.North", "Zone.South", "Disabled"], Convert.FromHexString("0100"), "unsupported token version"),
        (["Spawn.Player", "Spawn.Player.Squad", "Zone.North", "Zone.South", "Disabled"], Convert.FromHexString("0002"), "invalid root marker"),
        (["Spawn.Player", "Spawn.Player.Squad", "Zone.North", "Zone.South", "Disabled"], Convert.FromHexString("000000"), "trailing tokens"),
        (["Spawn.Player", "Spawn.Player.Squad", "Zone.North", "Zone.South", "Disabled"], Convert.FromHexString("0001010200"), "truncated query"),
        (["Spawn.Player", "Spawn.Player.Squad", "Zone.North", "Zone.South", "Disabled"], Convert.FromHexString("0001010109"), "dictionary index out of range"),
        (["Spawn.Player", "Spawn.Player.Squad", "Zone.North", "Zone.South", "Disabled"], Convert.FromHexString("00010700"), "unsupported expression"),
        (["A", "a"], Convert.FromHexString("0000"), "duplicate dictionary tag"),
        (["bad tag"], Convert.FromHexString("0000"), "invalid tag name"),
        (["None"], Convert.FromHexString("0000"), "None tag"),
        (["Spawn.Player", "Spawn.Player.Squad", "Zone.North", "Zone.South", "Disabled"], Convert.FromHexString("000104010401040104010401040104010401040104010401040104010401040104010401040104010401040104010401040104010401040104010401040104010401040104010100"), "nesting too deep"),
    ];
}
