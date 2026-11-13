using Ares.MapData.Packages;
using Ares.MapData.Profiles;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Components;

/// <summary>
/// The collision members an <c>FBodyInstance</c> block stores (members it does not store are <see langword="null"/>;
/// members stored as zero read as zero, the empty name or no overrides).
/// </summary>
/// <param name="ObjectType">Object type (collision channel of the body).</param>
/// <param name="CollisionEnabled">Collision enabled state.</param>
/// <param name="ProfileName">Collision profile name.</param>
/// <param name="Responses">Per-channel response overrides, in stored order.</param>
internal sealed record BodyInstanceValues(
    CollisionChannel? ObjectType,
    CollisionEnabled? CollisionEnabled,
    string? ProfileName,
    IReadOnlyList<CollisionResponseOverride>? Responses);

/// <summary>
/// Reads unversioned <c>FBodyInstance</c> blocks (see <see cref="BodyInstanceMembers"/>). Members stored as zero need no
/// known size; the strict header checks let it also search for a block's start.
/// </summary>
internal static class BodyInstanceReader
{
    private const int MaxResponses = 64;
    private const int WalkableSlopeMemberCount = 2;

    /// <summary>Reads the body instance at <paramref name="at"/>.</summary>
    /// <param name="package">The package.</param>
    /// <param name="at">Offset of the block's header.</param>
    /// <param name="end">Offset after the block.</param>
    /// <returns>The stored members, or <see langword="null"/> when the bytes are not a body instance this reader understands.</returns>
    public static BodyInstanceValues? TryRead(CookedPackage package, int at, out int end)
    {
        return TryRead(package, at, out end, out _);
    }

    /// <summary>
    /// The body instances that end exactly at <paramref name="end"/> and start at or after <paramref name="lowest"/>, each
    /// with its start offset; only readings that store at least one member count.
    /// </summary>
    public static IReadOnlyList<(int Start, BodyInstanceValues Values)> FindEndingAt(CookedPackage package, int lowest, int end)
    {
        List<(int Start, BodyInstanceValues Values)> hits = [];
        for (int start = end - 2; start >= lowest; start--)
        {
            if (TryRead(package, start, out int stop, out int listed) is { } values && stop == end && listed > 0)
            {
                hits.Add((start, values));
            }
        }

        return hits;
    }

    private static BodyInstanceValues? TryRead(CookedPackage package, int at, out int end, out int listed)
    {
        end = at;
        listed = 0;
        try
        {
            return Read(package, at, out end, out listed);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static BodyInstanceValues? Read(CookedPackage package, int at, out int end, out int listed)
    {
        end = at;
        listed = 0;
        if (UnversionedHeader.TryReadStrict(package.Data, at, BodyInstanceMembers.MemberCount) is not { } header)
        {
            return null;
        }

        listed = header.Members.Count;

        CollisionChannel? objectType = null;
        CollisionEnabled? collisionEnabled = null;
        string? profile = null;
        IReadOnlyList<CollisionResponseOverride>? responses = null;
        int offset = header.ValuesOffset;
        foreach (UnversionedMember member in header.Members)
        {
            switch (member.Index)
            {
                case BodyInstanceMembers.ObjectType:
                    objectType = (CollisionChannel)(member.IsNonZero ? package.ReadByte(offset) : 0);
                    break;
                case BodyInstanceMembers.CollisionEnabled:
                    collisionEnabled = (CollisionEnabled)(member.IsNonZero ? package.ReadByte(offset) : 0);
                    break;
                case BodyInstanceMembers.CollisionProfileName:
                    profile = member.IsNonZero ? package.ReadName(offset) : CookedPackage.NoneName;
                    break;
                case BodyInstanceMembers.CollisionResponses:
                    if (!member.IsNonZero)
                    {
                        responses = [];
                        continue;
                    }

                    if (ReadResponses(package, offset, out offset) is not { } read)
                    {
                        return null;
                    }

                    responses = read;
                    continue;
                case BodyInstanceMembers.WalkableSlopeOverride:
                    if (member.IsNonZero && !SkipWalkableSlope(package, offset, out offset))
                    {
                        return null;
                    }

                    continue;
                default:
                    if (member.IsNonZero && BodyInstanceMembers.FixedValueSize(member.Index) is null)
                    {
                        return null;
                    }

                    break;
            }

            if (member.IsNonZero)
            {
                offset += BodyInstanceMembers.FixedValueSize(member.Index) ?? 0;
            }
        }

        end = offset;
        return new BodyInstanceValues(objectType, collisionEnabled, profile, responses);
    }

    /// <summary>An <c>FCollisionResponse</c>: its response array, each entry a channel name and a response (block when not stored).</summary>
    private static List<CollisionResponseOverride>? ReadResponses(CookedPackage package, int at, out int end)
    {
        end = at;
        if (UnversionedHeader.TryReadStrict(package.Data, at, CollisionResponseMembers.MemberCount) is not { } header
            || header.Members.Any(member => member.Index != CollisionResponseMembers.ResponseArray))
        {
            return null;
        }

        int offset = header.ValuesOffset;
        List<CollisionResponseOverride> responses = [];
        if (!header.IsNonZero(CollisionResponseMembers.ResponseArray))
        {
            end = offset;
            return responses;
        }

        int count = package.ReadInt32(offset);
        offset += sizeof(int);
        if (count is < 0 or > MaxResponses)
        {
            return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (UnversionedHeader.TryReadStrict(package.Data, offset, ResponseChannelMembers.MemberCount) is not { } row
                || !row.IsNonZero(ResponseChannelMembers.Channel)
                || package.ReadUInt32(offset = row.ValuesOffset) >= package.Header.Names.Count
                || package.ReadUInt32(offset + 4) != 0)
            {
                return null;
            }

            string channel = package.ReadName(offset);
            offset += SceneComponentMembers.NameSize;
            CollisionResponse response = ResponseChannelMembers.DefaultResponse;
            if (row.Contains(ResponseChannelMembers.Response))
            {
                response = row.IsNonZero(ResponseChannelMembers.Response) ? (CollisionResponse)package.ReadByte(offset++) : CollisionResponse.Ignore;
            }

            int existing = responses.FindIndex(entry => entry.Channel == channel);
            if (existing >= 0)
            {
                responses[existing] = new CollisionResponseOverride(channel, response);
            }
            else
            {
                responses.Add(new CollisionResponseOverride(channel, response));
            }
        }

        end = offset;
        return responses;
    }

    /// <summary>Steps over an <c>FWalkableSlopeOverride</c> (a one-byte behaviour and a float angle).</summary>
    private static bool SkipWalkableSlope(CookedPackage package, int at, out int end)
    {
        end = at;
        if (UnversionedHeader.TryReadStrict(package.Data, at, WalkableSlopeMemberCount) is not { } header)
        {
            return false;
        }

        end = header.ValuesOffset + header.Members
            .Where(member => member.IsNonZero)
            .Sum(member => member.Index == WalkableSlopeOverrideMembers.WalkableSlopeBehavior ? 1 : sizeof(float));
        return true;
    }
}
