using StreamSchedule.Data;

namespace StreamSchedule.Commands;

public class Say : Command
{
    internal override string Call => "say";
    internal override Privileges MinPrivilege => Privileges.Uuh;
    internal override string Help => "say";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[] Arguments => ["f"];
    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string content = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);
        return Task.FromResult(new CommandResult(content, false, usedArgs.TryGetValue("f", out _)));
    }
}
