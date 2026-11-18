using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Progression;

/// <summary>The PC café event schedule, reward definitions and the player's progress.</summary>
public sealed record PcCafeEventResponse(
    [property: JsonPropertyName("attendanceReward")] IReadOnlyList<PcCafeReward> AttendanceReward,
    [property: JsonPropertyName("missionReward")] IReadOnlyList<PcCafeReward> MissionReward,
    [property: JsonPropertyName("attendance")] PcCafeAttendance Attendance,
    [property: JsonPropertyName("mission")] PcCafeMission Mission,
    [property: JsonPropertyName("lastSyncAt")] long LastSyncAt,
    [property: JsonPropertyName("startAt")] long StartAt,
    [property: JsonPropertyName("endAt")] long EndAt);

public sealed record PcCafeReward(
    [property: JsonPropertyName("rewardNo")] long RewardNo,
    [property: JsonPropertyName("rewardItem")] IReadOnlyList<PcCafeRewardItem> RewardItem);

public sealed record PcCafeRewardItem(
    [property: JsonPropertyName("gameAssetId")] long GameAssetId,
    [property: JsonPropertyName("amount")] long Amount);

public sealed record PcCafeAttendance(
    [property: JsonPropertyName("count")] long Count,
    [property: JsonPropertyName("claimedDays")] IReadOnlyList<long> ClaimedDays);

public sealed record PcCafeMission(
    [property: JsonPropertyName("playTime")] long PlayTime,
    [property: JsonPropertyName("claimedTimes")] IReadOnlyList<long> ClaimedTimes);
