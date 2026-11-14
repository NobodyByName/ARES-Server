namespace Ares.Structs.Game.Movement;

/// <summary>Animations a heavy landing plays, by landing level and direction.</summary>
public static class HeavyLandingAnimations
{
    /// <summary>The <c>HM_Landing_A_Com_R_HeavyFallBwd_00</c> animation.</summary>
    public static HeavyLandingClip HeavyFallBackward { get; } = new(
        "HM_Landing_A_Com_R_HeavyFallBwd_00", 30.0,
        [
            new HeavyLandingRootPoint(0.0f, 0.0f), new HeavyLandingRootPoint(0.0f, -9.94745731f), new HeavyLandingRootPoint(0.0f, -18.7187805f), new HeavyLandingRootPoint(0.0f, -26.3512497f),
            new HeavyLandingRootPoint(0.0f, -33.5402107f), new HeavyLandingRootPoint(0.0f, -40.3916664f), new HeavyLandingRootPoint(0.0f, -46.7622986f), new HeavyLandingRootPoint(0.0f, -52.2629509f),
            new HeavyLandingRootPoint(0.0f, -57.33078f), new HeavyLandingRootPoint(0.0f, -62.152626f), new HeavyLandingRootPoint(0.0f, -66.7207336f), new HeavyLandingRootPoint(0.0f, -71.3332825f),
            new HeavyLandingRootPoint(0.0f, -76.0362778f), new HeavyLandingRootPoint(0.0f, -80.7465973f), new HeavyLandingRootPoint(0.0f, -85.2875366f), new HeavyLandingRootPoint(0.0f, -89.4191284f),
            new HeavyLandingRootPoint(0.0f, -92.8368301f), new HeavyLandingRootPoint(0.0f, -95.6785049f), new HeavyLandingRootPoint(0.0f, -98.0265045f), new HeavyLandingRootPoint(0.0f, -99.6778259f),
            new HeavyLandingRootPoint(0.0f, -100.470901f), new HeavyLandingRootPoint(0.0f, -100.566483f), new HeavyLandingRootPoint(0.0f, -100.231026f), new HeavyLandingRootPoint(0.0f, -99.5568542f),
            new HeavyLandingRootPoint(0.0f, -98.5146713f), new HeavyLandingRootPoint(0.0f, -97.2974625f), new HeavyLandingRootPoint(0.0f, -96.0321503f), new HeavyLandingRootPoint(0.0f, -95.0737152f),
            new HeavyLandingRootPoint(0.0f, -94.3315277f), new HeavyLandingRootPoint(0.0f, -93.7034531f), new HeavyLandingRootPoint(0.0f, -93.0796509f), new HeavyLandingRootPoint(0.0f, -92.3740311f),
            new HeavyLandingRootPoint(0.0f, -91.5703888f), new HeavyLandingRootPoint(0.0f, -90.6772308f), new HeavyLandingRootPoint(0.0f, -89.9078369f), new HeavyLandingRootPoint(0.0f, -89.6210251f),
            new HeavyLandingRootPoint(0.0f, -89.4233093f), new HeavyLandingRootPoint(0.0f, -89.2255936f), new HeavyLandingRootPoint(0.0f, -89.0080032f), new HeavyLandingRootPoint(0.0f, -88.6283569f),
            new HeavyLandingRootPoint(0.0f, -87.9052277f), new HeavyLandingRootPoint(0.0f, -86.4194641f), new HeavyLandingRootPoint(0.0f, -85.3414078f), new HeavyLandingRootPoint(0.0f, -84.4118042f),
            new HeavyLandingRootPoint(0.0f, -83.5708389f), new HeavyLandingRootPoint(0.0f, -83.1214523f), new HeavyLandingRootPoint(0.0f, -83.0495071f), new HeavyLandingRootPoint(0.0f, -83.2624741f),
            new HeavyLandingRootPoint(0.0f, -83.6238937f), new HeavyLandingRootPoint(0.0f, -84.1787109f), new HeavyLandingRootPoint(0.0f, -85.1687851f), new HeavyLandingRootPoint(0.0f, -86.0901642f),
            new HeavyLandingRootPoint(0.0f, -86.707756f), new HeavyLandingRootPoint(0.0f, -87.2049026f), new HeavyLandingRootPoint(0.0f, -87.5606003f), new HeavyLandingRootPoint(0.0f, -87.6133118f),
            new HeavyLandingRootPoint(0.0f, -87.5423126f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 1.01676166f, 1.86666656f),
        ]);

    /// <summary>The <c>HM_Landing_A_Com_R_HeavyFallFwd_00</c> animation.</summary>
    public static HeavyLandingClip HeavyFallForward { get; } = new(
        "HM_Landing_A_Com_R_HeavyFallFwd_00", 30.0,
        [
            new HeavyLandingRootPoint(0.0f, 0.0f), new HeavyLandingRootPoint(0.0f, 5.66465855f), new HeavyLandingRootPoint(0.0f, 11.001317f), new HeavyLandingRootPoint(0.0f, 17.0160694f),
            new HeavyLandingRootPoint(0.0f, 22.763876f), new HeavyLandingRootPoint(0.0f, 28.3143406f), new HeavyLandingRootPoint(0.0f, 33.9049339f), new HeavyLandingRootPoint(0.0f, 38.6508331f),
            new HeavyLandingRootPoint(0.0f, 41.5516052f), new HeavyLandingRootPoint(0.0f, 44.8087845f), new HeavyLandingRootPoint(0.0f, 48.6112328f), new HeavyLandingRootPoint(0.0f, 52.9703064f),
            new HeavyLandingRootPoint(0.0f, 57.3447838f), new HeavyLandingRootPoint(0.0f, 61.4650002f), new HeavyLandingRootPoint(0.0f, 64.9787064f), new HeavyLandingRootPoint(0.0f, 67.9896011f),
            new HeavyLandingRootPoint(0.0f, 70.5552368f), new HeavyLandingRootPoint(0.0f, 72.6950073f), new HeavyLandingRootPoint(0.0f, 74.5884705f), new HeavyLandingRootPoint(0.0f, 76.6023331f),
            new HeavyLandingRootPoint(0.0f, 79.0456924f), new HeavyLandingRootPoint(0.0f, 81.6133957f), new HeavyLandingRootPoint(0.0f, 84.1660156f), new HeavyLandingRootPoint(0.0f, 86.2491379f),
            new HeavyLandingRootPoint(0.0f, 88.0426865f), new HeavyLandingRootPoint(0.0f, 89.5506668f), new HeavyLandingRootPoint(0.0f, 90.8248215f), new HeavyLandingRootPoint(0.0f, 91.9144897f),
            new HeavyLandingRootPoint(0.0f, 92.6096725f), new HeavyLandingRootPoint(0.0f, 93.0801392f), new HeavyLandingRootPoint(0.0f, 93.7023468f), new HeavyLandingRootPoint(0.0f, 94.5564575f),
            new HeavyLandingRootPoint(0.0f, 95.7556229f), new HeavyLandingRootPoint(0.0f, 97.1594543f), new HeavyLandingRootPoint(0.0f, 98.5376587f), new HeavyLandingRootPoint(0.0f, 100.19014f),
            new HeavyLandingRootPoint(0.0f, 101.32991f), new HeavyLandingRootPoint(0.0f, 102.460907f), new HeavyLandingRootPoint(0.0f, 103.448059f), new HeavyLandingRootPoint(0.0f, 104.166817f),
            new HeavyLandingRootPoint(0.0f, 104.509377f), new HeavyLandingRootPoint(0.0f, 104.470978f), new HeavyLandingRootPoint(0.0f, 104.156075f), new HeavyLandingRootPoint(0.0f, 103.517303f),
            new HeavyLandingRootPoint(0.0f, 102.567123f), new HeavyLandingRootPoint(0.0f, 101.474716f), new HeavyLandingRootPoint(0.0f, 100.424591f), new HeavyLandingRootPoint(0.0f, 99.3984222f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 0.973906457f, 1.5666666f),
        ]);

    /// <summary>The <c>HM_Landing_A_Com_R_HeavyFallLeft_00</c> animation.</summary>
    public static HeavyLandingClip HeavyFallLeft { get; } = new(
        "HM_Landing_A_Com_R_HeavyFallLeft_00", 30.0,
        [
            new HeavyLandingRootPoint(10.1766539f, -1.52955222f), new HeavyLandingRootPoint(2.63875294f, -3.86911488f), new HeavyLandingRootPoint(-4.04750776f, -6.10290909f), new HeavyLandingRootPoint(-10.2773829f, -8.27098751f),
            new HeavyLandingRootPoint(-16.2128696f, -10.4942093f), new HeavyLandingRootPoint(-22.1482792f, -11.4306822f), new HeavyLandingRootPoint(-28.4615784f, -12.0578241f), new HeavyLandingRootPoint(-34.7866516f, -13.2644196f),
            new HeavyLandingRootPoint(-41.871685f, -13.7410822f), new HeavyLandingRootPoint(-48.1312103f, -14.1372528f), new HeavyLandingRootPoint(-54.0663795f, -14.390749f), new HeavyLandingRootPoint(-60.1583481f, -13.6672544f),
            new HeavyLandingRootPoint(-65.6369019f, -12.3896179f), new HeavyLandingRootPoint(-70.0997849f, -11.4169521f), new HeavyLandingRootPoint(-73.718605f, -11.1126451f), new HeavyLandingRootPoint(-76.7268524f, -10.9348555f),
            new HeavyLandingRootPoint(-79.2525253f, -10.4802809f), new HeavyLandingRootPoint(-81.0689774f, -9.64238834f), new HeavyLandingRootPoint(-82.2713928f, -8.71329784f), new HeavyLandingRootPoint(-82.6731339f, -7.95496988f),
            new HeavyLandingRootPoint(-82.437912f, -7.76571798f), new HeavyLandingRootPoint(-81.8474045f, -7.7703433f), new HeavyLandingRootPoint(-81.4497681f, -7.71949673f), new HeavyLandingRootPoint(-81.3780212f, -7.46709347f),
            new HeavyLandingRootPoint(-81.4629669f, -6.97414446f), new HeavyLandingRootPoint(-81.6803207f, -6.43572569f), new HeavyLandingRootPoint(-81.9734344f, -5.84314346f), new HeavyLandingRootPoint(-82.2662506f, -4.94844294f),
            new HeavyLandingRootPoint(-82.2585526f, -3.52773452f), new HeavyLandingRootPoint(-81.7905807f, -1.54882205f), new HeavyLandingRootPoint(-81.4779663f, 0.0303973593f), new HeavyLandingRootPoint(-80.8965683f, 1.4700321f),
            new HeavyLandingRootPoint(-80.4003067f, 2.24414515f), new HeavyLandingRootPoint(-79.829689f, 2.32807946f), new HeavyLandingRootPoint(-79.5022812f, 2.04826856f), new HeavyLandingRootPoint(-79.3757019f, 1.45402348f),
            new HeavyLandingRootPoint(-79.189621f, 0.667190909f), new HeavyLandingRootPoint(-78.8554459f, 0.200135961f), new HeavyLandingRootPoint(-78.4637527f, 0.180446744f), new HeavyLandingRootPoint(-78.1805344f, 0.469988763f),
            new HeavyLandingRootPoint(-78.0526657f, 1.04494834f), new HeavyLandingRootPoint(-78.0204239f, 1.95790899f), new HeavyLandingRootPoint(-77.9520416f, 3.45464993f), new HeavyLandingRootPoint(-78.2056961f, 4.73752213f),
            new HeavyLandingRootPoint(-78.4346924f, 5.95433283f), new HeavyLandingRootPoint(-78.5789261f, 7.15463161f), new HeavyLandingRootPoint(-78.6306458f, 8.20036125f), new HeavyLandingRootPoint(-78.5226288f, 9.08620548f),
            new HeavyLandingRootPoint(-78.2562027f, 9.81823158f), new HeavyLandingRootPoint(-77.8447952f, 10.432394f), new HeavyLandingRootPoint(-77.2631836f, 10.9090481f), new HeavyLandingRootPoint(-76.6192551f, 11.2421103f),
            new HeavyLandingRootPoint(-76.038826f, 11.380826f), new HeavyLandingRootPoint(-75.4734039f, 11.5129347f), new HeavyLandingRootPoint(-75.4734039f, 11.5129347f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 0.994777143f, 1.77910352f),
        ]);

    /// <summary>The <c>HM_Landing_A_Com_R_HeavyFallOnSpot_00</c> animation.</summary>
    public static HeavyLandingClip HeavyFallOnSpot { get; } = new(
        "HM_Landing_A_Com_R_HeavyFallOnSpot_00", 30.0,
        [
            new HeavyLandingRootPoint(15.0675354f, -11.053688f), new HeavyLandingRootPoint(15.0243101f, -9.06463432f), new HeavyLandingRootPoint(15.1626911f, -9.19991779f), new HeavyLandingRootPoint(14.6859064f, -9.12481976f),
            new HeavyLandingRootPoint(12.9795094f, -8.52669525f), new HeavyLandingRootPoint(10.782896f, -7.44348907f), new HeavyLandingRootPoint(8.21645546f, -6.09247303f), new HeavyLandingRootPoint(5.59995651f, -4.83111906f),
            new HeavyLandingRootPoint(2.98402429f, -3.35046101f), new HeavyLandingRootPoint(1.25533831f, -2.25475526f), new HeavyLandingRootPoint(0.355882734f, -1.38350844f), new HeavyLandingRootPoint(0.0f, 0.0f),
            new HeavyLandingRootPoint(-0.166776866f, 1.84681785f), new HeavyLandingRootPoint(-0.359168977f, 3.79346871f), new HeavyLandingRootPoint(-0.751896203f, 5.77133608f), new HeavyLandingRootPoint(-1.3124696f, 7.80658627f),
            new HeavyLandingRootPoint(-2.0689652f, 9.83312225f), new HeavyLandingRootPoint(-2.87407088f, 11.777524f), new HeavyLandingRootPoint(-3.6324873f, 13.5636005f), new HeavyLandingRootPoint(-4.19365072f, 15.1015024f),
            new HeavyLandingRootPoint(-4.55452919f, 16.4078941f), new HeavyLandingRootPoint(-4.89430952f, 17.4077053f), new HeavyLandingRootPoint(-5.312047f, 17.9541893f), new HeavyLandingRootPoint(-5.66993856f, 18.0706062f),
            new HeavyLandingRootPoint(-5.85539579f, 17.7626305f), new HeavyLandingRootPoint(-5.90898466f, 17.0727997f), new HeavyLandingRootPoint(-5.89292049f, 16.1208801f), new HeavyLandingRootPoint(-5.82617855f, 14.9643097f),
            new HeavyLandingRootPoint(-5.71536303f, 13.6925411f), new HeavyLandingRootPoint(-5.51574516f, 12.4189606f), new HeavyLandingRootPoint(-5.15458107f, 11.1966639f), new HeavyLandingRootPoint(-4.5765748f, 10.1479053f),
            new HeavyLandingRootPoint(-3.76206708f, 9.4889164f), new HeavyLandingRootPoint(-2.8328948f, 9.43671894f), new HeavyLandingRootPoint(-1.97241497f, 10.107317f), new HeavyLandingRootPoint(-1.25482285f, 11.4027233f),
            new HeavyLandingRootPoint(-0.61833626f, 13.2173195f), new HeavyLandingRootPoint(-0.0383351482f, 15.0832014f), new HeavyLandingRootPoint(0.476729363f, 16.4288883f), new HeavyLandingRootPoint(1.0520792f, 17.2322426f),
            new HeavyLandingRootPoint(1.55158949f, 17.5910473f), new HeavyLandingRootPoint(1.91635823f, 17.6204338f), new HeavyLandingRootPoint(2.12266755f, 17.3679295f), new HeavyLandingRootPoint(2.28453994f, 16.8085327f),
            new HeavyLandingRootPoint(2.15650773f, 16.2467442f), new HeavyLandingRootPoint(1.92036819f, 15.9137611f), new HeavyLandingRootPoint(1.67143381f, 15.9407825f), new HeavyLandingRootPoint(1.3892287f, 16.4626675f),
            new HeavyLandingRootPoint(1.13971901f, 17.5026474f), new HeavyLandingRootPoint(0.761517525f, 18.8935356f), new HeavyLandingRootPoint(0.157072201f, 20.3470058f), new HeavyLandingRootPoint(-0.572050929f, 21.2348213f),
            new HeavyLandingRootPoint(-1.15529811f, 22.1982822f), new HeavyLandingRootPoint(-1.55731189f, 23.0109234f), new HeavyLandingRootPoint(-1.89153826f, 23.7138557f), new HeavyLandingRootPoint(-2.17783475f, 24.2829151f),
            new HeavyLandingRootPoint(-2.35342264f, 24.7137299f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 0.999426126f, 1.86666667f),
        ]);

    /// <summary>The <c>HM_Landing_A_Com_R_HeavyFallRight_00</c> animation.</summary>
    public static HeavyLandingClip HeavyFallRight { get; } = new(
        "HM_Landing_A_Com_R_HeavyFallRight_00", 30.0,
        [
            new HeavyLandingRootPoint(0.0f, 0.0f), new HeavyLandingRootPoint(7.71028042f, -1.60369575f), new HeavyLandingRootPoint(13.7204409f, -3.81384277f), new HeavyLandingRootPoint(20.7142448f, -3.909935f),
            new HeavyLandingRootPoint(27.6842175f, -3.44978428f), new HeavyLandingRootPoint(35.2333031f, -2.64686656f), new HeavyLandingRootPoint(43.4207344f, -1.46586156f), new HeavyLandingRootPoint(51.6315918f, -0.992213726f),
            new HeavyLandingRootPoint(58.7377205f, -0.521104991f), new HeavyLandingRootPoint(66.6075439f, 1.08928633f), new HeavyLandingRootPoint(76.3486786f, 2.67142653f), new HeavyLandingRootPoint(83.2916718f, 4.0138011f),
            new HeavyLandingRootPoint(90.3859482f, 5.74752188f), new HeavyLandingRootPoint(95.5934448f, 6.53035069f), new HeavyLandingRootPoint(99.0858459f, 6.46186304f), new HeavyLandingRootPoint(101.42218f, 5.37911606f),
            new HeavyLandingRootPoint(103.887573f, 3.85401988f), new HeavyLandingRootPoint(106.062607f, 2.4294498f), new HeavyLandingRootPoint(108.059189f, 1.06555665f), new HeavyLandingRootPoint(109.811829f, -0.264654279f),
            new HeavyLandingRootPoint(111.464287f, -1.44642186f), new HeavyLandingRootPoint(113.010826f, -2.28120065f), new HeavyLandingRootPoint(114.550148f, -2.90879512f), new HeavyLandingRootPoint(115.872681f, -3.53637719f),
            new HeavyLandingRootPoint(117.040779f, -4.12429333f), new HeavyLandingRootPoint(118.173004f, -4.47854233f), new HeavyLandingRootPoint(119.286934f, -4.57630873f), new HeavyLandingRootPoint(120.316658f, -4.3269186f),
            new HeavyLandingRootPoint(121.323166f, -3.65282845f), new HeavyLandingRootPoint(122.284767f, -2.58955431f), new HeavyLandingRootPoint(123.115524f, -1.23090518f), new HeavyLandingRootPoint(123.816177f, 0.576181948f),
            new HeavyLandingRootPoint(124.592606f, 3.11726189f), new HeavyLandingRootPoint(125.300667f, 6.05397081f), new HeavyLandingRootPoint(126.095222f, 8.87784195f), new HeavyLandingRootPoint(127.164291f, 11.594162f),
            new HeavyLandingRootPoint(127.931534f, 13.7160625f), new HeavyLandingRootPoint(128.368729f, 15.2159662f), new HeavyLandingRootPoint(128.503601f, 15.9431515f), new HeavyLandingRootPoint(128.458038f, 16.1793365f),
            new HeavyLandingRootPoint(128.221085f, 16.2261086f), new HeavyLandingRootPoint(127.719673f, 16.2162685f), new HeavyLandingRootPoint(127.154037f, 16.2671547f), new HeavyLandingRootPoint(126.738953f, 16.2550983f),
            new HeavyLandingRootPoint(126.797577f, 16.1399937f), new HeavyLandingRootPoint(127.192619f, 16.1408882f), new HeavyLandingRootPoint(127.791451f, 16.2440834f), new HeavyLandingRootPoint(128.517899f, 16.2944889f),
            new HeavyLandingRootPoint(129.198441f, 16.204155f), new HeavyLandingRootPoint(129.657806f, 15.9666109f), new HeavyLandingRootPoint(130.023956f, 15.9268589f), new HeavyLandingRootPoint(130.314209f, 16.1279831f),
            new HeavyLandingRootPoint(130.500992f, 16.4399548f), new HeavyLandingRootPoint(130.460449f, 16.8108234f), new HeavyLandingRootPoint(130.138763f, 17.2587204f), new HeavyLandingRootPoint(129.575089f, 17.6954727f),
            new HeavyLandingRootPoint(128.791519f, 17.8760834f), new HeavyLandingRootPoint(128.105988f, 18.0666237f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 0.999627888f, 1.9000001f),
        ]);

    /// <summary>The <c>HM_Landing_A_R_RecoveryRoll_00</c> animation.</summary>
    public static HeavyLandingClip RecoveryRoll { get; } = new(
        "HM_Landing_A_R_RecoveryRoll_00", 30.0,
        [
            new HeavyLandingRootPoint(0.0f, -0.852768838f), new HeavyLandingRootPoint(0.0f, 10.5041933f), new HeavyLandingRootPoint(0.0f, 21.9203758f), new HeavyLandingRootPoint(0.0f, 33.3918304f),
            new HeavyLandingRootPoint(0.0f, 44.9146118f), new HeavyLandingRootPoint(0.0f, 56.4847641f), new HeavyLandingRootPoint(0.0f, 68.0983505f), new HeavyLandingRootPoint(0.0f, 79.7514191f),
            new HeavyLandingRootPoint(0.0f, 91.4400177f), new HeavyLandingRootPoint(0.0f, 103.160202f), new HeavyLandingRootPoint(0.0f, 114.908012f), new HeavyLandingRootPoint(0.0f, 126.67952f),
            new HeavyLandingRootPoint(0.0f, 138.470764f), new HeavyLandingRootPoint(0.0f, 150.277817f), new HeavyLandingRootPoint(0.0f, 162.096695f), new HeavyLandingRootPoint(0.0f, 173.923477f),
            new HeavyLandingRootPoint(0.0f, 185.754196f), new HeavyLandingRootPoint(0.0f, 197.584915f), new HeavyLandingRootPoint(0.0f, 209.411713f), new HeavyLandingRootPoint(0.0f, 221.230576f),
            new HeavyLandingRootPoint(0.0f, 233.037628f), new HeavyLandingRootPoint(0.0f, 244.828873f), new HeavyLandingRootPoint(0.0f, 256.600372f), new HeavyLandingRootPoint(0.0f, 268.348175f),
            new HeavyLandingRootPoint(0.0f, 280.06839f), new HeavyLandingRootPoint(0.0f, 291.756989f), new HeavyLandingRootPoint(0.0f, 303.410034f), new HeavyLandingRootPoint(0.0f, 315.023621f),
            new HeavyLandingRootPoint(0.0f, 326.593781f), new HeavyLandingRootPoint(0.0f, 338.116577f), new HeavyLandingRootPoint(0.0f, 349.588013f), new HeavyLandingRootPoint(0.0f, 361.004211f),
            new HeavyLandingRootPoint(0.0f, 372.361145f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.QuickExit, 0.893470943f, 1.06666684f),
        ]);

    /// <summary>The <c>HM_Landing_A_UA_Pancake_00</c> animation.</summary>
    public static HeavyLandingClip Pancake { get; } = new(
        "HM_Landing_A_UA_Pancake_00", 30.0,
        [],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 3.02860999f, 5.0f),
        ]);

    /// <summary>The <c>HM_Landing_A_UA_SuperHeavyBwd_00</c> animation.</summary>
    public static HeavyLandingClip SuperHeavyBackward { get; } = new(
        "HM_Landing_A_UA_SuperHeavyBwd_00", 30.0,
        [
            new HeavyLandingRootPoint(0.0f, 0.0f), new HeavyLandingRootPoint(0.0f, -3.66577816f), new HeavyLandingRootPoint(0.0f, -6.72476387f), new HeavyLandingRootPoint(0.0f, -9.23637486f),
            new HeavyLandingRootPoint(0.0f, -15.5527172f), new HeavyLandingRootPoint(0.0f, -28.2628536f), new HeavyLandingRootPoint(0.0f, -42.8148727f), new HeavyLandingRootPoint(0.0f, -55.797287f),
            new HeavyLandingRootPoint(0.0f, -68.3032761f), new HeavyLandingRootPoint(0.0f, -74.4816589f), new HeavyLandingRootPoint(0.0f, -86.0440598f), new HeavyLandingRootPoint(0.0f, -97.029686f),
            new HeavyLandingRootPoint(0.0f, -106.667801f), new HeavyLandingRootPoint(0.0f, -116.018028f), new HeavyLandingRootPoint(0.0f, -126.12822f), new HeavyLandingRootPoint(0.0f, -136.149704f),
            new HeavyLandingRootPoint(0.0f, -147.039673f), new HeavyLandingRootPoint(0.0f, -158.081223f), new HeavyLandingRootPoint(0.0f, -167.26947f), new HeavyLandingRootPoint(0.0f, -174.478302f),
            new HeavyLandingRootPoint(0.0f, -178.542618f), new HeavyLandingRootPoint(0.0f, -182.102493f), new HeavyLandingRootPoint(0.0f, -183.504074f), new HeavyLandingRootPoint(0.0f, -185.582428f),
            new HeavyLandingRootPoint(0.0f, -188.422592f), new HeavyLandingRootPoint(0.0f, -191.611115f), new HeavyLandingRootPoint(0.0f, -195.152206f), new HeavyLandingRootPoint(0.0f, -198.696182f),
            new HeavyLandingRootPoint(0.0f, -202.100952f), new HeavyLandingRootPoint(0.0f, -205.236526f), new HeavyLandingRootPoint(0.0f, -208.331863f), new HeavyLandingRootPoint(0.0f, -211.407867f),
            new HeavyLandingRootPoint(0.0f, -214.149475f), new HeavyLandingRootPoint(0.0f, -216.723282f), new HeavyLandingRootPoint(0.0f, -219.057129f), new HeavyLandingRootPoint(0.0f, -219.804337f),
            new HeavyLandingRootPoint(0.0f, -221.301056f), new HeavyLandingRootPoint(0.0f, -222.750275f), new HeavyLandingRootPoint(0.0f, -224.119949f), new HeavyLandingRootPoint(0.0f, -225.402313f),
            new HeavyLandingRootPoint(0.0f, -226.708084f), new HeavyLandingRootPoint(0.0f, -227.903854f), new HeavyLandingRootPoint(0.0f, -228.922363f), new HeavyLandingRootPoint(0.0f, -229.815369f),
            new HeavyLandingRootPoint(0.0f, -230.740921f), new HeavyLandingRootPoint(0.0f, -231.749176f), new HeavyLandingRootPoint(0.0f, -232.692108f), new HeavyLandingRootPoint(0.0f, -233.542435f),
            new HeavyLandingRootPoint(0.0f, -234.280334f), new HeavyLandingRootPoint(0.0f, -234.893234f), new HeavyLandingRootPoint(0.0f, -235.375732f), new HeavyLandingRootPoint(0.0f, -235.729446f),
            new HeavyLandingRootPoint(0.0f, -235.962906f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
            new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f), new HeavyLandingRootPoint(0.0f, -236.091415f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 2.08588552f, 3.4666667f),
        ]);

    /// <summary>The <c>HM_Landing_A_UA_SuperHeavyFwd_00</c> animation.</summary>
    public static HeavyLandingClip SuperHeavyForward { get; } = new(
        "HM_Landing_A_UA_SuperHeavyFwd_00", 30.0,
        [
            new HeavyLandingRootPoint(0.0f, 0.0f), new HeavyLandingRootPoint(0.0f, 6.93376732f), new HeavyLandingRootPoint(0.0f, 13.4691658f), new HeavyLandingRootPoint(0.0f, 19.6165199f),
            new HeavyLandingRootPoint(0.0f, 25.3861542f), new HeavyLandingRootPoint(0.0f, 30.7883911f), new HeavyLandingRootPoint(0.0f, 35.8335571f), new HeavyLandingRootPoint(0.0f, 40.5319786f),
            new HeavyLandingRootPoint(0.0f, 44.8939743f), new HeavyLandingRootPoint(0.0f, 48.9298744f), new HeavyLandingRootPoint(0.0f, 52.6500015f), new HeavyLandingRootPoint(0.0f, 56.0646744f),
            new HeavyLandingRootPoint(0.0f, 59.1842232f), new HeavyLandingRootPoint(0.0f, 62.0189743f), new HeavyLandingRootPoint(0.0f, 64.5792465f), new HeavyLandingRootPoint(0.0f, 66.8753662f),
            new HeavyLandingRootPoint(0.0f, 68.9176559f), new HeavyLandingRootPoint(0.0f, 70.7164459f), new HeavyLandingRootPoint(0.0f, 72.2820511f), new HeavyLandingRootPoint(0.0f, 73.6248093f),
            new HeavyLandingRootPoint(0.0f, 74.7550354f), new HeavyLandingRootPoint(0.0f, 75.6830521f), new HeavyLandingRootPoint(0.0f, 76.4191818f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
            new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f), new HeavyLandingRootPoint(0.0f, 76.9737625f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 1.97775161f, 3.66666698f),
        ]);

    /// <summary>The <c>HM_Landing_A_UA_SuperHeavyLeft_00</c> animation.</summary>
    public static HeavyLandingClip SuperHeavyLeft { get; } = new(
        "HM_Landing_A_UA_SuperHeavyLeft_00", 30.0,
        [
            new HeavyLandingRootPoint(3.22506499f, 4.43969107f), new HeavyLandingRootPoint(3.15763593f, 4.43969107f), new HeavyLandingRootPoint(2.29244518f, 4.43969107f), new HeavyLandingRootPoint(-0.460838109f, 4.43969107f),
            new HeavyLandingRootPoint(-4.48001575f, 4.43969107f), new HeavyLandingRootPoint(-9.37492466f, 4.43969107f), new HeavyLandingRootPoint(-15.1409464f, 4.43969107f), new HeavyLandingRootPoint(-22.4458294f, 4.43969107f),
            new HeavyLandingRootPoint(-32.6597099f, 4.43969107f), new HeavyLandingRootPoint(-44.7311554f, 4.43969107f), new HeavyLandingRootPoint(-58.5036736f, 4.43969107f), new HeavyLandingRootPoint(-74.3268738f, 4.43969107f),
            new HeavyLandingRootPoint(-91.7007294f, 4.43969107f), new HeavyLandingRootPoint(-105.817139f, 4.43969107f), new HeavyLandingRootPoint(-121.530838f, 4.43969107f), new HeavyLandingRootPoint(-132.912018f, 4.43969107f),
            new HeavyLandingRootPoint(-140.695892f, 4.43969107f), new HeavyLandingRootPoint(-147.193802f, 4.43969107f), new HeavyLandingRootPoint(-152.425751f, 4.43969107f), new HeavyLandingRootPoint(-156.691605f, 4.43969107f),
            new HeavyLandingRootPoint(-160.787949f, 4.43969107f), new HeavyLandingRootPoint(-164.26947f, 4.43969107f), new HeavyLandingRootPoint(-167.143921f, 4.43969107f), new HeavyLandingRootPoint(-169.47084f, 4.43969107f),
            new HeavyLandingRootPoint(-171.316742f, 4.43969107f), new HeavyLandingRootPoint(-172.708328f, 4.43969107f), new HeavyLandingRootPoint(-173.735748f, 4.43969107f), new HeavyLandingRootPoint(-174.387604f, 4.43969107f),
            new HeavyLandingRootPoint(-174.99939f, 4.43969107f), new HeavyLandingRootPoint(-175.728241f, 4.43969107f), new HeavyLandingRootPoint(-176.298096f, 4.43969107f), new HeavyLandingRootPoint(-176.50647f, 4.43969107f),
            new HeavyLandingRootPoint(-176.388931f, 4.43969107f), new HeavyLandingRootPoint(-175.835434f, 4.43969107f), new HeavyLandingRootPoint(-174.998962f, 4.43969107f), new HeavyLandingRootPoint(-173.844681f, 4.43969107f),
            new HeavyLandingRootPoint(-172.066879f, 4.43969107f), new HeavyLandingRootPoint(-171.977051f, 4.43969107f), new HeavyLandingRootPoint(-172.06398f, 4.43969107f), new HeavyLandingRootPoint(-172.196259f, 4.43969107f),
            new HeavyLandingRootPoint(-172.364487f, 4.43969107f), new HeavyLandingRootPoint(-172.55426f, 4.43969107f), new HeavyLandingRootPoint(-172.751328f, 4.43969107f), new HeavyLandingRootPoint(-172.94101f, 4.43969107f),
            new HeavyLandingRootPoint(-173.108475f, 4.43969107f), new HeavyLandingRootPoint(-173.23938f, 4.43969107f), new HeavyLandingRootPoint(-173.319122f, 4.43969107f), new HeavyLandingRootPoint(-173.336685f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336685f, 4.43969107f), new HeavyLandingRootPoint(-173.336686f, 4.43969107f), new HeavyLandingRootPoint(-173.336686f, 4.43969107f), new HeavyLandingRootPoint(-173.336686f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336686f, 4.43969107f), new HeavyLandingRootPoint(-173.336687f, 4.43969107f), new HeavyLandingRootPoint(-173.336687f, 4.43969107f), new HeavyLandingRootPoint(-173.336687f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336688f, 4.43969107f), new HeavyLandingRootPoint(-173.336688f, 4.43969107f), new HeavyLandingRootPoint(-173.336688f, 4.43969107f), new HeavyLandingRootPoint(-173.336688f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336689f, 4.43969107f), new HeavyLandingRootPoint(-173.336689f, 4.43969107f), new HeavyLandingRootPoint(-173.336689f, 4.43969107f), new HeavyLandingRootPoint(-173.336689f, 4.43969107f),
            new HeavyLandingRootPoint(-173.33669f, 4.43969107f), new HeavyLandingRootPoint(-173.33669f, 4.43969107f), new HeavyLandingRootPoint(-173.33669f, 4.43969107f), new HeavyLandingRootPoint(-173.33669f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336691f, 4.43969107f), new HeavyLandingRootPoint(-173.336691f, 4.43969107f), new HeavyLandingRootPoint(-173.336691f, 4.43969107f), new HeavyLandingRootPoint(-173.336691f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336692f, 4.43969107f), new HeavyLandingRootPoint(-173.336692f, 4.43969107f), new HeavyLandingRootPoint(-173.336692f, 4.43969107f), new HeavyLandingRootPoint(-173.336692f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336693f, 4.43969107f), new HeavyLandingRootPoint(-173.336693f, 4.43969107f), new HeavyLandingRootPoint(-173.336693f, 4.43969107f), new HeavyLandingRootPoint(-173.336693f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336694f, 4.43969107f), new HeavyLandingRootPoint(-173.336694f, 4.43969107f), new HeavyLandingRootPoint(-173.336694f, 4.43969107f), new HeavyLandingRootPoint(-173.336694f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336695f, 4.43969107f), new HeavyLandingRootPoint(-173.336695f, 4.43969107f), new HeavyLandingRootPoint(-173.336695f, 4.43969107f), new HeavyLandingRootPoint(-173.336696f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336696f, 4.43969107f), new HeavyLandingRootPoint(-173.336696f, 4.43969107f), new HeavyLandingRootPoint(-173.336696f, 4.43969107f), new HeavyLandingRootPoint(-173.336697f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336697f, 4.43969107f), new HeavyLandingRootPoint(-173.336697f, 4.43969107f), new HeavyLandingRootPoint(-173.336697f, 4.43969107f), new HeavyLandingRootPoint(-173.336698f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336698f, 4.43969107f), new HeavyLandingRootPoint(-173.336698f, 4.43969107f), new HeavyLandingRootPoint(-173.336698f, 4.43969107f), new HeavyLandingRootPoint(-173.336699f, 4.43969107f),
            new HeavyLandingRootPoint(-173.336699f, 4.43969107f), new HeavyLandingRootPoint(-173.336699f, 4.43969107f), new HeavyLandingRootPoint(-173.336699f, 4.43969107f), new HeavyLandingRootPoint(-173.3367f, 4.43969107f),
            new HeavyLandingRootPoint(-173.3367f, 4.43969107f), new HeavyLandingRootPoint(-173.3367f, 4.43969107f), new HeavyLandingRootPoint(-173.3367f, 4.43969107f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 2.06521487f, 3.4958322f),
        ]);

    /// <summary>The <c>HM_Landing_A_UA_SuperHeavyRight_00</c> animation.</summary>
    public static HeavyLandingClip SuperHeavyRight { get; } = new(
        "HM_Landing_A_UA_SuperHeavyRight_00", 30.0,
        [
            new HeavyLandingRootPoint(0.0f, 0.0f), new HeavyLandingRootPoint(8.7510128f, 0.0f), new HeavyLandingRootPoint(21.4221344f, 0.0f), new HeavyLandingRootPoint(34.3179054f, 0.0f),
            new HeavyLandingRootPoint(44.2437706f, 0.0f), new HeavyLandingRootPoint(58.2692299f, 0.0f), new HeavyLandingRootPoint(72.2801743f, 0.0f), new HeavyLandingRootPoint(86.3839798f, 0.0f),
            new HeavyLandingRootPoint(100.683975f, 0.0f), new HeavyLandingRootPoint(115.963425f, 0.0f), new HeavyLandingRootPoint(127.878601f, 0.0f), new HeavyLandingRootPoint(138.19696f, 0.0f),
            new HeavyLandingRootPoint(142.195068f, 0.0f), new HeavyLandingRootPoint(146.64505f, 0.0f), new HeavyLandingRootPoint(150.423859f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
            new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f), new HeavyLandingRootPoint(151.029434f, 0.0f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 2.00555015f, 3.5333333f),
        ]);

    /// <summary>The <c>HM_Landing_A_UA_SuperPancakeGetUp_00</c> animation.</summary>
    public static HeavyLandingClip SuperPancakeGetUp { get; } = new(
        "HM_Landing_A_UA_SuperPancakeGetUp_00", 30.0,
        [
            new HeavyLandingRootPoint(0.0f, 0.0f),
        ],
        [
            new HeavyLandingWindow(HeavyLandingTransientState.QuickExit, 3.30578375f, 4.07599163f),
            new HeavyLandingWindow(HeavyLandingTransientState.Finished, 4.06509638f, 7.33333445f),
        ]);

    /// <summary>Levels of an ordinary heavy landing, lowest fall height first.</summary>
    public static IReadOnlyList<HeavyLandingLevel> Heavy { get; } =
    [
        new(0.0, HeavyFallOnSpot, HeavyFallForward, HeavyFallLeft, HeavyFallRight, HeavyFallBackward),
        new(1200.0, SuperHeavyForward, SuperHeavyForward, SuperHeavyLeft, SuperHeavyRight, SuperHeavyBackward),
        new(1800.0, Pancake, null, null, null, null),
        new(2400.0, SuperPancakeGetUp, null, null, null, null),
    ];

    /// <summary>Levels of a heavy landing softened by a recovery roll.</summary>
    public static IReadOnlyList<HeavyLandingLevel> Recovery { get; } =
    [
        new(0.0, RecoveryRoll, null, null, null, null),
    ];
}
