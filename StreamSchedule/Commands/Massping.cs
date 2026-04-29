using StreamSchedule.Data;
using TwitchLib.Client.Enums;

namespace StreamSchedule.Commands;

internal class Massping : Command
{
    public override string Call => "massping";
    public override Privileges Privileges => Privileges.Trusted;
    public override string Help => "let's get crazy and ping everyone in chat ";
    public override TimeSpan Cooldown => TimeSpan.FromHours(24);
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];
    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string first = message.Content.Split(" ").FirstOrDefault() ?? "yo";
        string names = string.Join(" ", BotCore.MessageCache
            .Where(m => m.UserType != UserType.Moderator)
            .GroupBy(m => m.Username)
            .Select(g => new { name = g.Key, count = g.Count() })
            .OrderByDescending(p => p.count)
            .Take(50).Select(p => p.name));
        
        return Task.FromResult(new CommandResult($"{first} {names}", requiresFilter: true, reply: false));
    }
}
