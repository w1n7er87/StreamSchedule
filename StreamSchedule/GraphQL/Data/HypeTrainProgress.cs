namespace StreamSchedule.GraphQL.Data;

public record HypeTrainProgress(
    HypeTrainLevel? Level,
    int? Goal,
    int? Progression,
    int? Total);
