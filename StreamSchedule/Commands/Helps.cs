using StreamSchedule.Data;
using StreamSchedule.Data.Models;

namespace StreamSchedule.Commands;

internal class Helps : Command
{
    internal override string Call => "helps";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "show command help: [command name] ";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Medium);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string[] split = message.content.Split(' ');
        if (split.Length < 1) { return Task.FromResult(new CommandResult(this.Help)); }

        string requestedCommand = split[0].ToLower();

        foreach (Command c in Commands.CurrentCommands)
        {
            var cmdAliases = BotCore.DBContext.CommandAliases.Find(c.Call.ToLower());
            
            if ((cmdAliases is null || cmdAliases.Aliases is null || !cmdAliases.Aliases.Contains(requestedCommand)) && !requestedCommand.Equals(c.Call)) continue;

            string aliases = "";
            if (cmdAliases?.Aliases is not null && cmdAliases.Aliases.Count != 0)
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

        List<TextCommand> textCommands = Commands.CurrentTextCommands;
        foreach (TextCommand c in textCommands)
        {
            if ((c.Aliases is null || !c.Aliases.Contains(requestedCommand)) && !requestedCommand.Equals(c.Name)) continue;
            
            string aliases = "";
            if (c.Aliases is not null && c.Aliases.Count != 0)
            {
                aliases = $"({string.Join(",", c.Aliases)}) ";
            }
            
            return Task.FromResult(new CommandResult($"{aliases}simple text command mhm . ", false));
        }
        
        return Task.FromResult(new CommandResult(this.Help));
    }
}
