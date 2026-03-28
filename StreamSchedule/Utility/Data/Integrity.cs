using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Utility.Data;

[PrimaryKey(nameof(ID))]
public class Integrity
{
    public int ID;
    public string Token = "";
    public string DeviceID = "";
    public DateTime ExpiresAt;
}
