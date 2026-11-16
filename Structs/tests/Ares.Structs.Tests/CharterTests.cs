using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Ares.Structs.Tests;

/// <summary>
/// Checks the compiled Structs assemblies use no files, sockets, clocks, timers, threads or console: the
/// framework assemblies those live in are allowed for other reasons, so this scans the types and members they reference.
/// </summary>
public sealed class CharterTests
{
    private const string DeniedNamespace = "System.Net.Sockets";

    private static readonly HashSet<string> s_deniedTypes =
    [
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.FileStream",
        "System.Diagnostics.Stopwatch",
        "System.Threading.Thread",
        "System.Console",
    ];

    private static readonly HashSet<string> s_deniedMembers =
    [
        "System.DateTime.get_Now",
        "System.DateTime.get_UtcNow",
        "System.DateTime.get_Today",
        "System.DateTimeOffset.get_Now",
        "System.DateTimeOffset.get_UtcNow",
        "System.Threading.Tasks.Task.Run",
        "System.Environment.get_TickCount",
        "System.Environment.get_TickCount64",
        "System.Random.get_Shared",
    ];

    [Theory]
    [MemberData(nameof(ArchitectureTests.Assemblies), MemberType = typeof(ArchitectureTests))]
    public void Assembly_uses_nothing_the_charter_excludes(string name)
    {
        string[] used = DeniedUses(name);
        Assert.True(used.Length == 0, $"{name} uses what Structs must not: {string.Join(", ", used)}");
    }

    [Fact]
    public void The_scan_finds_a_denied_use()
    {
        Assert.Contains("System.IO.File", DeniedUses("Ares.Structs.Tests"));
    }

    private static string[] DeniedUses(string name)
    {
        using FileStream stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, name + ".dll"));
        using PEReader image = new(stream);
        MetadataReader metadata = image.GetMetadataReader();
        IEnumerable<string> types = metadata.TypeReferences
            .Select(handle => TypeName(metadata, handle))
            .Where(type => s_deniedTypes.Contains(type) || type.StartsWith(DeniedNamespace + ".", StringComparison.Ordinal));
        IEnumerable<string> members = metadata.MemberReferences
            .Select(metadata.GetMemberReference)
            .Where(member => member.Parent.Kind == HandleKind.TypeReference)
            .Select(member => TypeName(metadata, (TypeReferenceHandle)member.Parent) + "." + metadata.GetString(member.Name))
            .Where(s_deniedMembers.Contains);
        return [.. types.Concat(members).Distinct()];
    }

    private static string TypeName(MetadataReader metadata, TypeReferenceHandle handle)
    {
        TypeReference type = metadata.GetTypeReference(handle);
        return metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name);
    }
}
