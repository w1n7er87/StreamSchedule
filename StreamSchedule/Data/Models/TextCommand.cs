using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using StreamSchedule.Commands;

namespace StreamSchedule.Data.Models;

[PrimaryKey(nameof(Name))]
internal class TextCommand : ICommand
{
    public string Name { get; set; }
    public string Content { get; set; }
    public Privileges Privileges { get; set; }
    
    [Column("Aliases")]
    public List<string>? StoredAliases { get; set; }

    [NotMapped]
    public List<string> Aliases
    {
        get {
            if (StoredAliases is not null) return StoredAliases;
            StoredAliases = new List<string>();
            return StoredAliases;
        }
        set => StoredAliases = value;
    }

    [NotMapped] public string Call => Name;
    [NotMapped] public string Help => "simple text command Aloo ";
    [NotMapped] public TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    [NotMapped] public Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    [NotMapped] public string[]? Arguments => null;

    public Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        return Task.FromResult(new CommandResult(Content, reply:false));
    }
}
