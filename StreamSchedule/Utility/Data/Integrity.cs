using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Utility.Data;

[PrimaryKey(nameof(ID))]
public class Integrity
{
    public int ID { get; }
    public string Token { get; set; }
    public string DeviceID { get; set; }
    public DateTime ExpiresAt { get; set; }
}
