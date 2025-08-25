using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StreamSchedule.Data.Models;

[PrimaryKey(nameof(CommandName))]
public class CommandAlias
{
    public string CommandName { get; set; }

    [Column("Aliases")] public List<string>? StoredAliases { get; set; }

    [NotMapped]
    public List<string> Aliases
    {
        get
        {
            if (StoredAliases is not null) return StoredAliases;
            StoredAliases = new();
            return StoredAliases;
        }
        set => StoredAliases = value;
    }
}
