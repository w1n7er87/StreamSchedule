using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Data.Models;

[PrimaryKey("Name")]
public class TextCommand
{
    public required string Name { get; set; }
    public List<string>? Aliases { get; set; }
    public required string Content { get; set; }
    public Privileges Privileges { get; set; }
}
