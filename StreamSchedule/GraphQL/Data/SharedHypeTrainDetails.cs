namespace StreamSchedule.GraphQL.Data;

public class SharedHypeTrainDetails
{
    public List<SharedHypeTrainProgress?>? SharedProgress { get; set; } = [];
    public SharedHypeTrainAllTimeHigh[]? SharedAllTimeHighRecords { get; set; }
}
