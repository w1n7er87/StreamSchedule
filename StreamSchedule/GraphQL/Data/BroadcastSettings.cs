namespace StreamSchedule.GraphQL.Data;

public class BroadcastSettings
{
    public Game? Game { get; set; }
    public string? Title { get; set; }
    public bool? IsMature { get; set; }
    public LiveUpNotificationInfo? LiveUpNotificationInfo { get; set; }
}
