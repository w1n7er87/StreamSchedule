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

        foreach (Command c in Commands.CurrentCommands)
        {
            if (split[0] != c.Call) continue;

            var cmdAliases = BotCore.DBContext.CommandAliases.Find(c.Call.ToLower());

            string aliases = "";
            if (cmdAliases is not null && cmdAliases.Aliases is not null && cmdAliases.Aliases.Count != 0)
            {
                aliases = $"({string.Join(",", cmdAliases.Aliases)}) ";
            }

            string args = "";
            if (c.Arguments is not null)
            {
                args = $" args: {string.Join(", ", c.Arguments)}";
            }

            return Task.FromResult(new CommandResult(aliases + c.Help + args));
        }
        return Task.FromResult(new CommandResult(this.Help));
    }
}
