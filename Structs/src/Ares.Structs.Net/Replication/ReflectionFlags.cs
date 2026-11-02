namespace Ares.Structs.Net.Replication;

/// <summary>Reflected property flags (<c>EPropertyFlags</c>). Only the bits servers use are named.</summary>
[Flags]
public enum PropertyFlags : ulong
{
    None = 0,

    /// <summary>Replicated (<c>CPF_Net</c>).</summary>
    Net = 0x20,

    /// <summary>Function parameter (<c>CPF_Parm</c>).</summary>
    Parameter = 0x80,

    /// <summary>Function return value (<c>CPF_ReturnParm</c>).</summary>
    ReturnParameter = 0x400,

    /// <summary>Skipped by replication (<c>CPF_RepSkip</c>).</summary>
    RepSkip = 0x8000_0000,
}

/// <summary>Reflected class flags (<c>EClassFlags</c>). Only the bits servers use are named.</summary>
[Flags]
public enum ClassFlags : uint
{
    None = 0,

    /// <summary>Native class (<c>CLASS_Native</c>); Blueprint classes order checksum fields by layout.</summary>
    Native = 0x80,

    /// <summary>Replication data was initialized (<c>CLASS_ReplicationDataIsSetUp</c>).</summary>
    ReplicationDataIsSetUp = 0x800,
}

/// <summary>Reflected function flags (<c>EFunctionFlags</c>). Only the bits servers use are named.</summary>
[Flags]
public enum FunctionFlags : uint
{
    None = 0,

    /// <summary>Network function (<c>FUNC_Net</c>).</summary>
    Net = 0x40,

    /// <summary>Multicast RPC (<c>FUNC_NetMulticast</c>).</summary>
    NetMulticast = 0x4000,

    /// <summary>Client-to-server RPC (<c>FUNC_NetServer</c>).</summary>
    NetServer = 0x20_0000,

    /// <summary>Server-to-client RPC (<c>FUNC_NetClient</c>).</summary>
    NetClient = 0x100_0000,
}

/// <summary>Reflected struct flags (<c>EStructFlags</c>). Only the bits servers use are named.</summary>
[Flags]
public enum StructFlags : uint
{
    None = 0,

    /// <summary>Has a native net serializer (<c>STRUCT_NetSerializeNative</c>): replicated as one value.</summary>
    NetSerializeNative = 0x400,

    /// <summary>Has a native delta serializer (<c>STRUCT_NetDeltaSerializeNative</c>).</summary>
    NetDeltaSerializeNative = 0x10_0000,
}
