using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Replication;

namespace Ares.Common.Game.Identity;

/// <summary>Validates and freezes the squad profiles of a reservation.</summary>
public static class SquadProfiles
{
    /// <summary>Copies profiles for the reserved players; older requests may omit profiles.</summary>
    /// <param name="players">Reserved tenancy ids.</param>
    /// <param name="profiles">Profiles, or null for an older request.</param>
    /// <returns>A read-only copy ordered by tenancy id.</returns>
    public static IReadOnlyList<CompactPlayerProfile> ForPlayers(IReadOnlyList<long> players, IReadOnlyList<CompactPlayerProfile?>? profiles)
    {
        ArgumentNullException.ThrowIfNull(players);
        if (profiles is null)
        {
            return [];
        }

        CompactPlayerProfile[] copy = [.. profiles.OfType<CompactPlayerProfile>()];
        if (copy.Length != profiles.Count || copy.Length != players.Count || copy.Any(profile => profile.TenancyUserId is not { Value: > 0 and <= long.MaxValue })
            || !copy.Select(profile => (long)profile.TenancyUserId.Value).Order().SequenceEqual(players.Order()))
        {
            throw new InvalidDataException("Profiles do not match the reserved players");
        }

        try
        {
            foreach (CompactPlayerProfile profile in copy)
            {
                if (profile.DisplayName is null || profile.DisplayNameDiscriminator is null || profile.ThirdPartyUserId is null || profile.ThirdPartyLastSeenAccountName is null)
                {
                    throw new InvalidDataException("Profile strings cannot be null");
                }

                profile.Write(new BitWriter());
            }
        }
        catch (InvalidPacketException error)
        {
            throw new InvalidDataException("Profile cannot be serialized", error);
        }

        return Array.AsReadOnly<CompactPlayerProfile>([.. copy.OrderBy(profile => profile.TenancyUserId.Value)]);
    }
}
