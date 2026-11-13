using System.Text;
using Ares.Common.Archives;
using Ares.MapData.Archives;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Profiles;

/// <summary>
/// The game's collision channels and profiles and its default shape complexity, read from its config files, and what a
/// body's collision setting blocks.
/// </summary>
/// <remarks>
/// The config files are applied in order. <c>DefaultChannelResponses</c> names the game channels and their default
/// responses; <c>Profiles</c> defines profiles by name; <c>EditProfiles</c> changes responses of existing profiles
/// once every file is read; <c>ProfileRedirects</c> renames profiles. A profile's responses start from the channel
/// defaults (engine channels block) and take its custom responses, then its edits. A profile without a known object
/// type is <see cref="CollisionChannel.WorldStatic"/>. The last <see cref="CollisionProfileConfig.DefaultShapeComplexity"/>
/// line of the physics settings wins; a clearing line restores the engine default.
/// </remarks>
internal sealed class CollisionProfiles
{
    private const string ChannelResponsesKey = "DefaultChannelResponses";
    private const string ProfilesKey = "Profiles";
    private const string EditProfilesKey = "EditProfiles";
    private const string RedirectsKey = "ProfileRedirects";
    private const string NameMember = "Name";
    private const string ChannelMember = "Channel";
    private const string ResponseMember = "Response";
    private const string DefaultResponseMember = "DefaultResponse";
    private const string CollisionEnabledMember = "CollisionEnabled";
    private const string ObjectTypeMember = "ObjectTypeName";
    private const string CustomResponsesMember = "CustomResponses";
    private const string OldNameMember = "OldName";
    private const string NewNameMember = "NewName";
    private const string ChannelPrefix = "ECC_";
    private const string ResponsePrefix = "ECR_";
    private const int ChannelCount = (int)CollisionChannel.GameTraceChannel18 + 1;

    private readonly Dictionary<string, CollisionChannel> _channels;
    private readonly Dictionary<string, CollisionProfile> _profiles;
    private readonly Dictionary<string, string> _redirects;
    private readonly CollisionResponse[] _defaults;
    private readonly CollisionProfile _playerCapsule;
    private readonly CollisionChannel _climbable;

    private CollisionProfiles(Dictionary<string, CollisionChannel> channels, Dictionary<string, CollisionProfile> profiles,
        Dictionary<string, string> redirects, CollisionResponse[] defaults, CollisionTraceFlag defaultShapeComplexity)
    {
        DefaultShapeComplexity = defaultShapeComplexity;
        _channels = channels;
        _profiles = profiles;
        _redirects = redirects;
        _defaults = defaults;
        _playerCapsule = profiles.GetValueOrDefault(CollisionProfileConfig.PlayerCapsuleProfile)
            ?? throw new InvalidDataException($"The collision config has no {CollisionProfileConfig.PlayerCapsuleProfile} profile");
        _climbable = channels.TryGetValue(CollisionProfileConfig.ClimbableChannel, out CollisionChannel climbable)
            ? climbable
            : throw new InvalidDataException($"The collision config has no {CollisionProfileConfig.ClimbableChannel} channel");
    }

    /// <summary>Channels by name: the engine's object types and the game's named channels.</summary>
    public IReadOnlyDictionary<string, CollisionChannel> Channels => _channels;

    /// <summary>Profiles by name.</summary>
    public IReadOnlyDictionary<string, CollisionProfile> Profiles => _profiles;

    /// <summary>New profile names by old name.</summary>
    public IReadOnlyDictionary<string, string> Redirects => _redirects;

    /// <summary>Each channel's default response, indexed by <see cref="CollisionChannel"/>.</summary>
    public IReadOnlyList<CollisionResponse> DefaultResponses => _defaults;

    /// <summary>Which collision bodies whose trace flag is <see cref="CollisionTraceFlag.UseDefault"/> offer.</summary>
    public CollisionTraceFlag DefaultShapeComplexity { get; }

    /// <summary>Reads the collision config of the game installed at <paramref name="gameDirectory"/>.</summary>
    /// <exception cref="InvalidDataException">The config pak or the collision config is missing or malformed.</exception>
    public static CollisionProfiles Load(string gameDirectory, ArchiveKeys keys)
    {
        using PakFile pak = PakFile.Open(Path.Combine(gameDirectory, CollisionProfileConfig.ConfigPak), keys);
        return Load(pak);
    }

    /// <summary>Reads the collision config files from <paramref name="pak"/>.</summary>
    /// <exception cref="InvalidDataException">A config file is missing, or the collision config is malformed.</exception>
    public static CollisionProfiles Load(PakFile pak)
    {
        List<string> texts = [];
        foreach (string path in CollisionProfileConfig.ConfigFiles)
        {
            if (!pak.Contains(path))
            {
                throw new InvalidDataException($"The config pak has no {path}");
            }

            texts.Add(Encoding.UTF8.GetString(pak.Read(path)));
        }

        return Parse(texts);
    }

    /// <summary>Applies the collision sections of <paramref name="configTexts"/> in order.</summary>
    /// <exception cref="InvalidDataException">
    /// The collision config is malformed or lacks the player capsule profile or the climbable channel, or the default shape
    /// complexity is not a trace flag.
    /// </exception>
    public static CollisionProfiles Parse(IEnumerable<string> configTexts)
    {
        Dictionary<string, CollisionChannel> channels = new(StringComparer.OrdinalIgnoreCase);
        for (CollisionChannel channel = CollisionChannel.WorldStatic; channel <= CollisionChannel.Destructible; channel++)
        {
            channels[channel.ToString()] = channel;
        }

        Dictionary<string, (CollisionChannel Channel, CollisionResponse Response)> gameChannels = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ProfileEntry> entries = new(StringComparer.OrdinalIgnoreCase);
        List<(string Name, IReadOnlyList<CollisionResponseOverride> Responses)> edits = [];
        Dictionary<string, string> redirects = new(StringComparer.OrdinalIgnoreCase);
        CollisionTraceFlag shapeComplexity = CollisionProfileConfig.EngineDefaultShapeComplexity;
        foreach (string text in configTexts)
        {
            foreach (ConfigLine line in ConfigSection.Read(text, CollisionProfileConfig.PhysicsSection))
            {
                if (string.Equals(line.Key, CollisionProfileConfig.DefaultShapeComplexity, StringComparison.OrdinalIgnoreCase))
                {
                    shapeComplexity = line.Operation == ConfigOperation.Clear
                        ? CollisionProfileConfig.EngineDefaultShapeComplexity
                        : CollisionProfileConfig.ParseTraceFlag(line.Value)
                            ?? throw new InvalidDataException($"Invalid {CollisionProfileConfig.DefaultShapeComplexity} '{line.Value}'");
                }
            }

            foreach (ConfigLine line in ConfigSection.Read(text, CollisionProfileConfig.Section))
            {
                if (line.Operation == ConfigOperation.Clear)
                {
                    Clear(line.Key, gameChannels, entries, edits, redirects);
                    continue;
                }

                IReadOnlyDictionary<string, string> members = ConfigSection.Members(line.Value);
                bool add = line.Operation == ConfigOperation.Add;
                switch (line.Key)
                {
                    case ChannelResponsesKey when add:
                        gameChannels[Required(members, NameMember, line)] =
                            (ParseChannel(Required(members, ChannelMember, line)), ParseResponse(Required(members, DefaultResponseMember, line)));
                        break;
                    case ChannelResponsesKey:
                        gameChannels.Remove(Required(members, NameMember, line));
                        break;
                    case ProfilesKey when add:
                        entries[Required(members, NameMember, line)] = new ProfileEntry(
                            ParseCollisionEnabled(Required(members, CollisionEnabledMember, line)),
                            members.GetValueOrDefault(ObjectTypeMember, ""),
                            ParseResponses(members.GetValueOrDefault(CustomResponsesMember, "")));
                        break;
                    case ProfilesKey:
                        entries.Remove(Required(members, NameMember, line));
                        break;
                    case EditProfilesKey when add:
                        edits.Add((Required(members, NameMember, line), ParseResponses(members.GetValueOrDefault(CustomResponsesMember, ""))));
                        break;
                    case EditProfilesKey:
                        string removed = Required(members, NameMember, line);
                        edits.RemoveAll(edit => string.Equals(edit.Name, removed, StringComparison.OrdinalIgnoreCase));
                        break;
                    case RedirectsKey when add:
                        redirects[Required(members, OldNameMember, line)] = Required(members, NewNameMember, line);
                        break;
                    case RedirectsKey:
                        redirects.Remove(Required(members, OldNameMember, line));
                        break;
                }
            }
        }

        CollisionResponse[] defaults = new CollisionResponse[ChannelCount];
        Array.Fill(defaults, CollisionResponse.Block);
        foreach ((string name, (CollisionChannel channel, CollisionResponse response)) in gameChannels)
        {
            channels[name] = channel;
            defaults[(int)channel] = response;
        }

        Dictionary<string, CollisionProfile> profiles = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string name, ProfileEntry entry) in entries)
        {
            CollisionResponse[] responses = [.. defaults];
            ApplyOverrides(channels, responses, entry.Responses);
            foreach ((string editName, IReadOnlyList<CollisionResponseOverride> editResponses) in edits)
            {
                if (string.Equals(editName, name, StringComparison.OrdinalIgnoreCase))
                {
                    ApplyOverrides(channels, responses, editResponses);
                }
            }

            CollisionChannel objectType = channels.GetValueOrDefault(entry.ObjectTypeName, CollisionChannel.WorldStatic);
            profiles[name] = new CollisionProfile(name, entry.CollisionEnabled, objectType, responses);
        }

        return new CollisionProfiles(channels, profiles, redirects, defaults, shapeComplexity);
    }

    /// <summary>The profile named <paramref name="name"/> after redirects, or <see langword="null"/> when there is none.</summary>
    public CollisionProfile? Find(string? name)
    {
        return string.IsNullOrEmpty(name) ? null : _profiles.GetValueOrDefault(_redirects.GetValueOrDefault(name, name));
    }

    /// <summary>
    /// What a component's collision setting blocks. A known profile (after redirects) decides everything; otherwise
    /// the component's own setting applies: <paramref name="collisionEnabled"/>, <paramref name="objectType"/>
    /// (<see cref="CollisionChannel.WorldStatic"/> when absent) and the channel defaults with
    /// <paramref name="overrides"/> applied in order. Overrides for unknown channels are ignored.
    /// </summary>
    public CollisionClassification Classify(string? profileName, CollisionEnabled collisionEnabled, CollisionChannel? objectType,
        IReadOnlyList<CollisionResponseOverride> overrides)
    {
        CollisionProfile? profile = Find(profileName);
        if (profile is not null)
        {
            return Classify(profile.CollisionEnabled, profile.ObjectType, profile.Responses);
        }

        CollisionResponse[] responses = [.. _defaults];
        ApplyOverrides(_channels, responses, overrides);
        return Classify(collisionEnabled, objectType ?? CollisionChannel.WorldStatic, responses);
    }

    /// <summary>
    /// What a body with these settings blocks. It blocks the player's movement when it takes part in queries, blocks
    /// the player capsule's object type, and the player capsule's profile blocks the body's object type. It blocks
    /// vaulting when it takes part in queries and blocks the climbable channel. An object type outside the channel
    /// table counts as blocked by the player capsule.
    /// </summary>
    public CollisionClassification Classify(CollisionEnabled collisionEnabled, CollisionChannel objectType, IReadOnlyList<CollisionResponse> responses)
    {
        if (collisionEnabled is not (CollisionEnabled.QueryOnly or CollisionEnabled.QueryAndPhysics or CollisionEnabled.QueryAndProbe))
        {
            return new CollisionClassification(false, false);
        }

        bool blocksPawn = responses[(int)_playerCapsule.ObjectType] == CollisionResponse.Block
            && ((int)objectType >= ChannelCount || _playerCapsule.Responses[(int)objectType] == CollisionResponse.Block);
        return new CollisionClassification(blocksPawn, responses[(int)_climbable] == CollisionResponse.Block);
    }

    private static void Clear(string key, Dictionary<string, (CollisionChannel Channel, CollisionResponse Response)> gameChannels,
        Dictionary<string, ProfileEntry> entries, List<(string Name, IReadOnlyList<CollisionResponseOverride> Responses)> edits,
        Dictionary<string, string> redirects)
    {
        switch (key)
        {
            case ChannelResponsesKey:
                gameChannels.Clear();
                break;
            case ProfilesKey:
                entries.Clear();
                break;
            case EditProfilesKey:
                edits.Clear();
                break;
            case RedirectsKey:
                redirects.Clear();
                break;
        }
    }

    private static void ApplyOverrides(Dictionary<string, CollisionChannel> channels, CollisionResponse[] responses,
        IReadOnlyList<CollisionResponseOverride> overrides)
    {
        foreach (CollisionResponseOverride entry in overrides)
        {
            if (channels.TryGetValue(entry.Channel, out CollisionChannel channel))
            {
                responses[(int)channel] = entry.Response;
            }
        }
    }

    private static List<CollisionResponseOverride> ParseResponses(string list)
    {
        List<CollisionResponseOverride> responses = [];
        foreach (string element in ConfigSection.Elements(list))
        {
            IReadOnlyDictionary<string, string> members = ConfigSection.Members(element);
            if (!members.TryGetValue(ChannelMember, out string? channel))
            {
                throw new InvalidDataException($"Collision response without a channel: {element}");
            }

            responses.Add(members.TryGetValue(ResponseMember, out string? response)
                ? new CollisionResponseOverride(channel, ParseResponse(response))
                : new CollisionResponseOverride(channel));
        }

        return responses;
    }

    private static CollisionChannel ParseChannel(string text) => ParseName<CollisionChannel>(text, ChannelPrefix, "collision channel");

    private static CollisionResponse ParseResponse(string text) => ParseName<CollisionResponse>(text, ResponsePrefix, "collision response");

    private static CollisionEnabled ParseCollisionEnabled(string text) => ParseName<CollisionEnabled>(text, "", "collision enabled value");

    private static T ParseName<T>(string text, string prefix, string what)
        where T : struct, Enum
    {
        string name = text.StartsWith(prefix, StringComparison.Ordinal) ? text[prefix.Length..] : text;
        return name.Length > 0 && char.IsLetter(name[0]) && Enum.TryParse(name, out T value)
            ? value
            : throw new InvalidDataException($"Unknown {what} {text}");
    }

    private static string Required(IReadOnlyDictionary<string, string> members, string member, ConfigLine line)
    {
        return members.TryGetValue(member, out string? value) && value.Length > 0
            ? value
            : throw new InvalidDataException($"{line.Key} entry has no {member}");
    }

    private sealed record ProfileEntry(CollisionEnabled CollisionEnabled, string ObjectTypeName, IReadOnlyList<CollisionResponseOverride> Responses);
}
