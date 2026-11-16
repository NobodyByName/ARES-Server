using Ares.Common.Archives;
using Ares.Common.Maps;
using Ares.MapData.Archives;
using Ares.MapData.Profiles;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Tests;

public sealed class CollisionProfilesTests : IDisposable
{
    internal const string Base = """
        [/Script/Engine.Other]
        +Profiles=(Name="Elsewhere",CollisionEnabled=QueryOnly)

        [/Script/Engine.CollisionProfile]
        +Profiles=(Name="NoCollision",CollisionEnabled=NoCollision,ObjectTypeName="WorldStatic",CustomResponses=((Channel="Visibility",Response=ECR_Ignore)))
        +Profiles=(Name="BlockAll",CollisionEnabled=QueryAndPhysics,bCanModify=False,ObjectTypeName="WorldStatic",CustomResponses=,HelpMessage="Blocks (all), by default.")
        +Profiles=(Name="OverlapAll",CollisionEnabled=QueryOnly,ObjectTypeName="WorldStatic",CustomResponses=((Channel="WorldStatic",Response=ECR_Overlap),(Channel="Pawn",Response=ECR_Overlap)))
        +Profiles=(Name="Old",CollisionEnabled=QueryOnly,ObjectTypeName="WorldStatic")
        +ProfileRedirects=(OldName="BlockingVolume",NewName="InvisibleWall")
        +ProfileRedirects=(OldName="StaticMeshComponent",NewName="BlockAll")
        """;

    internal const string Default = """
        [/Script/Engine.CollisionProfile]
        -Profiles=(Name="NoCollision",CollisionEnabled=NoCollision,ObjectTypeName="WorldStatic")
        -Profiles=(Name="Old",CollisionEnabled=QueryOnly)
        +Profiles=(Name="NoCollision",CollisionEnabled=NoCollision,ObjectTypeName="WorldStatic",CustomResponses=((Channel="PlayerPawn",Response=ECR_Ignore)))
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel3,DefaultResponse=ECR_Block,bTraceType=False,bStaticObject=False,Name="PlayerPawn")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel10,DefaultResponse=ECR_Ignore,bTraceType=True,bStaticObject=False,Name="Climbable")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel11,DefaultResponse=ECR_Ignore,bTraceType=True,bStaticObject=False,Name="HitScan")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel1,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="Minion")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel2,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="Harvester")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel4,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="Projectile")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel5,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="AIVisibility")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel6,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="ContextualActionCandidate")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel7,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="RadialBlock")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel8,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="InteractionProxy")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel9,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="RagdollProxy")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel12,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="DamageOcclusion")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel13,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="AudioOcclusion")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel14,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="UI")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel15,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="AITraceProjectile")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel16,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="PlayerTraceProjectile")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel17,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="WorldHitValidation")
        +DefaultChannelResponses=(Channel=ECC_GameTraceChannel18,DefaultResponse=ECR_Block,bTraceType=True,bStaticObject=False,Name="PlayerPawnPhysics")
        +Profiles=(Name="PlayerPawn",CollisionEnabled=QueryOnly,ObjectTypeName="PlayerPawn",CustomResponses=((Channel="Visibility",Response=ECR_Ignore),(Channel="PhysicsBody",Response=ECR_Overlap),(Channel="HitScan")))
        +Profiles=(Name="InvisibleWall",CollisionEnabled=QueryAndPhysics,ObjectTypeName="WorldStatic",CustomResponses=((Channel="Visibility",Response=ECR_Ignore)))
        +Profiles=(Name="ClimbOnly",CollisionEnabled=QueryAndPhysics,ObjectTypeName="WorldDynamic",CustomResponses=((Channel="PlayerPawn",Response=ECR_Overlap),(Channel="Climbable")))
        +Profiles=(Name="Physics",CollisionEnabled=QueryAndPhysics,ObjectTypeName="PhysicsBody",CustomResponses=((Channel="Climbable")))
        +Profiles=(Name="Untyped",CollisionEnabled=QueryAndPhysics,CustomResponses=((Channel="Climbable",Response=ECR_Block)))
        +EditProfiles=(Name="BlockAll",CustomResponses=((Channel="Climbable",Response=ECR_Block)))
        +EditProfiles=(Name="OverlapAll",CustomResponses=((Channel="PlayerPawn",Response=ECR_Overlap)))
        +EditProfiles=(Name="Missing",CustomResponses=((Channel="Climbable",Response=ECR_Block)))
        -ProfileRedirects=(OldName="BlockingVolume",NewName="InvisibleWall")
        +ProfileRedirects=(OldName="Wall",NewName="InvisibleWall")
        """;

    private readonly string _directory = Directory.CreateTempSubdirectory("ares-mapdata-").FullName;

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Channels_are_the_engine_object_types_and_the_named_game_channels()
    {
        CollisionProfiles profiles = CollisionProfiles.Parse([Base, Default]);

        Assert.Equal(CollisionChannel.PhysicsBody, profiles.Channels["PhysicsBody"]);
        Assert.Equal(CollisionChannel.GameTraceChannel3, profiles.Channels["PlayerPawn"]);
        Assert.Equal(CollisionChannel.GameTraceChannel10, profiles.Channels["Climbable"]);
        Assert.Equal(CollisionResponse.Block, profiles.DefaultResponses[(int)CollisionChannel.Visibility]);
        Assert.Equal(CollisionResponse.Ignore, profiles.DefaultResponses[(int)CollisionChannel.GameTraceChannel10]);
        Assert.Equal(CollisionResponse.Block, profiles.DefaultResponses[(int)CollisionChannel.GameTraceChannel3]);
    }

    [Fact]
    public void Later_files_remove_and_redefine_profiles_and_redirects()
    {
        CollisionProfiles profiles = CollisionProfiles.Parse([Base, Default]);

        Assert.False(profiles.Profiles.ContainsKey("Old"));
        Assert.False(profiles.Profiles.ContainsKey("Elsewhere"));
        Assert.False(profiles.Profiles.ContainsKey("Missing"));
        CollisionProfile noCollision = profiles.Profiles["NoCollision"];
        Assert.Equal(CollisionResponse.Block, noCollision.Responses[(int)CollisionChannel.Visibility]);
        Assert.Equal(CollisionResponse.Ignore, noCollision.Responses[(int)CollisionChannel.GameTraceChannel3]);
        Assert.False(profiles.Redirects.ContainsKey("BlockingVolume"));
        Assert.Equal("InvisibleWall", profiles.Find("Wall")?.Name);
        Assert.Equal("BlockAll", profiles.Find("StaticMeshComponent")?.Name);
        Assert.Null(profiles.Find("BlockingVolume"));
    }

    [Fact]
    public void Profile_responses_start_from_channel_defaults_then_take_custom_responses_and_edits()
    {
        CollisionProfiles profiles = CollisionProfiles.Parse([Base, Default]);

        CollisionProfile playerPawn = profiles.Profiles["PlayerPawn"];
        Assert.Equal(CollisionChannel.GameTraceChannel3, playerPawn.ObjectType);
        Assert.Equal(CollisionResponse.Ignore, playerPawn.Responses[(int)CollisionChannel.Visibility]);
        Assert.Equal(CollisionResponse.Overlap, playerPawn.Responses[(int)CollisionChannel.PhysicsBody]);
        Assert.Equal(CollisionResponse.Block, playerPawn.Responses[(int)CollisionChannel.GameTraceChannel11]);
        Assert.Equal(CollisionResponse.Ignore, playerPawn.Responses[(int)CollisionChannel.GameTraceChannel10]);
        Assert.Equal(CollisionResponse.Block, profiles.Profiles["BlockAll"].Responses[(int)CollisionChannel.GameTraceChannel10]);
        Assert.Equal(CollisionResponse.Overlap, profiles.Profiles["OverlapAll"].Responses[(int)CollisionChannel.GameTraceChannel3]);
        Assert.Equal(CollisionChannel.WorldStatic, profiles.Profiles["Untyped"].ObjectType);
    }

    [Fact]
    public void A_clear_line_empties_the_array_it_names()
    {
        const string Cleared = """
            [/Script/Engine.CollisionProfile]
            !Profiles=ClearArray
            +Profiles=(Name="PlayerPawn",CollisionEnabled=QueryOnly,ObjectTypeName="PlayerPawn")
            """;

        CollisionProfiles profiles = CollisionProfiles.Parse([Base, Default, Cleared]);

        Assert.Equal(["PlayerPawn"], profiles.Profiles.Keys);
    }

    [Theory]
    [InlineData("BlockAll", true, true, true)]
    [InlineData("StaticMeshComponent", true, true, true)]
    [InlineData("NoCollision", false, false, false)]
    [InlineData("OverlapAll", false, false, false)]
    [InlineData("Wall", true, false, true)]
    [InlineData("ClimbOnly", false, true, false)]
    [InlineData("Physics", true, true, false)]
    [InlineData("Untyped", true, true, true)]
    public void Named_profiles_decide_what_a_component_blocks(string profile, bool blocksPlayerChannel, bool blocksClimbable, bool blocksCapsule)
    {
        CollisionProfiles profiles = CollisionProfiles.Parse([Base, Default]);

        CollisionClassification result = profiles.Classify(profile, CollisionEnabled.NoCollision, CollisionChannel.PhysicsBody,
            [new CollisionResponseOverride("PlayerPawn", CollisionResponse.Ignore)]);

        Assert.Equal((blocksPlayerChannel, blocksClimbable, blocksCapsule), Bits(result));
    }

    [Fact]
    public void Custom_settings_apply_overrides_over_the_channel_defaults()
    {
        CollisionProfiles profiles = CollisionProfiles.Parse([Base, Default]);

        Assert.Equal((true, false, true), Bits(profiles.Classify("Custom", CollisionEnabled.QueryAndPhysics, null, [])));
        Assert.Equal((true, true, true),
            Bits(profiles.Classify(null, CollisionEnabled.QueryOnly, CollisionChannel.WorldDynamic, [new CollisionResponseOverride("Climbable")])));
        Assert.Equal((false, true, false), Bits(profiles.Classify("Custom", CollisionEnabled.QueryAndProbe, CollisionChannel.WorldStatic,
            [new CollisionResponseOverride("Climbable"), new CollisionResponseOverride("PlayerPawn", CollisionResponse.Overlap)])));
        Assert.Equal((true, false, false), Bits(profiles.Classify("Custom", CollisionEnabled.QueryAndPhysics, CollisionChannel.PhysicsBody,
            [new CollisionResponseOverride("NotAChannel", CollisionResponse.Ignore)])));
        Assert.Equal(default, profiles.Classify("Custom", CollisionEnabled.PhysicsOnly, null, [new CollisionResponseOverride("Climbable")]));
    }

    [Fact]
    public void A_body_that_takes_part_in_queries_blocks_every_named_channel_it_responds_to_with_block()
    {
        CollisionProfiles profiles = CollisionProfiles.Parse([Base, Default]);

        PlacementFlags wall = profiles.Classify("Wall", CollisionEnabled.NoCollision, null, []).Channels;
        PlacementFlags custom = profiles.Classify(null, CollisionEnabled.QueryOnly, CollisionChannel.WorldStatic,
            [new CollisionResponseOverride("PlayerTraceProjectile", CollisionResponse.Overlap), new CollisionResponseOverride("HitScan")]).Channels;

        // The engine trace channels the config does not name are left out.
        PlacementFlags unnamed = Enumerable.Range((int)CollisionChannel.EngineTraceChannel1, 6)
            .Aggregate(PlacementFlags.None, (mask, channel) => mask | CollisionChannelMask.Of((CollisionChannel)channel));
        PlacementFlags ignoredByDefault = CollisionChannelMask.Of(GameCollisionChannels.Climbable) | CollisionChannelMask.Of(GameCollisionChannels.HitScan);
        Assert.Equal(~(unnamed | ignoredByDefault | CollisionChannelMask.Of(CollisionChannel.Visibility)), wall);
        Assert.Equal(~(unnamed | CollisionChannelMask.Of(GameCollisionChannels.Climbable) | CollisionChannelMask.Of(GameCollisionChannels.PlayerTraceProjectile)), custom);
        Assert.True(custom.Blocks(GameCollisionChannels.HitScan));
        Assert.False(custom.Blocks(GameCollisionChannels.PlayerTraceProjectile));
    }

    [Fact]
    public void Game_channels_must_sit_where_structs_says()
    {
        string moved = Default.Replace("Channel=ECC_GameTraceChannel10,", "Channel=ECC_GameTraceChannel12,", StringComparison.Ordinal);
        string otherCapsule = Default.Replace("ObjectTypeName=\"PlayerPawn\"", "ObjectTypeName=\"Pawn\"", StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(() => CollisionProfiles.Parse([Base, moved]));
        Assert.Throws<InvalidDataException>(() => CollisionProfiles.Parse([Base, otherCapsule]));
    }

    private static (bool PlayerChannel, bool Climbable, bool Capsule) Bits(CollisionClassification classification) =>
        (classification.Channels.Blocks(GameCollisionChannels.PlayerPawn), classification.Channels.Blocks(GameCollisionChannels.Climbable),
            classification.BlocksPlayerCapsule);

    [Fact]
    public void The_player_capsule_profile_and_the_climbable_channel_are_required()
    {
        const string WithoutPlayer = """
            [/Script/Engine.CollisionProfile]
            +DefaultChannelResponses=(Channel=ECC_GameTraceChannel10,DefaultResponse=ECR_Ignore,Name="Climbable")
            """;
        const string WithoutClimbable = """
            [/Script/Engine.CollisionProfile]
            +Profiles=(Name="PlayerPawn",CollisionEnabled=QueryOnly,ObjectTypeName="Pawn")
            """;

        Assert.Throws<InvalidDataException>(() => CollisionProfiles.Parse([WithoutPlayer]));
        Assert.Throws<InvalidDataException>(() => CollisionProfiles.Parse([WithoutClimbable]));
        Assert.Throws<InvalidDataException>(() => CollisionProfiles.Parse([WithoutClimbable.Replace("QueryOnly", "Sometimes", StringComparison.Ordinal)]));
    }

    [Fact]
    public void The_default_shape_complexity_is_the_last_physics_setting_or_the_engine_default()
    {
        const string Physics = """
            [/Script/Engine.Other]
            DefaultShapeComplexity=CTF_UseSimpleAsComplex
            [/Script/Engine.PhysicsSettings]
            DefaultShapeComplexity=CTF_UseSimpleAsComplex
            DefaultShapeComplexity=ctf_usecomplexassimple
            """;

        Assert.Equal(CollisionProfileConfig.EngineDefaultShapeComplexity, CollisionProfiles.Parse([Base, Default]).DefaultShapeComplexity);
        Assert.Equal(CollisionTraceFlag.ComplexAsSimple, CollisionProfiles.Parse([Base, Physics, Default]).DefaultShapeComplexity);
        Assert.Equal(CollisionProfileConfig.EngineDefaultShapeComplexity,
            CollisionProfiles.Parse([Base, Physics, Default, "[/Script/Engine.PhysicsSettings]\n!DefaultShapeComplexity=ClearArray"]).DefaultShapeComplexity);
        Assert.Throws<InvalidDataException>(() => CollisionProfiles.Parse([Base, Default, "[/Script/Engine.PhysicsSettings]\nDefaultShapeComplexity=Complex"]));
    }

    [Fact]
    public void The_config_files_are_read_from_the_config_pak_in_order()
    {
        string path = Path.Combine(_directory, Path.GetFileName(CollisionProfileConfig.ConfigPak));
        SyntheticPak.Write(path, PakFile.InstallMountPrefix,
        [
            new SyntheticPakEntry(CollisionProfileConfig.ConfigFiles[1], [.. "﻿"u8, .. System.Text.Encoding.UTF8.GetBytes(Default.ReplaceLineEndings("\r\n"))], Encrypted: true),
            new SyntheticPakEntry(CollisionProfileConfig.ConfigFiles[0], System.Text.Encoding.UTF8.GetBytes(Base)),
        ]);
        using PakFile pak = PakFile.Open(path, new ArchiveKeys(SyntheticArchives.PakKey, SyntheticArchives.ContainerKey));

        CollisionProfiles profiles = CollisionProfiles.Load(pak);

        Assert.False(profiles.Profiles.ContainsKey("Old"));
        Assert.Equal((true, true, true), Bits(profiles.Classify("BlockAll", CollisionEnabled.NoCollision, null, [])));
    }
}
