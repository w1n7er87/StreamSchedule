namespace StreamSchedule.GraphQL.Data;

public class HypeTrainApproaching
{
    public DateTime? ExpiresAt { get; set; }
    public int? Goal { get; set; }
    public bool? IsGoldenKappaTrain { get; set; }
    public bool? IsTreasureTrain { get; set; }
    public List<HypeTrainApproachingEventsRemaining?>? EventsRemaining { get; set; } = [];
}
