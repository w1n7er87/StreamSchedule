using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Say : Command
{
    public override string Call => "say";
    public override Privileges Privileges => Privileges.Uuh;
    public override string Help => "say";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[] Arguments => ["f"];
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        string content = Commands.RetrieveArguments(Arguments, message.content, out Dictionary<string, string> usedArgs);
        return Task.FromResult(new CommandResult(content, false, usedArgs.TryGetValue("f", out _)));
    }
}
