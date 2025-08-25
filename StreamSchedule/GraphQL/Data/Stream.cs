namespace StreamSchedule.GraphQL.Data;

public record Stream(
    Game? Game,
    float? AverageFPS,
    float? Bitrate,
    int? ViewersCount,
    User? Broadcaster,
    int? ClipCount,
    DateTime? CreatedAt);
