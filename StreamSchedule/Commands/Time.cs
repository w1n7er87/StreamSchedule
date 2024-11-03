using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Time : Command
{
    internal override string Call => "time";
    internal override Privileges MinPrivilege => Privileges.None;
    internal override string Help => "current time.";
    internal override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    internal override Dictionary<string, DateTime> LastUsedOnChannel { get; set; } = [];
    internal override string[]? Arguments => null;

    internal override Task<CommandResult> Handle(UniversalMessageInfo message)
    {
        return Task.FromResult(new CommandResult($" British {DateTime.Now:ddd HH:mm:ss} Latege ", false));
    }
}
