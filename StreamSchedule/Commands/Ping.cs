using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Ping : Command
{
    internal override string Call => "ping";
    internal override Privileges MinPrivilege => Privileges.Trusted;
    internal override string Help => "ping";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Short);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        return Task.FromResult( new CommandResult("PotFriend ", false));
    }
}
