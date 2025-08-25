namespace StreamSchedule.GraphQL.Data;

public record BroadcastSettings(
    Game? Game,
    string? Title,
    bool? IsMature,
    LiveUpNotificationInfo? LiveUpNotificationInfo);
