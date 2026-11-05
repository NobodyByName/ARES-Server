namespace Ares.Structs.Core;

/// <summary>The game build these structures describe. Only the latest build is supported.</summary>
public static class GameBuild
{
    /// <summary>Steam build id.</summary>
    public const int SteamBuildId = 26439197;

    /// <summary>Project name in the network compatibility string.</summary>
    public const string ProjectName = "PioneerGame";

    /// <summary>Build identifier in the network compatibility string.</summary>
    public const string BuildIdentifier = "CL1450810-644C5505-BK4962";

    /// <summary>Engine network version.</summary>
    public const uint EngineNetworkVersion = 43;

    /// <summary>Game network version.</summary>
    public const uint GameNetworkVersion = 0;

    /// <summary>Network compatibility checksum of the identity above, checked in the handshake.</summary>
    public const uint NetworkChecksum = 0x91C089DCU;

    /// <summary>
    /// SHA-256 (lowercase hex) of the archive keys: the 32-byte pak key followed by the eight container
    /// key words, little-endian. Identifies the keys without containing them.
    /// </summary>
    public const string ArchiveKeysSha256 = "675e3cb86244aa3c457948321a46f1ad7081d1dda725fd1911537b0a127a3f28";
}
