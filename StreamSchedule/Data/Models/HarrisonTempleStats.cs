using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Data.Models;

[PrimaryKey(nameof(UserId))]
public class HarrisonTempleStat
{
    public int UserId { get; set; }
    public int TotalExp { get; set; }
    public int TotalCoins { get; set; }
}
