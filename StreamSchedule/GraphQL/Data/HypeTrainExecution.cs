namespace StreamSchedule.GraphQL.Data;

public record HypeTrainExecution(
    HypeTrainConfig? Config,
    HypeTrainProgress? Progress,
    DateTime? StartedAt,
    DateTime? ExpiresAt,
    bool? IsGoldenKappaTrain,
    bool? IsTreasureTrain,
    bool? IsFastMode,
    TreasureTrainDetails? TreasureTrainDetails,
    HypeTrainCompleted? AllTimeHigh,
    HypeTrainParticipation?[]? Participations,
    SharedHypeTrainDetails? SharedHypeTrainDetails,
    CommunityTrainDetails? VariantTrainDetails);
