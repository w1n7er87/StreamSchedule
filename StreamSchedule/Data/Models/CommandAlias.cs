using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Data.Models;

[PrimaryKey("CommandName")]
public class CommandAlias
{
    public required string CommandName {  get; set; }
    public List<string>? Aliases { get; set; }
}
