using Ares.Common.GameData;

namespace Ares.Common.Testing;

/// <summary>
/// The real-data test tier: the game data ARES-GameDataExtract wrote from the installed game, in the directory that
/// <see cref="DirectoryVariable"/> names (<c>tools/check.sh</c> sets it locally). It is loaded once per test run and shared.
/// Without the variable, real-data tests are skipped: mark them with <see cref="RealDataFactAttribute"/> or
/// <see cref="RealDataTheoryAttribute"/>, or call <see cref="Require"/> first (also from a test class's constructor, which
/// skips each of its tests).
/// </summary>
public static class RealGameData
{
    /// <summary>Environment variable naming the extracted game data directory.</summary>
    public const string DirectoryVariable = "ARES_GAMEDATA_DIR";

    /// <summary>Why a real-data test is skipped.</summary>
    public const string SkipReason =
        $"Real-data test: set {DirectoryVariable} to a game data directory written by ARES-GameDataExtract extract to run it.";

    private static readonly Lazy<GameDataSet> s_data = new(() => GameDataSet.Load(Directory!), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The configured directory, or null when <see cref="DirectoryVariable"/> is not set.</summary>
    public static string? Directory => Environment.GetEnvironmentVariable(DirectoryVariable) is { Length: > 0 } directory ? directory : null;

    /// <summary>Whether a directory is configured, so that real-data tests run.</summary>
    public static bool IsConfigured => Directory is not null;

    /// <summary>
    /// The loaded data. Loading fails the test (it is not skipped) when the configured directory cannot be used; read it
    /// only from a real-data test.
    /// </summary>
    /// <exception cref="InvalidOperationException">No directory is configured.</exception>
    /// <exception cref="GameDataException">The configured directory cannot be used.</exception>
    public static GameDataSet Data => IsConfigured ? s_data.Value
        : throw new InvalidOperationException($"{DirectoryVariable} is not set; mark the test as a real-data test.");

    /// <summary>The loaded data; skips the calling test when no directory is configured.</summary>
    public static GameDataSet Require()
    {
        Assert.SkipUnless(IsConfigured, SkipReason);
        return Data;
    }
}
