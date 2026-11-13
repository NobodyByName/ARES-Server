using System.Reflection;

namespace Ares.Common.Tests;

/// <summary>
/// Checks the compiled assemblies of the Common component only reference what its boundary allows.
/// The build already rejects disallowed project/package references; this also catches
/// framework assemblies (e.g. hosting or logging) that a boundary excludes.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly string[] s_expected = ["Ares.Common", "Ares.Common.Security", "Ares.Common.Control", "Ares.Common.Control.Server", "Ares.Common.Game", "Ares.Common.Maps"];
    private static readonly string[] s_allowedPrefixes = ["System", "netstandard", "mscorlib", "Microsoft.Extensions.", "Microsoft.AspNetCore.", "Ares.Structs.", "Ares.Common"];

    public static TheoryData<string> Assemblies => new(s_expected);

    [Fact]
    public void All_component_assemblies_are_built()
    {
        foreach (string name in s_expected)
        {
            Assert.True(File.Exists(PathOf(name)), $"Missing assembly {name}.dll");
        }
    }

    [Theory]
    [MemberData(nameof(Assemblies))]
    public void Assembly_references_stay_inside_the_boundary(string name)
    {
        AssemblyName[] references = Assembly.LoadFrom(PathOf(name)).GetReferencedAssemblies();
        string[] outside =
        [
            .. references
                .Select(reference => reference.Name ?? "")
                .Where(reference => !s_allowedPrefixes.Any(prefix => reference.StartsWith(prefix, StringComparison.Ordinal))),
        ];
        Assert.True(outside.Length == 0, $"{name} references assemblies outside the Common component's boundary: {string.Join(", ", outside)}");
    }

    private static string PathOf(string name) => Path.Combine(AppContext.BaseDirectory, name + ".dll");
}
