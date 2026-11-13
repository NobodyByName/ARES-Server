using System.Buffers.Binary;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Game.Loot;

/// <summary>The actor of an item stack lying in the world (<c>APickup</c>): a dropped item or loose loot.</summary>
public static class Pickups
{
    /// <summary>Class the server spawns, with its class default.</summary>
    public static ArchetypeClass Class { get; } = new(ClassPaths.BpPickupBaseC, "/Game/Pioneer/Items/BP_PickupBase.Default__BP_PickupBase_C");

    /// <summary>Class whose replication layout the spawned Blueprint uses.</summary>
    public const string ActorClass = ClassPaths.Pickup;

    /// <summary>
    /// Name of the pickup's interaction component (the "Pickup" prompt), the only component that sends content
    /// with the pickup.
    /// </summary>
    public const string InteractionComponent = "Interaction";

    /// <summary>Class of the interaction component.</summary>
    public const string InteractionClass = ClassPaths.StandardInteractionComponent;

    /// <summary>Class of the item data assets a pickup references (<see cref="PickupProperties.DataAsset"/>).</summary>
    public const string DataAssetClass = ClassPaths.ItemDataAsset;

    /// <summary>Name of the pickup's collision mesh component, the interaction's attach parent and prompt component.</summary>
    public const string RootColliderComponent = "RootCollider";

    /// <summary>Class of the collision mesh component.</summary>
    public const string RootColliderClass = ClassPaths.StaticMeshComponent;

    /// <summary>Distance within which a pickup is relevant to a player, in centimetres (the class default's net cull distance).</summary>
    public const double NetCullDistance = 10000;

    /// <summary>
    /// Distance within which a player's drop joins that player's earlier pickup of the same item instead of spawning a
    /// new one, in centimetres (<c>UPickupTweakable.MergeDistance</c>).
    /// </summary>
    public const double MergeDistance = 300;

    /// <summary>
    /// Drops of an item merge only when its maximum stack size is above this (<c>UPickupTweakable.MaxStackSizeMergeThreshold</c>).
    /// </summary>
    public const int MergeStackSizeThreshold = 10;

    /// <summary>
    /// How far ahead of the dropping character, along its facing and at the height of its capsule centre, a new pickup
    /// spawns, in centimetres (<c>UPickupManager</c>).
    /// </summary>
    public const double DropDistance = 100;
}

/// <summary>
/// The item stack a pickup holds (<c>FBBItemContainerItem</c>), as replicated. A null member is not sent: the client
/// keeps the class default.
/// </summary>
public sealed record PickupItem
{
    /// <summary>Index of the item in the usable item table.</summary>
    public ushort? DataAssetIndex { get; init; }

    /// <summary>Quality.</summary>
    public sbyte? Quality { get; init; }

    /// <summary>Stack amount (<c>Amount.Value</c>).</summary>
    public int? Amount { get; init; }

    /// <summary>Largest amount the amount's quantization covers (<c>Amount.Quantization.MaxVal</c>).</summary>
    public int? AmountMaxValue { get; init; }

    /// <summary>Smallest amount the amount's quantization covers (<c>Amount.Quantization.MinVal</c>).</summary>
    public int? AmountMinValue { get; init; }

    /// <summary>Bits of a small amount change (<c>Amount.Quantization.SmallChangeBitCount</c>).</summary>
    public byte? AmountSmallChangeBitCount { get; init; }

    /// <summary>Durability quantized to a signed byte.</summary>
    public sbyte? QuantizedDurability { get; init; }

    /// <summary>Rounds loaded into the item's magazine or chamber.</summary>
    public ushort? AmmoInClip { get; init; }

    /// <summary>Recharge quantized to a signed byte.</summary>
    public sbyte? QuantizedRecharge { get; init; }

    /// <summary>
    /// Item identity. The four 32-bit words of <c>FGuid</c> (A to D) are the four groups of eight hexadecimal digits
    /// of <see cref="Guid.ToString(string)"/> with format "N".
    /// </summary>
    public Guid? Guid { get; init; }

    /// <summary>Where the item came from (<see cref="KnownEnums.ItemSources"/>).</summary>
    public byte? ItemSource { get; init; }

    /// <summary>Index of the item's sub-container.</summary>
    public byte? SubItemContainerIndex { get; init; }

    /// <summary>Index of the item's skin.</summary>
    public byte? SkinReplicationIndex { get; init; }
}

/// <summary>
/// Replicated properties of a pickup actor (<see cref="Pickups.ActorClass"/>), as member paths and values for the
/// property codec. A null member is not sent. The mod visual indices are not supported.
/// </summary>
public sealed record PickupProperties
{
    private const string ItemPath = PropertyNames.ContainedItemBb;
    private const string AmountPath = $"{ItemPath}.{PropertyNames.Amount}";
    private const string QuantizationPath = $"{AmountPath}.{PropertyNames.Quantization}";
    private const string GuidPath = $"{ItemPath}.{PropertyNames.Guid}";
    private static readonly string[] s_guidWords = [PropertyNames.A, PropertyNames.B, PropertyNames.C, PropertyNames.D];

    /// <summary>Whether clients apply <see cref="Movement"/> (<c>bReplicateMovement</c>).</summary>
    public bool? ReplicateMovement { get; init; }

    /// <summary>The actor's role on the server.</summary>
    public NetRole? Role { get; init; }

    /// <summary>The actor's role on clients.</summary>
    public NetRole? RemoteRole { get; init; }

    /// <summary>Location, rotation and physics state (<c>ReplicatedMovement</c>).</summary>
    public RepMovement? Movement { get; init; }

    /// <summary>Object id of the item's data asset (<c>DefaultPickupDataAsset</c>); the client builds the visuals from it.</summary>
    public ulong? DataAsset { get; init; }

    /// <summary>The item stack (<c>ContainedItem_BB</c>).</summary>
    public PickupItem? Item { get; init; }

    /// <summary>Server world time of the drop, in seconds (<c>DropServerWorldTime</c>).</summary>
    public double? DropServerWorldTime { get; init; }

    /// <summary>Player id of the player who dropped the item (<c>DroppingPlayerId</c>).</summary>
    public int? DroppingPlayerId { get; init; }

    /// <summary>Object id of the pawn that picked the item up (<c>PickedUpBy</c>).</summary>
    public ulong? PickedUpBy { get; init; }

    /// <summary>The values to send, by member path.</summary>
    /// <returns>Values typed for <see cref="ValueCodec"/>.</returns>
    public Dictionary<string, object?> ToValues()
    {
        Dictionary<string, object?> values = new(StringComparer.Ordinal);
        Add(values, PropertyNames.ReplicateMovement, ReplicateMovement);
        Add(values, PropertyNames.RemoteRole, (ulong?)RemoteRole);
        Add(values, PropertyNames.ReplicatedMovement, Movement);
        Add(values, PropertyNames.Role, (ulong?)Role);
        Add(values, PropertyNames.DefaultPickupDataAsset, DataAsset);
        if (Item is not null)
        {
            AddItem(values, Item);
        }

        Add(values, PropertyNames.DropServerWorldTime, DropServerWorldTime);
        Add(values, PropertyNames.DroppingPlayerId, DroppingPlayerId);
        Add(values, PropertyNames.PickedUpBy, PickedUpBy);
        return values;
    }

    /// <summary>Reads decoded values.</summary>
    /// <param name="values">Values by member path, as <see cref="ValueCodec"/> types them.</param>
    /// <returns>The properties.</returns>
    /// <exception cref="InvalidPacketException">A member is not a pickup property or has the wrong type, or the item identity is incomplete.</exception>
    public static PickupProperties FromValues(IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        PickupProperties result = new();
        PickupItem item = new();
        bool hasItem = false;
        int[] guid = new int[s_guidWords.Length];
        int guidWords = 0;
        foreach ((string path, object? value) in values)
        {
            int word = path.StartsWith(GuidPath + ".", StringComparison.Ordinal) ? Array.IndexOf(s_guidWords, path[(GuidPath.Length + 1)..]) : -1;
            if (word >= 0)
            {
                guid[word] = Get<int>(value);
                guidWords++;
                hasItem = true;
                continue;
            }

            hasItem |= path.StartsWith(ItemPath + ".", StringComparison.Ordinal);
            switch (path)
            {
                case PropertyNames.ReplicateMovement:
                    result = result with { ReplicateMovement = Get<bool>(value) };
                    break;
                case PropertyNames.RemoteRole:
                    result = result with { RemoteRole = (NetRole)Get<ulong>(value) };
                    break;
                case PropertyNames.ReplicatedMovement:
                    result = result with { Movement = value as RepMovement ?? throw Invalid() };
                    break;
                case PropertyNames.Role:
                    result = result with { Role = (NetRole)Get<ulong>(value) };
                    break;
                case PropertyNames.DefaultPickupDataAsset:
                    result = result with { DataAsset = Get<ulong>(value) };
                    break;
                case $"{ItemPath}.{PropertyNames.DataAssetIndex}":
                    item = item with { DataAssetIndex = Get<ushort>(value) };
                    break;
                case $"{ItemPath}.{PropertyNames.Quality}":
                    item = item with { Quality = Get<sbyte>(value) };
                    break;
                case $"{QuantizationPath}.{PropertyNames.MaxVal}":
                    item = item with { AmountMaxValue = Get<int>(value) };
                    break;
                case $"{QuantizationPath}.{PropertyNames.MinVal}":
                    item = item with { AmountMinValue = Get<int>(value) };
                    break;
                case $"{QuantizationPath}.{PropertyNames.SmallChangeBitCount}":
                    item = item with { AmountSmallChangeBitCount = (byte)Get<ulong>(value) };
                    break;
                case $"{AmountPath}.{PropertyNames.Value}":
                    item = item with { Amount = Get<int>(value) };
                    break;
                case $"{ItemPath}.{PropertyNames.QuantizedDurability}":
                    item = item with { QuantizedDurability = Get<sbyte>(value) };
                    break;
                case $"{ItemPath}.{PropertyNames.AmmoInClip}":
                    item = item with { AmmoInClip = Get<ushort>(value) };
                    break;
                case $"{ItemPath}.{PropertyNames.QuantizedRecharge}":
                    item = item with { QuantizedRecharge = Get<sbyte>(value) };
                    break;
                case $"{ItemPath}.{PropertyNames.ItemSource}":
                    item = item with { ItemSource = (byte)Get<ulong>(value) };
                    break;
                case $"{ItemPath}.{PropertyNames.SubItemContainerIndex}":
                    item = item with { SubItemContainerIndex = (byte)Get<ulong>(value) };
                    break;
                case $"{ItemPath}.{PropertyNames.SkinReplicationIndex}":
                    item = item with { SkinReplicationIndex = (byte)Get<ulong>(value) };
                    break;
                case PropertyNames.DropServerWorldTime:
                    result = result with { DropServerWorldTime = Get<double>(value) };
                    break;
                case PropertyNames.DroppingPlayerId:
                    result = result with { DroppingPlayerId = Get<int>(value) };
                    break;
                case PropertyNames.PickedUpBy:
                    result = result with { PickedUpBy = Get<ulong>(value) };
                    break;
                default:
                    throw new InvalidPacketException("Not a supported pickup property");
            }
        }

        if (guidWords is not 0 and not 4)
        {
            throw new InvalidPacketException("Incomplete pickup item identity");
        }

        if (guidWords != 4)
        {
            return hasItem ? result with { Item = item } : result;
        }

        byte[] bytes = new byte[16];
        for (int word = 0; word < guid.Length; word++)
        {
            BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(word * 4), guid[word]);
        }

        item = item with { Guid = new Guid(bytes, bigEndian: true) };
        return hasItem ? result with { Item = item } : result;
    }

    private static void AddItem(Dictionary<string, object?> values, PickupItem item)
    {
        Add(values, $"{ItemPath}.{PropertyNames.DataAssetIndex}", item.DataAssetIndex);
        Add(values, $"{ItemPath}.{PropertyNames.Quality}", item.Quality);
        Add(values, $"{QuantizationPath}.{PropertyNames.MaxVal}", item.AmountMaxValue);
        Add(values, $"{QuantizationPath}.{PropertyNames.MinVal}", item.AmountMinValue);
        Add(values, $"{QuantizationPath}.{PropertyNames.SmallChangeBitCount}", (ulong?)item.AmountSmallChangeBitCount);
        Add(values, $"{AmountPath}.{PropertyNames.Value}", item.Amount);
        Add(values, $"{ItemPath}.{PropertyNames.QuantizedDurability}", item.QuantizedDurability);
        Add(values, $"{ItemPath}.{PropertyNames.AmmoInClip}", item.AmmoInClip);
        Add(values, $"{ItemPath}.{PropertyNames.QuantizedRecharge}", item.QuantizedRecharge);
        if (item.Guid.HasValue)
        {
            Span<byte> bytes = stackalloc byte[16];
            item.Guid.Value.TryWriteBytes(bytes, bigEndian: true, out _);
            for (int word = 0; word < s_guidWords.Length; word++)
            {
                values[$"{GuidPath}.{s_guidWords[word]}"] = BinaryPrimitives.ReadInt32BigEndian(bytes[(word * 4)..]);
            }
        }

        Add(values, $"{ItemPath}.{PropertyNames.ItemSource}", (ulong?)item.ItemSource);
        Add(values, $"{ItemPath}.{PropertyNames.SubItemContainerIndex}", (ulong?)item.SubItemContainerIndex);
        Add(values, $"{ItemPath}.{PropertyNames.SkinReplicationIndex}", (ulong?)item.SkinReplicationIndex);
    }

    private static void Add<T>(Dictionary<string, object?> values, string path, T? value)
        where T : struct
    {
        if (value is { } present)
        {
            values[path] = present;
        }
    }

    private static void Add(Dictionary<string, object?> values, string path, RepMovement? value)
    {
        if (value is not null)
        {
            values[path] = value;
        }
    }

    private static T Get<T>(object? value)
        where T : struct => value is T typed ? typed : throw Invalid();

    private static InvalidPacketException Invalid() => new("Pickup value has the wrong type");
}
