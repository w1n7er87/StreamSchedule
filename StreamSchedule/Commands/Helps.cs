using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Helps : Command
{
    internal override string Call => "helps";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show command help: [command name] ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds(Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.Message.Split(' ');
        if (split.Length < 1) { return Task.FromResult(new CommandResult(this.Help)); }
        foreach (Command? c in Body.CurrentCommands)
        {
            if (c == null) { continue; }

            string a = "";
            if (c.Arguments is not null)
            {
                a = " args: ";
                foreach (string arg in c.Arguments)
                {
                    a += arg + " ";
                }
            }

            if (split[0] == c.Call) { return Task.FromResult(new CommandResult(c.Help + a)); }
        }
        return Task.FromResult(new CommandResult(this.Help));
    }
}
