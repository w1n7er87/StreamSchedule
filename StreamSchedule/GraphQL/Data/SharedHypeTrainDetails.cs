namespace StreamSchedule.GraphQL.Data;

public record SharedHypeTrainDetails(
    SharedHypeTrainProgress?[]? SharedProgress,
    SharedHypeTrainAllTimeHigh?[]? SharedAllTimeHighRecords
);
