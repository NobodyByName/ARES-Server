namespace Ares.Structs.Net.GameplayTags;

/// <summary>A gameplay tag by name (dot-separated identifiers); the empty name is no tag.</summary>
/// <param name="Name">Tag name.</param>
public sealed record GameplayTag(string Name);
