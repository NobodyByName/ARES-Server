using Ares.Raid.World;
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
        LootSocket cabinet = DamLootSockets.All.Single(socket => socket is { Actor: CabinetActor, Socket: CabinetSocket });
        LootContainerType type = LootContainerTypes.Find(cabinet.ContainerTag)!;

        Assert.Empty(cabinet.OverrideMaterials);
        Assert.NotEmpty(type.Variants);
        Assert.Equal(type.Classes, LootSocketClasses.Candidates(cabinet));
        Assert.Equal(type.Classes, LootSocketClasses.Supported(cabinet));
        Assert.EndsWith(ServerCabinetClass, Assert.Single(type.Classes).Class, StringComparison.Ordinal);
    }

    [Fact]
    public void A_listed_override_material_selects_the_variant_class()
    {
        LootSocket cabinet = DamLootSockets.All.Single(socket => socket is { Actor: CabinetActor, Socket: CabinetSocket });
        LootContainerType type = LootContainerTypes.Find(cabinet.ContainerTag)!;
        LootContainerVariant variant = type.Variants[^1];

        LootSocket painted = cabinet with { OverrideMaterials = [variant.Material] };
        Assert.Equal(variant.Material, LootSocketClasses.Material(painted, DamLootSockets.Meshes));
        Assert.Equal(variant.Classes, LootSocketClasses.Candidates(painted));
        Assert.NotEqual(type.Classes, variant.Classes);

        LootSocket upperCase = cabinet with { OverrideMaterials = [variant.Material.ToUpperInvariant()] };
        Assert.Equal(variant.Classes, LootSocketClasses.Candidates(upperCase));
    }

    [Fact]
    public void An_empty_slot_falls_back_to_the_mesh_material_and_an_unlisted_material_to_the_default()
    {
        LootSocket cabinet = DamLootSockets.All.Single(socket => socket is { Actor: CabinetActor, Socket: CabinetSocket });
        LootContainerType type = LootContainerTypes.Find(cabinet.ContainerTag)!;
        LootContainerVariant variant = type.Variants[0];
        LootSocketMesh[] meshes = [new("/Game/Mesh.Mesh", [variant.Material])];
        LootSocket onMesh = cabinet with { Mesh = 0, OverrideMaterials = [null] };

        Assert.Equal(variant.Material, LootSocketClasses.Material(onMesh, meshes));
        Assert.Equal(variant.Classes, LootSocketClasses.Candidates(onMesh, meshes));

        LootSocket unlisted = onMesh with { OverrideMaterials = ["/Game/Unlisted.Unlisted"] };
        Assert.Equal(type.Classes, LootSocketClasses.Candidates(unlisted, meshes));

        LootSocketMesh[] bare = [new("/Game/Mesh.Mesh", [])];
        Assert.Null(LootSocketClasses.Material(onMesh with { OverrideMaterials = [] }, bare));
        Assert.Equal(type.Classes, LootSocketClasses.Candidates(onMesh with { OverrideMaterials = [] }, bare));
    }

    [Fact]
    public void A_tag_without_a_container_type_has_no_classes()
    {
        LootSocket wreck = DamLootSockets.All.First(socket => socket.ContainerTag == UnmatchedTag);

        Assert.Null(LootContainerTypes.Find(UnmatchedTag));
        Assert.Empty(LootSocketClasses.Candidates(wreck));
        Assert.Empty(LootSocketClasses.Supported(wreck));
    }

    [Fact]
    public void Supported_classes_are_the_candidates_the_server_can_serve()
    {
        int unsupported = 0;
        foreach (LootSocket socket in DamLootSockets.All)
        {
            IReadOnlyList<ArchetypeClass> candidates = LootSocketClasses.Candidates(socket);
            IReadOnlyList<ArchetypeClass> supported = LootSocketClasses.Supported(socket);
            Assert.Equal(candidates.Where(candidate => LootContainerClasses.Find(candidate.Class) is not null), supported);
            unsupported += candidates.Count - supported.Count;
        }

        Assert.NotEqual(0, unsupported);
    }
}
