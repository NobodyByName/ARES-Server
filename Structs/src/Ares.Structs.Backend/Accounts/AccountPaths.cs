namespace Ares.Structs.Backend.Accounts;

/// <summary>Request paths of the account API.</summary>
public static class AccountPaths
{
    /// <summary><c>POST</c>; reply <see cref="PersistentPlayerKeysResponse"/>.</summary>
    public const string GetPersistentPlayerKeys = "/v1/shared/get-persistent-player-keys";

    /// <summary><c>POST</c> <see cref="SetPersistentPlayerKeysRequest"/>; reply <see cref="PersistentPlayerKeysResponse"/>.</summary>
    public const string SetPersistentPlayerKeys = "/v1/shared/set-persistent-player-keys";
}
