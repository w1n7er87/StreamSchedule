namespace StreamSchedule.GraphQL.Data;

public record HypeTrainApproaching(
    DateTime? ExpiresAt,
    int? Goal,
    bool? IsGoldenKappaTrain,
    bool? IsTreasureTrain,
    HypeTrainApproachingEventsRemaining?[]? EventsRemaining);
