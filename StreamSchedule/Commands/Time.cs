using StreamSchedule.Data;

namespace StreamSchedule.Commands;

internal class Time : Command
{
    public override string Call => "time";
    public override Privileges Privileges => Privileges.None;
    public override string Help => "current time";
    public override TimeSpan Cooldown => TimeSpan.FromSeconds((int)Cooldowns.Long);
    public override Dictionary<string, DateTime> LastUsedOnChannel { get; } = [];
    public override string[]? Arguments => null;
    public override List<string> Aliases { get; set; } = [];

    public override Task<CommandResult> Handle(UniversalMessageInfo message) => Task.FromResult(new CommandResult($" British {DateTime.Now:ddd HH:mm:ss} Latege ", false));
}
