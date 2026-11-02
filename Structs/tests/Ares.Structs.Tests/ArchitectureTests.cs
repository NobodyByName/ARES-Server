using System.Reflection;

namespace Ares.Structs.Tests;

/// <summary>
/// Checks the compiled assemblies of the Structs component only reference what its boundary allows.
/// The build already rejects disallowed project/package references; this also catches
/// framework assemblies (e.g. hosting or logging) that a boundary excludes.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly string[] s_expected = ["Ares.Structs.Core", "Ares.Structs.Net", "Ares.Structs.Game", "Ares.Structs.Backend"];
    private static readonly string[] s_allowedPrefixes = ["System", "netstandard", "mscorlib", "Ares.Structs."];

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
        Assert.True(outside.Length == 0, $"{name} references assemblies outside the Structs component's boundary: {string.Join(", ", outside)}");
    }

    private static string PathOf(string name) => Path.Combine(AppContext.BaseDirectory, name + ".dll");
}
