using System.Globalization;
using System.Numerics;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World.Movement;

public sealed class SnapHookUseTests
{
    /// <summary>
    /// The client's own prediction of ten throws, standing or walking between them: per throw "tick, feet x y z before it,
    /// target x y z, hit, surface, secondary presses while the hook was out, control yaw and pitch, camera offset x y z"; per pull "&gt; first
    /// tick, feet x y z and velocity after it", then per later pull tick "tick, forward and right axes, jump, control yaw,
    /// encoded velocity x y z".
    /// </summary>
    private static readonly string[] s_throws =
    [
        "11910 473792.4986622789 562240.6306480815 -36793.453124999985 472939.1431282398 561500.4044882983 -36768.95526624436 1 25 11956 -2.463716554019543 -0.12266093029850497 -121.22100122100125 41.51404151404154 178.26617826617826",
        "> 11934 473761.1320255258 562226.99732775 -36793.453124999985 0 0 0",
        "  11935 0 0 0 -2.701037800031994 2047 2047 2047",
        "  11936 0 0 0 -2.696395957605363 2043 2044 2047",
        "  11937 0 0 0 -2.69424813942743 2035 2036 2046",
        "  11938 0 0 0 -2.689613035377407 2023 2026 2046",
        "  11939 0 0 0 -2.672921638222782 2009 2014 2044",
        "  11940 0 0 0 -2.6627069986464584 1992 1998 2043",
        "  11941 0 0 0 -2.654245567256259 1972 1981 2041",
        "  11942 0 0 0 -2.6308291809814275 1950 1961 2039",
        "  11943 0 0 0 -2.6140291589761437 1924 1938 2037",
        "  11944 -126 0 0 -2.616894007343595 1899 1910 2034",
        "  11945 -126 0 0 -2.6179549455609914 1872 1879 2029",
        "  11946 -126 0 0 -2.6301776305334053 1843 1844 2025",
        "  11947 -126 0 0 -2.656225316673151 1811 1807 2020",
        "  11948 -126 0 0 -2.688432752468702 1778 1765 2015",
        "  11949 -126 0 0 -2.7067775767092823 1742 1721 2010",
        "  11950 -89 -89 0 -2.711335670369574 1689 1704 2005",
        "  11951 -89 -89 0 -2.7257303037276572 1635 1686 2001",
        "  11952 -89 -89 0 -2.7595178458978107 1581 1667 1997",
        "  11953 -89 -89 0 -2.788730854651473 1528 1647 1993",
        "  11954 -89 -89 0 -2.814103162186119 1499 1645 1992",
        "  11955 -89 -89 0 -2.840550237216899 1493 1655 2000",
        "  11956 -89 -89 0 -2.8570241855011163 1539 1702 2073",
        "12138 473481.676261129 562050.7741984096 -36799.051137725495 472809.5507086036 561631.6181542845 -36657.160793670686 1 25 0 -2.6360954442014153 -0.027034125666032884 -114.38339438339437 41.51404151404154 166.1538461538462",
        "> 12158 473481.676261129 562050.7741984096 -36799.051137725495 0 0 0",
        "  12159 0 0 0 -2.6368117202866634 2046 2047 2048",
        "  12160 0 0 0 -2.6368117202866634 2043 2044 2048",
        "  12161 0 0 0 -2.6368117202866634 2033 2038 2049",
        "  12162 0 0 0 -2.6368117202866634 2020 2030 2050",
        "  12163 0 0 0 -2.637169833520936 2004 2020 2051",
        "  12164 0 0 0 -2.637169833520936 1985 2008 2052",
        "  12165 0 0 0 -2.637527930234651 1962 1994 2054",
        "  12166 0 0 0 -2.637527930234651 1937 1978 2056",
        "  12167 0 0 0 -2.637886010441562 1908 1960 2058",
        "  12168 0 0 0 -2.6382463813809642 1876 1940 2060",
        "  12169 0 0 0 -2.6382463813809642 1840 1918 2063",
        "  12170 0 0 0 -2.638604426138269 1802 1894 2066",
        "  12171 0 0 0 -2.638962454430039 1760 1868 2069",
        "  12172 0 0 0 -2.6393204662700063 1715 1840 2072",
        "  12173 0 0 0 -2.6396784616718953 1667 1810 2076",
        "  12174 0 0 0 -2.640036440649427 1619 1780 2079",
        "  12175 0 0 0 -2.640394403216317 1570 1750 2083",
        "  12176 0 0 0 -2.640394403216317 1522 1720 2086",
        "  12177 0 0 0 -2.640394403216317 1474 1690 2090",
        "  12178 0 0 0 -2.640394403216317 1470 1687 2090",
        "  12179 0 0 0 -2.640394403216317 1470 1687 2090",
        "  12180 0 0 0 -2.640394403216317 1487 1698 2089",
        "12475 473545.4601090727 562033.379889503 -36800.69186521258 472777.32843739795 561578.9634106976 -36384.6125929401 1 15 0 -2.654021386377946 0.2747616811435663 -117.70451770451768 41.51404151404154 131.7704517704518",
        "> 12497 473545.4601090727 562033.379889503 -36800.69186521258 0 0 0",
        "  12498 0 0 0 -2.6625921047549737 2046 2047 2048",
        "  12499 0 0 0 -2.663307375276963 2043 2045 2050",
        "  12500 0 0 0 -2.663307375276963 2034 2039 2053",
        "  12501 0 0 0 -2.661499297704064 2021 2032 2059",
        "  12502 0 0 0 -2.65752438927528 2006 2023 2065",
        "  12503 0 0 0 -2.6538934527385565 1988 2012 2073",
        "  12504 0 0 0 -2.650624992984495 1966 1999 2082",
        "  12505 0 0 0 -2.649536219292144 1942 1985 2092",
        "  12506 0 0 0 -2.6498843632697713 1914 1968 2104",
        "  12507 0 0 0 -2.649874447300497 1883 1950 2117",
        "  12508 0 0 0 -2.6495158857339924 1849 1930 2131",
        "  12509 0 0 0 -2.649863645572763 1813 1909 2147",
        "  12510 0 0 0 -2.65022091297975 1773 1885 2163",
        "  12511 0 0 0 -2.650226043034391 1730 1860 2182",
        "  12512 0 0 0 -2.6505839481687206 1684 1832 2201",
        "  12513 0 0 0 -2.650941837269623 1638 1805 2220",
        "  12514 0 0 0 -2.651278996247934 1592 1778 2240",
        "  12515 0 0 0 -2.651278337762012 1546 1751 2259",
        "  12516 0 0 0 -2.6512676510521116 1500 1723 2279",
        "  12517 0 0 0 -2.650540139582054 1495 1721 2280",
        "  12518 0 0 1 -2.650527491823042 1504 1726 2307",
        "  12519 126 0 1 -2.6497984689776426 1508 1730 2320",
        "  12520 126 0 1 -2.649043752239657 1512 1733 2330",
        "  12521 126 0 0 -2.648644572497863 1504 1729 2311",
        "  12522 126 0 0 -2.647168499569357 1520 1739 2300",
        "12899 472470.19873819104 561664.2447768969 -36350.00000255832 471633.9325281526 563054.5956123963 -35433.692004037555 0 0 0 2.100551176079585 0.4479523747433381 -114.96947496947496 41.51404151404154 113.2112332112332",
        "13157 472380.19882402173 561601.7115032 -36350.00000000069 471813.21138004377 563118.3129781359 -35426.86131169542 0 0 13163 1.9170117810961358 0.4519950393674849 -114.96947496947496 41.51404151404154 112.82051282051282",
        "13221 472380.19882402173 561601.7115032 -36350.00000000069 471813.21138004377 563118.3129781359 -35426.86131169542 0 0 13227 1.9170117810961358 0.4519950393674849 -114.96947496947496 41.51404151404154 112.82051282051282",
        "13447 473731.4975353216 562249.344218902 -36793.45284674195 472824.4399489793 561670.3373318191 -36714.238201932865 1 25 13455 -2.6120668930353377 -0.078193295999396 -113.2112332112332 41.51404151404154 172.01465201465203",
        "13490 473731.4975353216 562249.344218902 -36793.45284674195 472824.6902884892 561671.1479549941 -36715.34684833395 1 25 13512 -2.6124054719925938 -0.0791978891977367 -113.2112332112332 41.51404151404154 172.01465201465203",
        "13652 473731.4975353216 562249.344218902 -36793.45284674195 472808.8473737899 561690.674436768 -36708.42078855948 1 25 13692 -2.6354459806094894 -0.07253968472026322 -113.4065934065934 41.51404151404154 171.23321123321125",
        "> 13676 473731.4975353216 562249.344218902 -36793.45284674195 0 0 0",
        "  13677 0 0 0 -2.6354459806094894 2046 2047 2047",
        "  13678 0 0 0 -2.6354459806094894 2042 2044 2047",
        "  13679 0 0 0 -2.635804766929328 2033 2039 2047",
        "  13680 0 0 0 -2.635804766929328 2020 2031 2047",
        "  13681 0 0 0 -2.635804766929328 2004 2021 2047",
        "  13682 0 0 0 -2.635804766929328 1984 2009 2047",
        "  13683 0 0 0 -2.636163536638163 1961 1995 2047",
        "  13684 0 0 0 -2.636163536638163 1935 1980 2047",
        "  13685 0 0 0 -2.636163536638163 1906 1962 2046",
        "  13686 0 0 0 -2.6365222897497556 1874 1942 2046",
        "  13687 0 0 0 -2.6365285510882717 1838 1921 2046",
        "  13688 0 0 0 -2.6365285510882717 1799 1897 2046",
        "  13689 0 0 0 -2.6368934905978243 1757 1872 2045",
        "  13690 0 0 0 -2.6368934905978243 1712 1844 2045",
        "  13691 0 0 0 -2.637240914590068 1663 1815 2045",
        "  13692 0 0 0 -2.6376046908739843 1675 1822 2045",
        "14097 473670.7310550273 563149.0695776378 -36793.41114597913 474126.8848935282 562161.4676874985 -36017.579108607606 1 14 0 -1.176028873393896 0.5083793621811908 -113.4065934065934 41.51404151404154 107.15506715506717",
        "> 14123 473654.63107038144 563142.0695843135 -36793.44270646264 -138 -60 -0.07772742948452475",
        "  14124 0 -126 0 -1.1775961904028807 1996 2024 2048",
        "  14125 0 -126 0 -1.178874638087544 1995 2019 2051",
        "  14126 0 -126 0 -1.1807370172498923 1996 2009 2057",
        "  14127 0 -126 0 -1.1816404865335288 1997 1996 2065",
        "  14128 0 -126 0 -1.1820495206447772 1998 1980 2074",
        "  14129 0 -126 0 -1.1824944196162732 2000 1961 2086",
        "  14130 0 -126 0 -1.1821982287058366 2002 1938 2100",
        "  14131 0 -126 0 -1.1821520533478742 2005 1912 2115",
        "  14132 0 -126 0 -1.1824691130659295 2008 1883 2133",
        "  14133 0 -126 0 -1.1823452478848004 2012 1851 2153",
        "  14134 0 -126 0 -1.1817668988439418 2017 1816 2175",
        "  14135 0 -126 0 -1.1812805424264132 2023 1778 2198",
        "  14136 0 -126 0 -1.1806268130415383 2030 1736 2224",
        "  14137 0 -126 0 -1.1786866339899953 2038 1692 2252",
        "  14138 0 -126 0 -1.1772342298661318 2048 1645 2282",
        "  14139 0 -126 0 -1.1762231757943804 2060 1598 2312",
        "  14140 0 -126 0 -1.1721324312556445 2073 1551 2342",
        "  14141 0 -126 0 -1.1678941466673955 2088 1506 2372",
        "  14142 0 -126 0 -1.1638276143668738 2108 1466 2400",
        "  14143 0 126 0 -1.1606837421966494 2163 1479 2406",
        "  14144 0 126 0 -1.1602073120918712 2217 1496 2410",
        "  14145 0 126 0 -1.159803326546516 2271 1516 2411",
        "  14146 0 126 0 -1.159803326546516 2322 1539 2410",
        "  14147 0 126 0 -1.1593679402906225 2372 1567 2406",
        "  14148 0 126 0 -1.1593679402906225 2419 1597 2400",
        "  14149 0 126 0 -1.158272614572843 2459 1627 2393",
        "  14150 0 126 0 -1.1579408220089784 2494 1657 2384",
        "  14151 0 126 1 -1.156959198143888 2510 1689 2398",
        "  14152 0 126 1 -1.1568477414417309 2525 1720 2409",
        "  14153 0 126 1 -1.156404594700732 2539 1748 2415",
        "  14154 0 126 1 -1.155425028944995 2535 1750 2393",
    ];

    /// <summary>Per throw: the tick the hook holds on, the pull's first tick, the tick its end releases the target, the tick the item clears.</summary>
    private static readonly (uint? Attach, uint? PullFirst, uint? PullEnd, uint Cleared)[] s_events =
    [
        (11923, 11934, 11956, 11956), (12147, 12158, 12180, 12181), (12486, 12497, 12522, 12523), (null, null, null, 12937),
        (null, null, null, 13163), (null, null, null, 13227), (null, null, null, 13455), (13503, null, null, 13512),
        (13665, 13676, 13692, 13692), (14112, 14123, 14154, 14155),
    ];

    [Fact]
    public void Ten_throws_attach_pull_and_clear_on_the_ticks_the_client_predicts_and_pulls_move_at_its_velocity()
    {
        List<(string[] Throw, List<string[]> Pull)> throws = [];
        foreach (string row in s_throws)
        {
            string[] cells = row.Trim().Split(' ');
            if (row.StartsWith(' ') || row.StartsWith('>'))
            {
                throws[^1].Pull.Add(cells);
            }
            else
            {
                throws.Add((cells, []));
            }
        }

        Assert.Equal(s_events.Length, throws.Count);
        List<string> differences = [];
        int compared = 0;
        for (int index = 0; index < throws.Count; index++)
        {
            (string[] header, List<string[]> pull) = throws[index];
            uint thrownAt = Tick(header[0]);
            Vector3D feet = Vector(header, 1);
            Vector3D target = Vector(header, 4);
            uint[] presses = [.. header[9].Split(',').Select(Tick).Where(tick => tick != 0)];
            Dictionary<uint, string[]> steps = pull.Skip(1).ToDictionary(cells => Tick(cells[0]));
            (Vector3D Position, Vector3D Velocity)? open = null;
            SnapHookUse use = SnapHookUse.None;
            (uint? attach, uint? pullFirst, uint? pullEnd, uint? cleared) = (null, null, null, null);
            for (uint tick = thrownAt; cleared is null && tick < thrownAt + 120; tick++)
            {
                SnapHookControls controls = new(true, tick == thrownAt, tick == thrownAt, presses.Contains(tick), presses.Contains(tick), 0, 0);
                use = use.StepItem(controls, tick, feet, 0, null);
                if (tick == thrownAt)
                {
                    // The client's target in place of the aim, which needs the level.
                    use = use with
                    {
                        Hook = use.Hook with { HasTarget = header[7] == "1", AttachPoint = target, Surface = (PhysicalSurfaceType)int.Parse(header[8], CultureInfo.InvariantCulture) },
                        LastPoint = target,
                    };
                }

                attach ??= use.Hook.Attached ? tick : null;
                cleared ??= tick > thrownAt && !use.Hook.Thrown ? tick : null;
                int time = SnapHookUse.Time(tick);
                if (use.Pull is not { } running)
                {
                    if (use.Target is { } created && SnapHookMovement.CanStart(created, time))
                    {
                        string[] start = pull[0];
                        Assert.Equal(Tick(start[1]), tick);
                        pullFirst = tick;
                        open = (Vector(start, 2), Vector(start, 5));
                        use = use with { Pull = SnapHookMovement.Begin(created, open.Value.Position with { Z = open.Value.Position.Z + DamPlayerStarts.Capsule.HalfHeight }) };
                    }

                    continue;
                }

                (Vector3D position, Vector3D velocity) = open!.Value;
                string[] cells = steps[tick];
                MovementInput movement = new(tick, sbyte.Parse(cells[1], CultureInfo.InvariantCulture), sbyte.Parse(cells[2], CultureInfo.InvariantCulture),
                    cells[3] == "1" ? MovementButtons.Jump : MovementButtons.None);
                Vector3D direction = FlatGroundMovement.Direction(movement, double.Parse(cells[4], CultureInfo.InvariantCulture));
                SnapHookStep step = SnapHookMovement.Step(running, new SnapHookInput(direction, cells[3] == "1", presses.Contains(tick)),
                    position with { Z = position.Z + DamPlayerStarts.Capsule.HalfHeight }, velocity, new SnapHookGround(0, true), time, PawnBlackboard.TickSeconds);
                Vector3D ground = FlatGroundMovement.StepVelocity(velocity, direction, new Vector3D(0, 0, 1), PawnBlackboard.TickSeconds, GroundMovementParameters.Armed,
                    true, true);
                Vector3D next = step.Exit == SnapHookExit.None ? SnapHookMovement.BlendIn(ground, step.Velocity, step.State.Ticks) : step.Velocity;
                open = (new Vector3D(position.X + (next.X * PawnBlackboard.TickSeconds), position.Y + (next.Y * PawnBlackboard.TickSeconds),
                    position.Z + (next.Z * PawnBlackboard.TickSeconds)), next);
                use = use with { Pull = step.Exit == SnapHookExit.None ? step.State : null, Target = step.State.Target };
                pullEnd = step.Exit == SnapHookExit.None ? null : tick;
                compared++;
                string encoded = string.Join(' ', new[] { next.X, next.Y, next.Z }.Select(value => Encoded(value).ToString(CultureInfo.InvariantCulture)));
                if (encoded != string.Join(' ', cells[5..8]))
                {
                    differences.Add($"{tick}: {string.Join(' ', cells[5..8])} / {encoded}");
                }
            }

            Assert.Equal(s_events[index], (attach, pullFirst, pullEnd, cleared ?? 0));
        }

        // 113 of 116 pull steps give the client's velocity exactly. The steps here move without collision: the first pull grazes
        // the ground on its last two ticks; the last pull's end step, steered and jumping, is a few steps off.
        Assert.Equal(116, compared);
        Assert.True(differences.Select(difference => difference[..5]).SequenceEqual(["11955", "11956", "14154"]),
            $"{differences.Count} pull ticks differ (tick: client / Raid):\n{string.Join('\n', differences)}");
    }

    // A velocity component as the pawn frame encodes it.
    private static long Encoded(double velocity)
    {
        PendulumScalarField field = (PendulumScalarField)PendulumRegistry.Descriptors[PawnBlackboard.InitialState
            .Single(variable => variable.Slot == PawnBlackboard.MovementStateSlot).DescriptorId].Fields[PawnBlackboard.VelocityXField];
        return (long)Math.Ceiling(((velocity + PawnBlackboard.VelocityRange) * (field.Maximum - 1) / (2 * PawnBlackboard.VelocityRange)) - 0.5);
    }

    [Fact]
    public void A_throw_that_hits_nothing_ends_where_the_crosshair_and_the_throw_point_it()
    {
        foreach (string row in s_throws.Where(row => !row.StartsWith(' ') && !row.StartsWith('>') && row.Split(' ')[7] == "0"))
        {
            string[] cells = row.Split(' ');
            SnapHookHook hook = SnapHookAim.Throw(0, Vector(cells, 1), double.Parse(cells[10], CultureInfo.InvariantCulture),
                double.Parse(cells[11], CultureInfo.InvariantCulture), Vector(cells, 12), null);
            Assert.False(hook.HasTarget);
            Vector3D target = Vector(cells, 4);
            Assert.True(Distance(hook.AttachPoint, target) < 1, $"{cells[0]}: {hook.AttachPoint} / {target}");
        }
    }

    [Fact]
    public void The_item_node_shows_aiming_the_flight_the_hold_and_the_call_back_and_keeps_the_latest_throw()
    {
        const uint Node = 100;
        const uint Thrown = 120;
        SnapHookUse use = SnapHookUse.None;
        Assert.Equal((0UL, 0UL, 1UL, s_never), Fields(use));
        use = use.StepItem(new SnapHookControls(true, false, false, true, true, 0, 0), 110, default, 0, null);
        Assert.Equal((Bit(QuickItemProtocol.AimingBit), (ulong)QuickItemProtocol.ItemUseAiming, 1UL, s_never), Fields(use));
        use = use.StepItem(new SnapHookControls(true, true, true, true, false, 0, 0), Thrown, default, 0, null);
        ulong flying = Bit(QuickItemProtocol.HookOutBit) | Bit(QuickItemProtocol.HookFlyingBit);
        ulong thrownAt = PawnSlideState.TimeBias + (ulong)SnapHookUse.Time(Thrown);
        Assert.Equal((flying, (ulong)QuickItemProtocol.ItemUseThrown, 1UL, thrownAt), Fields(use));
        // The node stays fresh until the primary use is let go.
        use = use.StepItem(new SnapHookControls(true, false, false, true, false, 0, 0), Thrown + 1, default, 0, null);
        Assert.Equal((flying, (ulong)QuickItemProtocol.ItemUseThrown, 0UL, thrownAt), Fields(use));
        use = use with { Hook = use.Hook with { Attached = true, HasTarget = true, PullsPlayer = true, Surface = PhysicalSurfaceType.Concrete } };
        ulong held = Bit(QuickItemProtocol.HookOutBit) | Bit(QuickItemProtocol.HookAttachedBit) | Bit(QuickItemProtocol.HookTargetBit) | Bit(QuickItemProtocol.HookPullsBit);
        Assert.Equal((held, (ulong)QuickItemProtocol.ItemUseThrown, 0UL, thrownAt), Fields(use));
        Assert.Equal((ulong)PhysicalSurfaceType.Concrete, Value(use, QuickItemProtocol.SurfaceField));

        // Calling back a hook that holds on leaves the cancelled bit; a secondary use still held does not aim.
        use = use.StepItem(new SnapHookControls(true, false, false, true, true, 0, 0), Thrown + 20, default, 0, null);
        Assert.Equal((Bit(QuickItemProtocol.HookCancelledBit), (ulong)QuickItemProtocol.ItemUseThrown, 0UL, s_never), Fields(use));
        Assert.Equal(PawnSlideState.TimeBias + (ulong)SnapHookUse.Time(Thrown), Value(use, QuickItemProtocol.LastThrowTimeField));
        Assert.Equal(0UL, Value(use, QuickItemProtocol.SurfaceField));
        use = use.StepItem(new SnapHookControls(true, false, false, true, false, 0, 0), Thrown + 21, default, 0, null);
        Assert.Equal((Bit(QuickItemProtocol.HookCancelledBit), (ulong)QuickItemProtocol.ItemUseThrown, 0UL, s_never), Fields(use));
        use = use.StepItem(new SnapHookControls(true, false, false, true, true, 0, 0), Thrown + 22, default, 0, null);
        Assert.Equal(Bit(QuickItemProtocol.HookCancelledBit) | Bit(QuickItemProtocol.AimingBit), Value(use, QuickItemProtocol.HookStateField));

        // A node laid out after the throw knows nothing of it.
        Assert.Equal((0UL, 0UL, 1UL, s_never), Fields(use with { Aiming = false }, Thrown + 30));
        return;

        (ulong Bits, ulong Use, ulong Fresh, ulong Thrown) Fields(SnapHookUse state, uint from = Node) =>
            (Value(state, QuickItemProtocol.HookStateField, from), Value(state, QuickItemProtocol.ItemUseField, from), Value(state, QuickItemProtocol.UnknownFreshField, from),
                Value(state, QuickItemProtocol.ThrowTimeField, from));
    }

    [Fact]
    public void A_player_throws_at_a_wall_is_pulled_along_its_reports_with_frames_held_and_lands_back_in_ground_movement()
    {
        (Dictionary<uint, CharacterState> states, uint pullFirst, uint end) = PullAtWall(cancelAt: null);
        Vector3D target = states[Throw].SnapHook.Hook.AttachPoint;
        Assert.True(states[Throw].SnapHook.Hook.HasTarget);
        Assert.Equal(WallFace, target.X, 3);
        // The crosshair ray from the camera meets the wall where the throw then hits it (to the camera offset's step).
        Assert.Equal(s_camera.Y, target.Y, 0.1);
        Assert.Equal(s_camera.Z, target.Z, 0.1);
        Assert.False(states[Throw + 13].SnapHook.Hook.Attached);
        Assert.True(states[Throw + 14].SnapHook.Hook.Attached);
        Assert.Equal(SnapHookUse.Time(Throw + 14) + SnapHookThrow.TimeUnits(SnapHookParameters.PullDelaySeconds), states[Throw + 14].SnapHook.Target!.Value.CreatedAt);
        Assert.Equal(Throw + 25, pullFirst);
        Assert.Null(states[pullFirst - 1].SnapHook.Pull);
        foreach (uint frame in Enumerable.Range((int)pullFirst, (int)(end - pullFirst)).Select(frame => (uint)frame))
        {
            Assert.Equal(SnapHookParameters.PullingFlags, states[frame].GraphFlags & SnapHookParameters.PullingFlags);
            // The reports' coarser grid.
            Assert.Equal(Report(frame + 1).X, states[frame].Position.X, 0);
        }

        Assert.All(Enumerable.Range((int)Throw, (int)(end - Throw + 1)).Select(frame => states[(uint)frame]), state => Assert.True(state.SnapHook.Busy));
        Assert.Equal(SnapHookUse.Time(end), states[end].SnapHook.Target!.Value.RemovedAt);
        Assert.True(states[end].SnapHook.Hook.Thrown);
        Assert.False(states[end + 1].SnapHook.Busy);
        Assert.Equal(0UL, states[end + 1].GraphFlags & SnapHookParameters.PullingFlags);
        Assert.Equal(0UL, Value(states[end + 1].SnapHook, QuickItemProtocol.HookStateField, Start));
    }

    [Fact]
    public void A_secondary_press_during_the_pull_calls_the_hook_back_and_ends_the_pull_on_that_tick()
    {
        const uint Cancel = Throw + 32;
        (Dictionary<uint, CharacterState> states, _, uint end) = PullAtWall(Cancel);
        Assert.Equal(Cancel, end);
        Assert.NotNull(states[Cancel - 1].SnapHook.Pull);
        Assert.False(states[Cancel].SnapHook.Busy);
        Assert.Equal(SnapHookUse.Time(Cancel), states[Cancel].SnapHook.Target!.Value.RemovedAt);
        Assert.Equal(Bit(QuickItemProtocol.HookCancelledBit), Value(states[Cancel].SnapHook, QuickItemProtocol.HookStateField, Start));
    }

    [Fact]
    public void A_throw_whose_frames_arrive_late_is_simulated_again_into_the_same_states()
    {
        (Dictionary<uint, CharacterState> inOrder, _, _) = PullAtWall(cancelAt: null);
        // The three packets carrying the throw's frame come after three later ones.
        List<uint> packets = [.. ScriptedClient.InOrder(Start + 3, Throw - 1), Throw + 3, Throw + 4, Throw + 5, Throw, Throw + 1, Throw + 2,
            .. ScriptedClient.InOrder(Throw + 6, Throw + 80)];
        Dictionary<uint, CharacterState> late = ScriptedClient.Deliver(Movement(), frame => Script(frame, null), packets, heldItem: s_ready)
            .ToDictionary(state => state.Frame);
        foreach (uint frame in late.Keys.Where(frame => frame is >= Throw and < Throw + 70))
        {
            Assert.Equal(inOrder[frame].SnapHook, late[frame].SnapHook);
        }
    }

    private const uint Start = 30;
    private const uint Throw = Start + 10;
    private const double WallFace = 1190;
    private const double PullSpeed = 60;
    private static readonly ulong s_never = ((PendulumScalarField)PendulumRegistry.Descriptors[QuickItemProtocol.ItemNodeDescriptor].Fields[QuickItemProtocol.ThrowTimeField]).Default;
    private static readonly HeldItemInput s_ready = new(HeldItemMovementParameters.Default) { SnapHookReady = true };

    // Behind, to the right of and above the feet.
    private static readonly Vector3D s_camera = new(-120, 41.5, 178);

    // A floor and, 11.9 m ahead along the heading, a wall.
    private static readonly CollisionWorld s_world = ScriptedClient.World((new Vector3D(0, 0, -10), new Vector3(3000, 3000, 10)),
        (new Vector3D(WallFace + 10, 0, 300), new Vector3(10, 1000, 300)));

    private static PlayerMovement Movement() => new(Start, default, 0, s_world, double.PositiveInfinity);

    // Throws at the wall, then reports the character pulled towards it from the pull's second tick on.
    private static (Dictionary<uint, CharacterState> States, uint PullFirst, uint End) PullAtWall(uint? cancelAt)
    {
        Dictionary<uint, CharacterState> states = ScriptedClient.Deliver(Movement(), frame => Script(frame, cancelAt),
            ScriptedClient.InOrder(Start + 3, Throw + 80), heldItem: s_ready).ToDictionary(state => state.Frame);
        uint pullFirst = states.Keys.Order().First(frame => states[frame].SnapHook.Pull is not null);
        uint end = states.Keys.Order().First(frame => frame > pullFirst && states[frame].SnapHook.Pull is null);
        return (states, pullFirst, end);
    }

    private static ScriptedFrame Script(uint frame, uint? cancelAt)
    {
        const ulong Allowed = 1UL << WeaponProtocol.AllowPrimaryBit;
        ulong flags = frame == Throw ? Allowed | (1UL << WeaponProtocol.FireDownBit) | (1UL << WeaponProtocol.FirePressedBit) : Allowed;
        flags |= frame == cancelAt ? 1UL << WeaponProtocol.SecondaryDownBit : 0;
        return new ScriptedFrame(Report(frame), WeaponFlags: flags, Camera: s_camera);
    }

    // Where the client puts the character: standing until the pull, then moving towards the wall.
    private static Vector3D Report(uint frame) => new(Math.Max(0, (int)frame - (int)(Throw + 25)) * PullSpeed, 0, 0);

    private static ulong Bit(int bit) => 1UL << bit;

    private static ulong Value(SnapHookUse use, int field, uint nodeFrom = 100) => use.ItemNodeValues(nodeFrom).Single(value => value.Field == field).Value;

    private static double Distance(Vector3D a, Vector3D b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)) + ((a.Z - b.Z) * (a.Z - b.Z)));

    private static uint Tick(string text) => uint.Parse(text, CultureInfo.InvariantCulture);

    private static Vector3D Vector(string[] cells, int first) =>
        new(double.Parse(cells[first], CultureInfo.InvariantCulture), double.Parse(cells[first + 1], CultureInfo.InvariantCulture),
            double.Parse(cells[first + 2], CultureInfo.InvariantCulture));
}
