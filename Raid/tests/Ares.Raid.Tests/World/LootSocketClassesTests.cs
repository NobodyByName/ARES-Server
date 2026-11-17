using Ares.Raid.World.Loot;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World;

public sealed class LootSocketClassesTests
{
    private const string CabinetActor = "StaticMeshActor_59f2d924aecddadf_19";
    private const string CabinetSocket = "ContainerSocket.Door_4";
    private const string UnmatchedTag = "Socket.LootContainer.ARC.Wreck03";
    private const string ServerCabinetClass = "/BP_SocketContainer_Gvt_Server_01_x400z250_Door.BP_SocketContainer_Gvt_Server_01_x400z250_Door_C";

    [Fact]
    public void A_socket_without_overrides_gets_the_default_classes_of_its_type()
    {
        LootSocket cabinet = TestGameData.Dam.LootSockets.Single(socket => socket is { Actor: CabinetActor, Socket: CabinetSocket });
        LootContainerType type = TestGameData.Loot.FindContainerType(cabinet.ContainerTag)!;

        Assert.Empty(cabinet.OverrideMaterials);
        Assert.NotEmpty(type.Variants);
        Assert.Equal(type.Classes, LootSocketClasses.Candidates(TestGameData.Data, cabinet));
        Assert.Equal(type.Classes, LootSocketClasses.Supported(TestGameData.Data, cabinet));
        Assert.EndsWith(ServerCabinetClass, Assert.Single(type.Classes).Class, StringComparison.Ordinal);
    }

    [Fact]
    public void A_listed_override_material_selects_the_variant_class()
    {
        LootSocket cabinet = TestGameData.Dam.LootSockets.Single(socket => socket is { Actor: CabinetActor, Socket: CabinetSocket });
        LootContainerType type = TestGameData.Loot.FindContainerType(cabinet.ContainerTag)!;
        LootContainerVariant variant = type.Variants[^1];

        LootSocket painted = cabinet with { OverrideMaterials = [variant.Material] };
        Assert.Equal(variant.Material, LootSocketClasses.Material(painted, TestGameData.Dam.LootSocketMeshes));
        Assert.Equal(variant.Classes, LootSocketClasses.Candidates(TestGameData.Data, painted));
        Assert.NotEqual(type.Classes, variant.Classes);

        LootSocket upperCase = cabinet with { OverrideMaterials = [variant.Material.ToUpperInvariant()] };
        Assert.Equal(variant.Classes, LootSocketClasses.Candidates(TestGameData.Data, upperCase));
    }

    [Fact]
    public void An_empty_slot_falls_back_to_the_mesh_material_and_an_unlisted_material_to_the_default()
    {
        LootSocket cabinet = TestGameData.Dam.LootSockets.Single(socket => socket is { Actor: CabinetActor, Socket: CabinetSocket });
        LootContainerType type = TestGameData.Loot.FindContainerType(cabinet.ContainerTag)!;
        LootContainerVariant variant = type.Variants[0];
        LootSocketMesh[] meshes = [new("/Game/Mesh.Mesh", [variant.Material])];
        LootSocket onMesh = cabinet with { Mesh = 0, OverrideMaterials = [null] };

        Assert.Equal(variant.Material, LootSocketClasses.Material(onMesh, meshes));
        Assert.Equal(variant.Classes, LootSocketClasses.Candidates(TestGameData.Loot, onMesh, meshes));

        LootSocket unlisted = onMesh with { OverrideMaterials = ["/Game/Unlisted.Unlisted"] };
        Assert.Equal(type.Classes, LootSocketClasses.Candidates(TestGameData.Loot, unlisted, meshes));

        LootSocketMesh[] bare = [new("/Game/Mesh.Mesh", [])];
        Assert.Null(LootSocketClasses.Material(onMesh with { OverrideMaterials = [] }, bare));
        Assert.Equal(type.Classes, LootSocketClasses.Candidates(TestGameData.Loot, onMesh with { OverrideMaterials = [] }, bare));
    }

    [Fact]
    public void A_tag_without_a_container_type_has_no_classes()
    {
        LootSocket wreck = TestGameData.Dam.LootSockets.First(socket => socket.ContainerTag == UnmatchedTag);

        Assert.Null(TestGameData.Loot.FindContainerType(UnmatchedTag));
        Assert.Empty(LootSocketClasses.Candidates(TestGameData.Data, wreck));
        Assert.Empty(LootSocketClasses.Supported(TestGameData.Data, wreck));
    }

    [Fact]
    public void Supported_classes_are_the_candidates_the_server_can_serve()
    {
        int unsupported = 0;
        foreach (LootSocket socket in TestGameData.Dam.LootSockets)
        {
            IReadOnlyList<ArchetypeClass> candidates = LootSocketClasses.Candidates(TestGameData.Data, socket);
            IReadOnlyList<ArchetypeClass> supported = LootSocketClasses.Supported(TestGameData.Data, socket);
            Assert.Equal(candidates.Where(candidate => LootSocketClasses.Served(TestGameData.Loot, candidate.Class) is not null), supported);
            unsupported += candidates.Count - supported.Count;
        }

        Assert.NotEqual(0, unsupported);
    }
}
