using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Data.Models;

[PrimaryKey(nameof(ID))]
public class PermittedTerm
{
    public int ID { get; set; }
    public string Term { get; set; }
    public bool Noreplace { get; set; } = false;
    public bool Anycase { get; set; } = false;
    public string? Alternative { get; set; }
}
