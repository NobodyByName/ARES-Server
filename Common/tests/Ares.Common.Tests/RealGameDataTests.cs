using Ares.Common.Testing;

namespace Ares.Common.Tests;

/// <summary>
/// The real-data tier marked for a whole class: the constructor calls <see cref="RealGameData.Require"/>, which skips
/// every test of the class when <see cref="RealGameData.DirectoryVariable"/> is not set.
/// </summary>
public sealed class RealGameDataTests
{
    private readonly GameData.GameData _data = RealGameData.Require();

    [Fact]
    public void The_data_is_loaded_once_from_the_configured_directory()
    {
        Assert.Same(_data, RealGameData.Data);
        Assert.Equal(Path.GetFullPath(RealGameData.Directory!), _data.Source);
    }
}
