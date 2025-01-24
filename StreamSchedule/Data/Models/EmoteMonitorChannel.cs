using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Data.Models;

[PrimaryKey("ID")]
public class EmoteMonitorChannel
{
    public int ID { get; set; }
    public bool Deleted { get; set; } = false;
    public int ChannelID { get; set; }
    public string ChannelName { get; set; }
    public string OutputChannelName { get; set; }
    public List<string> UpdateSubscribers { get; set; } = [];
}
