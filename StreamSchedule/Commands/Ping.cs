using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Ping : Command
{
    internal override string Call => "ping";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "ping";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Minute);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        bool result = BotCore.Client.Connect();
        Console.WriteLine($"is connected: {BotCore.Client.IsConnected} connect result: {result} \n");
        return Task.FromResult(new CommandResult("PotFriend ", false));
    }
}
