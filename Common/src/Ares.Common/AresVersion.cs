using System.Reflection;

namespace Ares.Common;

/// <summary>The ARES release version: <c>YYYYMMDDNN</c>, set as <c>AresVersion</c> in <c>Directory.Build.props</c>.</summary>
public static class AresVersion
{
    /// <summary>This build's version.</summary>
    public static string Current { get; } =
        typeof(AresVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
}
