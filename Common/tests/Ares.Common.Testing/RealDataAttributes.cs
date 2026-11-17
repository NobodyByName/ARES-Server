using System.Runtime.CompilerServices;

namespace Ares.Common.Testing;

/// <summary>
/// A fact of the real-data tier: skipped with <see cref="RealGameData.SkipReason"/> unless
/// <see cref="RealGameData.DirectoryVariable"/> is set. Read the data with <see cref="RealGameData.Data"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RealDataFactAttribute : FactAttribute
{
    /// <summary>Creates the attribute.</summary>
    public RealDataFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = RealGameData.SkipReason;
        SkipType = typeof(RealGameData);
        SkipUnless = nameof(RealGameData.IsConfigured);
    }
}

/// <summary>
/// A theory of the real-data tier: skipped with <see cref="RealGameData.SkipReason"/> unless
/// <see cref="RealGameData.DirectoryVariable"/> is set. Read the data with <see cref="RealGameData.Data"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RealDataTheoryAttribute : TheoryAttribute
{
    /// <summary>Creates the attribute.</summary>
    public RealDataTheoryAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = RealGameData.SkipReason;
        SkipType = typeof(RealGameData);
        SkipUnless = nameof(RealGameData.IsConfigured);
    }
}
