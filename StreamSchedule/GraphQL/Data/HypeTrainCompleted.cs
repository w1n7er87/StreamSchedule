namespace StreamSchedule.GraphQL.Data;

public record HypeTrainCompleted(
    HypeTrainLevel? Level,
    int? Goal,
    int? Progression);
