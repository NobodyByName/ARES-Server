namespace Ares.Structs.Game.World;

/// <summary>Runtime state of a data layer (<c>EDataLayerRuntimeState</c>).</summary>
public enum DataLayerRuntimeState
{
    Unloaded = 0,
    Loaded = 1,
    Activated = 2,
}

/// <summary>An authored runtime data layer of a map. All current layers are roots (no parent).</summary>
/// <param name="Name">Layer instance name.</param>
/// <param name="Asset">Data layer asset path.</param>
/// <param name="InitialRuntimeState">State the layer starts in.</param>
public sealed record DataLayer(string Name, string Asset, DataLayerRuntimeState InitialRuntimeState);
