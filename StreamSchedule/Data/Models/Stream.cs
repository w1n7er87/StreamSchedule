using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Data.Models;

[PrimaryKey("Id")]
public class Stream
{
    public int Id { get; set; }
    public DateOnly StreamDate { get; set; }
    public TimeOnly StreamTime { get; set; }
    public string? StreamTitle { get; set; }
    public StreamStatus StreamStatus { get; set; }
}
