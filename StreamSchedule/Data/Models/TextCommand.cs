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

    [NotMapped] public string Call => Name;
    [NotMapped] public string Help => "simple text command Aloo ";
    [NotMapped] public TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    [NotMapped] public Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    [NotMapped] public string[]? Arguments => null;

    public Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] replacement = message.Content.Split(' ', StringSplitOptions.TrimEntries);
        int targetIndex = 0;

        Span<string> split = Content.Split('`').AsSpan();
        for (int i = 0; i < split.Length; i++)
        {
            if (split[i].StartsWith('/'))
            {
                split[i] = replacement.Length == 0 || replacement.Length - 1 < targetIndex || string.IsNullOrWhiteSpace(replacement[targetIndex]) ? split[i].Replace("/", "") : replacement[targetIndex][..Math.Min(replacement[targetIndex].Length, 50)];

                targetIndex = Math.Min(replacement.Length - 1, targetIndex + 1);
                continue;
            }
            
            if (split[i].StartsWith('@'))
            {
                split[i] = message.Sender.Username!;
            }
        }

        return Task.FromResult(new CommandResult(string.Join(null, split), false, true));
    }
}
