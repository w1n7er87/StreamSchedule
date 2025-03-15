using StreamSchedule.Data;
using System.Text;

namespace StreamSchedule.Commands;

internal class GetCommands : Command
{
    internal override string Call => "commands";
    internal override Privileges MinPrivilege => Privileges.Banned;
    internal override string Help => "show list of commands available to you";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["q"];

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        _ = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);

        StringBuilder response = new();
        if (usedArgs.TryGetValue("q", out _))
        {
            response.Append(string.Join(", ", BotCore.DBContext.TextCommands.Where(c => c.Privileges <= message.sender.Privileges).Select(c => c.Name)));
        }
        else
        {
            response.Append(string.Join(", ", Commands.CurrentCommands.Where(c => c.MinPrivilege <= message.sender.Privileges).Select(c => c.Call)));
        }
        return Task.FromResult(new CommandResult(response + ". "));
    }
}
