namespace StreamSchedule.GraphQL.Data;

public class Stream
{
    public Game? Game { get; set; }
    public float? AverageFPS { get; set; }
    public float? Bitrate { get; set; }
    public int? ViewersCount { get; set; }
    public User? Broadcaster { get; set; }
    public int? ClipCount { get; set; }
    public DateTime? CreatedAt { get; set; }
}
