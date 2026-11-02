using Ares.Structs.Core.Geometry;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Structs.Tests.Net;

/// <summary>Reference encodings for the actor channel content tests (bytes in hex, bit lengths).</summary>
internal static class ChannelContentVectors
{
    /// <summary>Largest packet used by the vectors.</summary>
    public const int MaxPacketBytes = 1024;

    /// <summary>Block, encoded, bits.</summary>
    public static IReadOnlyList<(ActorContentBlock Block, string Encoded, long Bits)> Blocks { get; } =
    [
        (new ActorContentBlock(Convert.FromHexString(""), 0, false), "0200", 10),
        (new ActorContentBlock(Convert.FromHexString("01"), 1, true), "0b04", 11),
        (new ActorContentBlock(Convert.FromHexString("ebb13058e1e3eb8c821e"), 77, false), "6aaec7c260858faf330a7a", 87),
        (new ActorContentBlock(Convert.FromHexString("f6190632df6e186b1da84e2e1b6771d3e6ecc9fef43f6a09248bbfec44805e55e1bfdca7fee95eaa42dc9f8fb7a37038a8ecd9b4e70128afe3be2670dd30c748371023b52a81490a3e7df6118f0e8fec9d258412e318510b"), 700, true), "e729d86718c87cbb61ac75a03ab96c9cc54d9bb327fbd3ffa825902cfeb213017a5585ff729ffaa77ba90a717f3ede8ec2e1a0b267d39e07a0bc8efb9ac075c31c23dd408cd4aa042629f8f4d9473c3a3cb27796104a8c63442d", 718),
    ];

    /// <summary>Field payload, field count, encoded, bits.</summary>
    public static IReadOnlyList<(ActorFieldPayload Field, int FieldCount, string Encoded, long Bits)> Fields { get; } =
    [
        (new ActorFieldPayload(0, Convert.FromHexString(""), 0), 1, "0000", 9),
        (new ActorFieldPayload(3, Convert.FromHexString("e401"), 9), 4, "4b9007", 19),
        (new ActorFieldPayload(44, Convert.FromHexString("1b33b0b9f007e0265d61e08e1bd1b0"), 120), 67, "2cfcc60c6c2efc01b8495718b8e346342c", 134),
        (new ActorFieldPayload(65534, Convert.FromHexString("9879b38100"), 33), 65535, "feff429879b38100", 57),
    ];

    /// <summary>Static ids, encoded, bits.</summary>
    public static IReadOnlyList<(ulong[] Guids, string Encoded, long Bits)> MustMaps { get; } =
    [
        ([3UL], "010006", 24),
        ([11UL, 9UL, 7UL], "030016120e", 40),
        ([3UL, 5UL, 7UL, 9UL, 11UL, 13UL, 15UL, 17UL, 19UL, 21UL, 23UL, 25UL, 27UL, 29UL, 31UL, 33UL, 35UL, 37UL, 39UL, 41UL, 43UL, 45UL, 47UL, 49UL, 51UL, 53UL, 55UL, 57UL, 59UL, 61UL, 63UL, 65UL, 67UL, 69UL, 71UL, 73UL, 75UL, 77UL, 79UL, 81UL, 83UL, 85UL, 87UL, 89UL, 91UL, 93UL, 95UL, 97UL, 99UL, 101UL, 103UL, 105UL, 107UL, 109UL, 111UL, 113UL, 115UL, 117UL, 119UL, 121UL, 123UL, 125UL, 127UL, 129UL, 131UL, 133UL, 135UL, 137UL, 139UL, 141UL, 143UL, 145UL, 147UL, 149UL, 151UL, 153UL, 155UL, 157UL, 159UL, 161UL, 163UL, 165UL, 167UL, 169UL, 171UL, 173UL, 175UL, 177UL, 179UL, 181UL, 183UL, 185UL, 187UL, 189UL, 191UL, 193UL, 195UL, 197UL, 199UL, 201UL, 203UL, 205UL, 207UL, 209UL, 211UL, 213UL, 215UL, 217UL, 219UL, 221UL, 223UL, 225UL, 227UL, 229UL, 231UL, 233UL, 235UL, 237UL, 239UL, 241UL, 243UL, 245UL, 247UL, 249UL, 251UL, 253UL, 255UL, 257UL, 259UL, 261UL, 263UL, 265UL, 267UL, 269UL, 271UL, 273UL, 275UL, 277UL, 279UL, 281UL, 283UL, 285UL, 287UL, 289UL, 291UL, 293UL, 295UL, 297UL, 299UL, 301UL, 303UL, 305UL, 307UL, 309UL, 311UL, 313UL, 315UL, 317UL, 319UL, 321UL, 323UL, 325UL, 327UL, 329UL, 331UL, 333UL, 335UL, 337UL, 339UL, 341UL, 343UL, 345UL, 347UL, 349UL, 351UL, 353UL, 355UL, 357UL, 359UL, 361UL, 363UL, 365UL, 367UL, 369UL, 371UL, 373UL, 375UL, 377UL, 379UL, 381UL, 383UL, 385UL, 387UL, 389UL, 391UL, 393UL, 395UL, 397UL, 399UL, 401UL, 403UL, 405UL, 407UL, 409UL, 411UL, 413UL, 415UL, 417UL, 419UL, 421UL, 423UL, 425UL, 427UL, 429UL, 431UL, 433UL, 435UL, 437UL, 439UL, 441UL, 443UL, 445UL, 447UL, 449UL, 451UL, 453UL, 455UL, 457UL, 459UL, 461UL, 463UL, 465UL, 467UL, 469UL, 471UL, 473UL, 475UL, 477UL, 479UL, 481UL, 483UL, 485UL, 487UL, 489UL, 491UL, 493UL, 495UL, 497UL, 499UL, 501UL, 503UL, 505UL, 507UL, 509UL, 511UL, 513UL], "0001060a0e12161a1e22262a2e32363a3e42464a4e52565a5e62666a6e72767a7e82868a8e92969a9ea2a6aaaeb2b6babec2c6caced2d6dadee2e6eaeef2f6fafe030207020b020f02130217021b021f02230227022b022f02330237023b023f02430247024b024f02530257025b025f02630267026b026f02730277027b027f02830287028b028f02930297029b029f02a302a702ab02af02b302b702bb02bf02c302c702cb02cf02d302d702db02df02e302e702eb02ef02f302f702fb02ff02030407040b040f04130417041b041f04230427042b042f04330437043b043f04430447044b044f04530457045b045f04630467046b046f04730477047b047f04830487048b048f04930497049b049f04a304a704ab04af04b304b704bb04bf04c304c704cb04cf04d304d704db04df04e304e704eb04ef04f304f704fb04ff04030607060b060f06130617061b061f06230627062b062f06330637063b063f06430647064b064f06530657065b065f06630667066b066f06730677067b067f06830687068b068f06930697069b069f06a306a706ab06af06b306b706bb06bf06c306c706cb06cf06d306d706db06df06e306e706eb06ef06f306f706fb06ff060308", 3608),
    ];

    /// <summary>Initial bunches of a spawned player controller, a plain spawned actor, a map actor and a map actor with components.</summary>
    public static IReadOnlyList<(ChannelBunch Bunch, string Encoded, long Bits)> Initializations { get; } =
    [
        (new ActorInitialization(new ActorSpawn(20UL, 11UL, 0UL, new Vector3D(1.5, 2.5, -3.0)), new PlayerControllerOpen(0, 77U, 12345), [new ActorContentBlock(Convert.FromHexString("01"), 1, true), new ActorContentBlock(Convert.FromHexString("ebb13058e1e3eb8c821e"), 77, false)]).ToBunch(4, 9, [13UL, 11UL, 15UL]), "134109647e0d0300161a1e281600010000000000e0ff0000000000001000010000000000200003a00900002007060060816aaec7c260858faf330a7a", 479),
        (new ActorInitialization(new ActorSpawn(22UL, 13UL, 0UL, default), null, []).ToBunch(5, 0, []), "53410064a60101001a2c1a0000", 100),
        (new StaticActorInitialization(new StaticActorReference(0x2001UL), [new ActorContentBlock(Convert.FromHexString("01"), 1, true), new ActorContentBlock(Convert.FromHexString("ebb13058e1e3eb8c821e"), 77, false)]).ToBunch(6, 1023), "9301ff67960303800b54733d16062b7c7c9d51d003", 162),
        (new StaticActorInitialization(new StaticActorReference(0x2001UL), [new ActorContentBlock(Convert.FromHexString("01"), 1, true)]) { Components = [new StaticSubobjectContent(0x2003UL, new ActorContentBlock(Convert.FromHexString("01"), 1, true)), new StaticSubobjectContent(0x2005UL, new ActorContentBlock(Convert.FromHexString("ebb13058e1e3eb8c821e"), 77, false))] }.ToBunch(7, 4), "d3010464fe0403800bec00b04016006baec7c260858faf330a7a", 207),
    ];

    /// <summary>Stable subobject 22 with block 1.</summary>
    public const string StableSubobject = "b11408";

    /// <summary>Bit length of <see cref="StableSubobject"/>.</summary>
    public const long StableSubobjectBits = 20;

    /// <summary>Dynamic subobject 24 of class 13 with block 2, outer is the channel actor.</summary>
    public const string DynamicSubobject = "c0a051733d16062b7c7c9d51d003";

    /// <summary>Bit length of <see cref="DynamicSubobject"/>.</summary>
    public const long DynamicSubobjectBits = 106;

    /// <summary>Dynamic subobject 26 of class 15 with block 0, outer 22.</summary>
    public const string DynamicSubobjectWithOuter = "d0e0810500";

    /// <summary>Bit length of <see cref="DynamicSubobjectWithOuter"/>.</summary>
    public const long DynamicSubobjectWithOuterBits = 37;

    /// <summary>Client block for subobject 30: rep layout, 5 payload bits (all set).</summary>
    public const string ClientSubobject = "f1287c";

    /// <summary>Bit length of <see cref="ClientSubobject"/>.</summary>
    public const long ClientSubobjectBits = 23;

    /// <summary>Client block naming "NewComp" inside 20, with a checksum and 3 payload bits (5).</summary>
    public const string ClientPathReference = "0814a0200000003895dd0dbdb5c10104bc37af1a14";

    /// <summary>Bit length of <see cref="ClientPathReference"/>.</summary>
    public const long ClientPathReferenceBits = 165;

    /// <summary>Name, encoded, bits.</summary>
    public static IReadOnlyList<(NetworkName Name, string Encoded, long Bits)> Names { get; } =
    [
        (new NetworkName("None", 0U), "0100", 9),
        (new NetworkName("Actor", 0U), "9901", 9),
        (new NetworkName("Control", 0U), "ff0500", 17),
        (new NetworkName("Actor", 2U), "0c00000082c6e8dee4000400000000", 113),
        (new NetworkName("/Game/Maps/Dam_Sub_1", 0U), "2a0000005e8ec2daca5e9ac2e0e65e88c2dabea6eac4be62000000000000", 233),
        (new NetworkName("Persistent", 7U), "16000000a0cae4e6d2e6e8cadce8000e00000000", 153),
    ];

    /// <summary>Visibility, encoded, bits.</summary>
    public static IReadOnlyList<(LevelVisibility Visibility, string Encoded, long Bits)> Visibilities { get; } =
    [
        (new LevelVisibility("None", "None", false, false, new VisibilityTransaction(0, false)), "090000", 21),
        (new LevelVisibility("/Game/Maps/Dam_Sub_1", "None", true, false, new VisibilityTransaction(5, true)), "53010000f07214d656f6d2140637f74214d6f6355527f61503000000005001", 245),
        (new LevelVisibility("/Game/A", "/Game/A_File", false, true, new VisibilityTransaction(1073741824, false)), "84000000f07214d656f6120400000000a0010000e0e528acadec25e8cb288dad0c00000000404040400002", 342),
        (new LevelVisibility("/Game/B", "/Game/B", false, false, new VisibilityTransaction(0, false)), "81000000f07214d656f62204000000000000", 141),
    ];

    /// <summary>Reader, argument, malformed bits, bit length, what is wrong.</summary>
    public static TheoryData<string, int, string, long, string> Malformed => new()
    {
        { "block", 0, "00", 2, "subobject content read as actor content" },
        { "block", 0, "920100", 20, "truncated content" },
        { "field", 4, "04", 3, "reserved field index" },
        { "field", 4, "4101", 11, "truncated field payload" },
        { "mustmap", 0, "0000", 16, "no ids" },
        { "mustmap", 0, "02000606", 32, "duplicate id" },
        { "mustmap", 0, "010008", 24, "dynamic id" },
        { "stable", 0, "02", 2, "actor content read as subobject" },
        { "stable", 0, "2000", 11, "not stably named" },
        { "stable", 0, "180400", 19, "static id" },
        { "dynamic", 0, "2004", 11, "stable subobject read as dynamic" },
        { "dynamic", 0, "2008", 12, "delete message" },
        { "dynamic", 0, "20c01000", 29, "dynamic class id" },
        { "clientpath", 0, "2800", 10, "not a path reference" },
        { "clientpath", 0, "081000", 18, "path reference without path" },
        { "name", 0, "1d00", 9, "unknown hardcoded name" },
        { "name", 0, "0000000000", 33, "empty name" },
        { "transaction", 0, "020202022000", 41, "transaction index above 31 bits" },
        { "visibility", 0, "070000", 21, "visible and still trying" },
    };
}
