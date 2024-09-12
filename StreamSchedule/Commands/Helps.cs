using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Helps : Command
{
    internal override string Call => "helps";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show command help: [command name] ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];

    internal override Task<string> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        if (split.Length < 1) { return Task.FromResult(this.Help); }
        foreach (Command? c in Body.CurrentCommands)
        {
            if (c == null) { continue; }
            if (split[0] == c.Call) { return Task.FromResult(c.Help); }
        }
        return Task.FromResult(this.Help);
    }
}
