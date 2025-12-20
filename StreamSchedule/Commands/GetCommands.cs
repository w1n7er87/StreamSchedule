using StreamSchedule.Data;
using System.Text;

namespace StreamSchedule.Commands;

internal class GetCommands : Command
{
    public override string Call => "commands";
    public override Privileges Privileges => Privileges.Banned;
    public override string Help => "show list of commands available to you";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["q"];
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        _ = Commands.RetrieveArguments(Arguments, message.Content, out Dictionary<string, string> usedArgs);

        StringBuilder response = new();
        response.Append(string.Join(" ", Commands.AllCommands.Where(c => c.Privileges <= message.Sender.Privileges).Select(c => c.Call)));
        return Task.FromResult(new CommandResult(response + ". "));
    }
}
