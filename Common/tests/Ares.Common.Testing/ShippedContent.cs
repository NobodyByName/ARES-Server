using Ares.Common.Game.Content;

namespace Ares.Common.Testing;

/// <summary>The ARES content the repository ships (<c>content/</c>, copied next to the tests), loaded once per test run.</summary>
public static class ShippedContent
{
    private static readonly Lazy<AresContent> s_content = new(() => AresContent.Load(Directory), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The shipped content folder next to the tests.</summary>
    public static string Directory => Path.Combine(AppContext.BaseDirectory, "content");

    /// <summary>The loaded content.</summary>
    public static AresContent Value => s_content.Value;

    /// <summary>Copies the shipped content into a folder, so a test can change some of its files.</summary>
    /// <param name="target">The folder; created when missing.</param>
    public static void CopyTo(string target)
    {
        System.IO.Directory.CreateDirectory(target);
        foreach (string file in AresContent.FileNames)
        {
            File.Copy(Path.Combine(Directory, file), Path.Combine(target, file), overwrite: true);
        }
    }
}
