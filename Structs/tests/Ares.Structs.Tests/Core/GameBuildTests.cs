using System.Text;
using Ares.Structs.Core;

namespace Ares.Structs.Tests.Core;

public sealed class GameBuildTests
{
    [Fact]
    public void Network_checksum_matches_the_build_identity()
    {
        string identity = $"Embark {GameBuild.ProjectName}, BuildID: {GameBuild.BuildIdentifier}, "
            + $"EngineNetVer: {GameBuild.EngineNetworkVersion}, GameNetVer: {GameBuild.GameNetworkVersion}";

        // The engine folds each character into four CRC input bytes.
        Assert.Equal(GameBuild.NetworkChecksum, Crc32(Encoding.UTF32.GetBytes(identity.ToLowerInvariant())));
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte value in data)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }

        return ~crc;
    }
}
