using System.Text.Json;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Eos;
using Ares.Structs.Backend.Profiles;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Backend.Social;

namespace Ares.Structs.Tests.Backend;

public sealed class BackendJsonTests
{
    private static readonly EmbarkProfile s_profile = new(1, 2, 3, new DisplayName("Name", "0001"), 4, "a@b.invalid", true, 5,
        "0", "Name", "3", "2000-01-01", "FR", false, true);

    [Fact]
    public void Profile_writes_snake_case_then_camel_case_with_email_once()
    {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(s_profile, BackendJson.Options));
        string[] names = [.. document.RootElement.EnumerateObject().Select(property => property.Name)];

        Assert.Equal("tenancy_user_id", names[0]);
        Assert.Equal(15 + 14, names.Length);
        Assert.Single(names, name => name == "email");
        Assert.Equal(names.Length, names.Distinct().Count());
        Assert.Equal(1, document.RootElement.GetProperty("tenancyUserId").GetInt64());
        Assert.Equal("0001", document.RootElement.GetProperty("display_name").GetProperty("discriminator").GetString());
    }

    [Fact]
    public void Notification_payload_starts_with_its_discriminator_and_has_no_helper_members()
    {
        Notification notification = new("7", PartyPlayerReadyForMatchmakingNotification.Discriminator,
            new PartyPlayerReadyForMatchmakingNotification(1, "party", IsReadyForMatchmaking: true));

        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(notification, BackendJson.Options));
        JsonElement payload = document.RootElement.GetProperty("payload");

        Assert.Equal("discriminator", payload.EnumerateObject().First().Name);
        Assert.Equal(PartyPlayerReadyForMatchmakingNotification.Discriminator, payload.GetProperty("discriminator").GetString());
        Assert.False(payload.TryGetProperty("Name", out _));
        Assert.True(payload.GetProperty("isReadyForMatchmaking").GetBoolean());
    }

    [Fact]
    public void Eos_real_values_keep_their_decimal_point()
    {
        Dictionary<string, EosConfigValue> section = new() { ["Seconds"] = 20.0, ["Count"] = 3L, ["Ops"] = new[] { "A" } };

        string json = JsonSerializer.Serialize(section, BackendJson.Options);

        Assert.Equal("""{"Seconds":20.0,"Count":3,"Ops":["A"]}""", json);
    }

    [Fact]
    public void Eos_defaults_contain_every_section()
    {
        EosSdkConfiguration configuration = EosSdkDefaults.Configuration;

        Assert.Equal(110, configuration.Client.Count);
        Assert.Equal(39, configuration.Services.Count);
        Assert.Equal(new EosInteger(4), configuration.Client["BaseService"]["HttpRetryLimit"]);
    }

    [Fact]
    public void Empty_changeset_lists_every_change_kind()
    {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(Changeset.Empty, BackendJson.Options));

        Assert.Equal(["items", "quests", "stats", "offers", "projects", "notifications"],
            [.. document.RootElement.EnumerateObject().Select(property => property.Name)]);
        Assert.Equal(0, document.RootElement.GetProperty("items").GetProperty("created").GetArrayLength());
    }
}
