namespace StreamSchedule.GraphQL.Data;

public class HypeTrainExecution
{
    public HypeTrainConfig? Config { get; set; }
    public HypeTrainProgress? Progress { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool? IsGoldenKappaTrain { get; set; }
    public bool? IsTreasureTrain { get; set; }
    public bool? IsFastMode { get; set; }
    
    public TreasureTrainDetails? TreasureTrainDetails { get; set; }
    public HypeTrainCompleted? AllTimeHigh { get; set; }
    public List<HypeTrainParticipation?>? Participations { get; set; } = [];
    public SharedHypeTrainDetails? SharedHypeTrainDetails { get; set; }
}