using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Data.Models;

[PrimaryKey("StreamDate")]
public class Stream
{
    public DateOnly StreamDate { get; set; }
    public TimeOnly StreamTime { get; set; }
    public string? StreamTitle { get; set; }
}
