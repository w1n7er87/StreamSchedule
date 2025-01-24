using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Data.Models;

[PrimaryKey("ID")]
public class EmoteMonitorChannel
{
    public int ID { get; set; }
    public int ChannelID { get; set; }
    public string ChannelName { get; set; }
    public string OutputChannelName { get; set; }
}
