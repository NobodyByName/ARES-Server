namespace Ares.Structs.Backend.Bootstrap;

/// <summary>Request paths of the client bootstrap API.</summary>
public static class BootstrapPaths
{
    /// <summary><c>POST</c> <see cref="ManifestRequest"/>; reply <see cref="ManifestResponse"/>.</summary>
    public const string Manifest = "/v1/shared/manifest";

    /// <summary><c>POST</c> <see cref="LatestBuildRequest"/>; reply <see cref="LatestBuildResponse"/>.</summary>
    public const string LatestBuild = "/v1/shared/build/v2/latest";
}
